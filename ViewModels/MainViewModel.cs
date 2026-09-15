using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Komik.Helpers;
using Komik.Models;
using Komik.Services;

namespace Komik.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IComicLoaderService _loaderService;
    private readonly IFilePickerService _pickerService;
    private readonly ILibraryRepository _repository;

    private string? _currentComicPath;
    private ComicEntity? _currentComicEntity;

    // Cache of recently decoded page datas to keep memory low and page turns instantaneous
    private readonly ConcurrentDictionary<int, ComicPageData> _pageCache = new();
    private const int MaxCacheEntries = 8;
    private readonly Queue<int> _cacheEvictionQueue = new();

    private CancellationTokenSource? _pageLoadCts;
    private CancellationTokenSource? _webtoonCts;
    private CancellationTokenSource? _progressSaveCts;
    private DispatcherTimer? _toastTimer;

    [ObservableProperty]
    private ComicBook? _currentComic;

    [ObservableProperty]
    private int _currentPageIndex;

    [ObservableProperty]
    private ImageSource? _currentPageImage;

    [ObservableProperty]
    private ImageSource? _secondPageImage;

    [ObservableProperty]
    private FitMode _fitMode = FitMode.FitToHeight;

    [ObservableProperty]
    private ViewMode _viewMode = ViewMode.SinglePage;

    [ObservableProperty]
    private ReadingDirection _readingDirection = ReadingDirection.LeftToRight;

    [ObservableProperty]
    private double _zoomFactor = 1.0;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isToolbarVisible = true;

    [ObservableProperty]
    private bool _isPageOverlayVisible;

    [ObservableProperty]
    private bool _isFullscreen;

    [ObservableProperty]
    private ElementTheme _requestedTheme = ElementTheme.Dark;

    [ObservableProperty]
    private bool _isToastVisible;

    [ObservableProperty]
    private string _toastMessage = string.Empty;

    [ObservableProperty]
    private bool _isCurrentPageBookmarked;

    private readonly IOcrService _ocrService;

    // Live reading-session tracking (active time + distinct pages), checkpointed so stats stay current.
    private readonly ReadingSessionTracker _sessionTracker = new();
    private readonly SemaphoreSlim _sessionSaveLock = new(1, 1);
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _sessionCheckpointTimer;
    private static readonly TimeSpan SessionCheckpointInterval = TimeSpan.FromSeconds(45);

    [ObservableProperty]
    private bool _isOcrLayerVisible;

    [ObservableProperty]
    private OcrPageResult? _currentPageOcrResult;

    [ObservableProperty]
    private bool _isOcrLoading;

    [ObservableProperty]
    private bool _isSearchingOcr;

    [ObservableProperty]
    private string _ocrSearchQuery = string.Empty;

    public ObservableCollection<OcrSearchResultItem> OcrSearchResults { get; } = new();

    public ObservableCollection<WebtoonPageItem> WebtoonPages { get; } = new();

    public bool IsWebtoonMode => ViewMode == ViewMode.VerticalContinuous;

    public bool IsOcrSupported => _ocrService.IsOcrSupported;

    public ColorCorrectionSettings ColorSettings { get; } = new();

    public ObservableCollection<BookmarkEntity> Bookmarks { get; } = new();

    public MainViewModel(
        IComicLoaderService? loaderService = null,
        IFilePickerService? pickerService = null,
        ILibraryRepository? repository = null,
        IOcrService? ocrService = null)
    {
        _loaderService = loaderService ?? new ComicLoaderService();
        _pickerService = pickerService ?? new FilePickerService();
        _repository = repository ?? new LibraryRepository();
        _ocrService = ocrService ?? new OcrService();

        ColorSettings.PropertyChanged += (s, e) =>
        {
            if (HasComic)
            {
                _ = RenderCurrentPageAsync();
                if (IsWebtoonMode)
                {
                    _ = LoadWebtoonPagesAsync();
                }
            }
        };
    }


    public async Task ApplyDefaultSettingsAsync()
    {
        try
        {
            await _repository.InitializeAsync();
            var settings = await _repository.GetAppSettingsAsync();

            FitMode = settings.DefaultFitMode switch
            {
                "FitToWidth" => FitMode.FitToWidth,
                "ActualSize" => FitMode.ActualSize,
                _ => FitMode.FitToHeight
            };

            ReadingDirection = settings.DefaultReadingDirection == "RightToLeft"
                ? ReadingDirection.RightToLeft
                : ReadingDirection.LeftToRight;

            if (Enum.TryParse<ReadingPreset>(settings.DefaultReadingPreset, out var parsedPreset))
            {
                ColorSettings.Preset = parsedPreset;
            }
            else if (settings.DefaultNightMode)
            {
                ColorSettings.Preset = ReadingPreset.NightMode;
            }
            else
            {
                ColorSettings.Preset = ReadingPreset.Original;
            }


            RequestedTheme = settings.Theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] Failed to load default settings: {ex.Message}");
        }
    }

    public bool HasComic => CurrentComic != null && CurrentComic.PageCount > 0;
    public bool HasNoComic => !HasComic;

    public string ComicTitle => CurrentComic?.Title ?? "Komik";
    public int TotalPages => CurrentComic?.PageCount ?? 0;

    public bool CanGoNext
    {
        get
        {
            if (!HasComic) return false;
            if (ViewMode == ViewMode.DoublePage)
            {
                return CurrentPageIndex + 2 < TotalPages || CurrentPageIndex < TotalPages - 1;
            }
            return CurrentPageIndex < TotalPages - 1;
        }
    }

    public bool CanGoPrevious => HasComic && CurrentPageIndex > 0;

    public bool IsDoublePageMode => ViewMode == ViewMode.DoublePage;

    public bool IsSinglePageMode => ViewMode == ViewMode.SinglePage;

    public bool IsRtlMode => ReadingDirection == ReadingDirection.RightToLeft;

    public double CurrentPageNumber => CurrentPageIndex + 1;

    public string BookmarkGlyph => IsCurrentPageBookmarked ? "\uE8A4" : "\uE74E";

    public bool IsFitHeight => FitMode == FitMode.FitToHeight;
    public bool IsFitWidth => FitMode == FitMode.FitToWidth;
    public bool IsFitActual => FitMode == FitMode.ActualSize;

    partial void OnFitModeChanged(FitMode value)
    {
        OnPropertyChanged(nameof(IsFitHeight));
        OnPropertyChanged(nameof(IsFitWidth));
        OnPropertyChanged(nameof(IsFitActual));
    }

    public string ReadingDirectionDisplay => ReadingDirection == ReadingDirection.LeftToRight ? "LTR" : "RTL";

    public string PageDisplayString
    {
        get
        {
            if (!HasComic) return string.Empty;

            if (ViewMode == ViewMode.DoublePage && CurrentPageIndex + 1 < TotalPages)
            {
                return $"{CurrentPageIndex + 1}-{CurrentPageIndex + 2} / {TotalPages}";
            }

            return $"{CurrentPageIndex + 1} / {TotalPages}";
        }
    }

    public string ZoomDisplayString => $"{Math.Round(ZoomFactor * 100)}%";

    [RelayCommand]
    public async Task OpenFileAsync()
    {
        try
        {
            string? path = await _pickerService.PickComicFileAsync();
            if (!string.IsNullOrEmpty(path))
            {
                await LoadComicPathAsync(path);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to open file: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task OpenFolderAsync()
    {
        try
        {
            string? path = await _pickerService.PickComicFolderAsync();
            if (!string.IsNullOrEmpty(path))
            {
                await LoadComicPathAsync(path);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to open folder: {ex.Message}");
        }
    }

    public async Task LoadComicPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        // Flush progress of previous comic before switching
        await FlushReadingProgressAsync();

        IsLoading = true;
        DismissError();

        try
        {
            var comic = await _loaderService.LoadComicAsync(path);

            CurrentComic?.Dispose();
            _pageCache.Clear();
            _cacheEvictionQueue.Clear();

            _currentComicPath = path;
            CurrentComic = comic;
            _ocrService.ClearCache();
            CurrentPageOcrResult = null;
            IsOcrLayerVisible = false;

            // Check database for existing reading progress and bookmarks

            int resumePage = 0;
            try
            {
                _currentComicEntity = await _repository.GetComicByPathAsync(path);
                if (_currentComicEntity != null)
                {
                    await LoadBookmarksAsync(_currentComicEntity.Id);

                    if (_currentComicEntity.IsCompleted || _currentComicEntity.LastReadPage >= comic.PageCount - 1)
                    {
                        // User is re-reading a completed comic from the beginning
                        resumePage = 0;
                        _currentComicEntity.IsCompleted = false;
                        _currentComicEntity.LastReadPage = 0;
                        _currentComicEntity.LastReadAt = DateTime.UtcNow;
                        await _repository.UpdateReadingProgressAsync(path, 0, comic.PageCount);
                        ShowToast($"Re-reading '{comic.Title}' from page 1");
                    }
                    else if (_currentComicEntity.LastReadPage > 0 && _currentComicEntity.LastReadPage < comic.PageCount)
                    {
                        resumePage = _currentComicEntity.LastReadPage;
                        ShowToast($"Resumed at page {resumePage + 1}");
                    }
                    else
                    {
                        _currentComicEntity.LastReadAt = DateTime.UtcNow;
                        await _repository.UpdateReadingProgressAsync(path, 0, comic.PageCount);
                    }
                }
                else
                {
                    Bookmarks.Clear();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Failed to query comic entity: {ex.Message}");
            }

            CurrentPageIndex = resumePage;

            if (_currentComicEntity != null)
            {
                _sessionTracker.Start(_currentComicEntity.Id, resumePage);
                if (IsDoublePageMode && resumePage + 1 < comic.PageCount) _sessionTracker.RecordPageView(resumePage + 1);
                StartSessionCheckpoints();
            }

            NotifyStateChanged();
            await CheckIsCurrentPageBookmarkedAsync();
            await RenderCurrentPageAsync();
            if (IsWebtoonMode)
            {
                _ = LoadWebtoonPagesAsync();
            }

            TriggerOverlayNotification();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        if (!CanGoNext) return;

        int target;
        if (ViewMode == ViewMode.DoublePage)
        {
            target = CurrentPageIndex + 2;
        }
        else
        {
            target = CurrentPageIndex + 1;
        }

        if (target >= TotalPages)
        {
            target = TotalPages - 1;
        }

        CurrentPageIndex = target;
        NotifyStateChanged();
        await CheckIsCurrentPageBookmarkedAsync();
        await RenderCurrentPageAsync();
        QueueProgressSave();
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (!CanGoPrevious) return;

        int target;
        if (ViewMode == ViewMode.DoublePage)
        {
            target = CurrentPageIndex - 2;
        }
        else
        {
            target = CurrentPageIndex - 1;
        }

        if (target < 0) target = 0;

        CurrentPageIndex = target;
        NotifyStateChanged();
        await CheckIsCurrentPageBookmarkedAsync();
        await RenderCurrentPageAsync();
        QueueProgressSave();
    }

    [RelayCommand]
    public async Task FirstPageAsync()
    {
        if (!HasComic || CurrentPageIndex == 0) return;
        CurrentPageIndex = 0;
        NotifyStateChanged();
        await CheckIsCurrentPageBookmarkedAsync();
        await RenderCurrentPageAsync();
        QueueProgressSave();
    }

    [RelayCommand]
    public async Task LastPageAsync()
    {
        if (!HasComic || CurrentPageIndex >= TotalPages - 1) return;
        CurrentPageIndex = TotalPages - 1;
        NotifyStateChanged();
        await CheckIsCurrentPageBookmarkedAsync();
        await RenderCurrentPageAsync();
        QueueProgressSave();
    }

    public async Task GoToPageAsync(int pageIndex)
    {
        if (!HasComic) return;
        if (pageIndex < 0) pageIndex = 0;
        if (pageIndex >= TotalPages) pageIndex = TotalPages - 1;

        if (pageIndex == CurrentPageIndex) return;

        CurrentPageIndex = pageIndex;
        NotifyStateChanged();
        await CheckIsCurrentPageBookmarkedAsync();
        await RenderCurrentPageAsync();
        QueueProgressSave();
    }

    [RelayCommand]
    public void SetFitMode(FitMode mode)
    {
        FitMode = mode;
        ZoomFactor = 1.0;
        OnPropertyChanged(nameof(ZoomDisplayString));
    }

    [RelayCommand]
    public async Task ToggleViewModeAsync()
    {
        ViewMode = (ViewMode == ViewMode.SinglePage) ? ViewMode.DoublePage : ViewMode.SinglePage;
        if (ViewMode == ViewMode.DoublePage)
        {
            if (CurrentPageIndex % 2 != 0 && CurrentPageIndex > 0)
            {
                CurrentPageIndex--;
            }
        }
        OnPropertyChanged(nameof(IsDoublePageMode));
        OnPropertyChanged(nameof(IsSinglePageMode));
        OnPropertyChanged(nameof(IsWebtoonMode));
        NotifyStateChanged();
        await RenderCurrentPageAsync();
        TriggerOverlayNotification();
    }

    [RelayCommand]
    public async Task ToggleReadingDirectionAsync()
    {
        ReadingDirection = (ReadingDirection == ReadingDirection.LeftToRight)
            ? ReadingDirection.RightToLeft
            : ReadingDirection.LeftToRight;

        OnPropertyChanged(nameof(IsRtlMode));
        OnPropertyChanged(nameof(ReadingDirectionDisplay));

        ShowToast(ReadingDirection == ReadingDirection.RightToLeft
            ? "Manga Mode: Right-to-Left (◀ Next / Prev ▶)"
            : "Standard Mode: Left-to-Right (◀ Prev / Next ▶)");
        await RenderCurrentPageAsync();
    }

    #region Zoom & Window

    [RelayCommand]
    public void ZoomIn()
    {
        ZoomFactor = Math.Min(5.0, Math.Round(ZoomFactor + 0.15, 2));
        OnPropertyChanged(nameof(ZoomDisplayString));
    }

    [RelayCommand]
    public void ZoomOut()
    {
        ZoomFactor = Math.Max(0.25, Math.Round(ZoomFactor - 0.15, 2));
        OnPropertyChanged(nameof(ZoomDisplayString));
    }

    [RelayCommand]
    public void ResetZoom()
    {
        ZoomFactor = 1.0;
        OnPropertyChanged(nameof(ZoomDisplayString));
    }

    [RelayCommand]
    public void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        RequestedTheme = (RequestedTheme == ElementTheme.Dark) ? ElementTheme.Light : ElementTheme.Dark;
    }

    [RelayCommand]
    public async Task ToggleWebtoonModeAsync()
    {
        if (ViewMode == ViewMode.VerticalContinuous)
        {
            ViewMode = ViewMode.SinglePage;
            ShowToast("Switched to Single Page Mode");
        }
        else
        {
            ViewMode = ViewMode.VerticalContinuous;
            ShowToast("Switched to Webtoon Continuous Mode");
            await LoadWebtoonPagesAsync();
        }
        OnPropertyChanged(nameof(IsWebtoonMode));
        OnPropertyChanged(nameof(IsDoublePageMode));
        OnPropertyChanged(nameof(IsSinglePageMode));
        NotifyStateChanged();
        await RenderCurrentPageAsync();
    }

    [RelayCommand]
    public void SetReadingPreset(ReadingPreset preset)
    {
        ColorSettings.Preset = preset;
        ShowToast($"Reading Theme: {preset}");
    }

    [RelayCommand]
    public async Task ToggleOcrLayerAsync()
    {
        if (CurrentComic == null) return;

        if (IsOcrLayerVisible)
        {
            IsOcrLayerVisible = false;
            CurrentPageOcrResult = null;
            return;
        }

        IsOcrLoading = true;
        try
        {
            var pageData = await GetPageDataWithCacheAsync(CurrentPageIndex, CancellationToken.None);
            var ocrRes = await _ocrService.RecognizePageAsync(CurrentPageIndex, pageData);
            CurrentPageOcrResult = ocrRes;
            IsOcrLayerVisible = ocrRes != null && ocrRes.Words.Count > 0;
            if (ocrRes == null || ocrRes.Words.Count == 0)
            {
                ShowToast("No speech or text detected on this page.");
            }
            else
            {
                ShowToast($"Detected {ocrRes.Words.Count} words on page.");
            }
        }
        catch (Exception ex)
        {
            ShowToast($"OCR error: {ex.Message}");
        }
        finally
        {
            IsOcrLoading = false;
        }
    }

    [RelayCommand]
    public async Task SearchInComicOcrAsync(string? query)
    {
        string q = query ?? OcrSearchQuery;
        if (CurrentComic == null || string.IsNullOrWhiteSpace(q)) return;

        IsSearchingOcr = true;
        OcrSearchResults.Clear();
        try
        {
            var matches = await _ocrService.SearchInComicAsync(CurrentComic, q);
            foreach (var m in matches)
            {
                OcrSearchResults.Add(m);
            }
            if (matches.Count == 0)
            {
                ShowToast($"No matches found for '{q}'");
            }
            else
            {
                ShowToast($"Found {matches.Count} matching page(s)");
            }
        }
        catch (Exception ex)
        {
            ShowToast($"OCR search failed: {ex.Message}");
        }
        finally
        {
            IsSearchingOcr = false;
        }
    }

    [RelayCommand]
    public void CopyCurrentPageText()
    {
        if (CurrentPageOcrResult != null && !string.IsNullOrWhiteSpace(CurrentPageOcrResult.FullText))
        {
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(CurrentPageOcrResult.FullText);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            ShowToast("Page text copied to clipboard!");
        }
        else
        {
            ShowToast("No OCR text available to copy.");
        }
    }

    public Task LoadWebtoonPagesAsync()
    {
        if (CurrentComic == null || CurrentComic.PageCount == 0) return Task.CompletedTask;

        _webtoonCts?.Cancel();
        _webtoonCts = new CancellationTokenSource();
        var ct = _webtoonCts.Token;

        WebtoonPages.Clear();
        var allItems = new List<WebtoonPageItem>(CurrentComic.PageCount);
        for (int i = 0; i < CurrentComic.PageCount; i++)
        {
            var item = new WebtoonPageItem
            {
                PageIndex = i,
                IsLoading = true
            };
            allItems.Add(item);
            WebtoonPages.Add(item);
        }

        // Fire parallel asynchronous background pipeline to stream the entire comic rapidly
        _ = Task.Run(async () =>
        {
            try
            {
                using var semaphore = new SemaphoreSlim(6);

                // Prioritize current viewing page, then outward
                int current = CurrentPageIndex;
                var sorted = allItems.OrderBy(x => Math.Abs(x.PageIndex - current)).ToList();

                var tasks = sorted.Select(async item =>
                {
                    if (ct.IsCancellationRequested) return;
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        if (ct.IsCancellationRequested) return;
                        var pageData = await GetPageDataWithCacheAsync(item.PageIndex, ct);
                        if (ct.IsCancellationRequested) return;

                        var dispatcher = App.DispatcherQueue;
                        if (dispatcher != null)
                        {
                            var tcs = new TaskCompletionSource<bool>();
                            dispatcher.TryEnqueue(async () =>
                            {
                                try
                                {
                                    if (!ct.IsCancellationRequested)
                                    {
                                        var source = await ImageHelper.CreateImageSourceAsync(pageData, ColorSettings);
                                        item.Image = source;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Webtoon page {item.PageIndex} UI decode error: {ex.Message}");
                                }
                                finally
                                {
                                    item.IsLoading = false;
                                    tcs.TrySetResult(true);
                                }
                            });
                            await tcs.Task;
                        }
                        else
                        {
                            var source = await ImageHelper.CreateImageSourceAsync(pageData, ColorSettings);
                            item.Image = source;
                            item.IsLoading = false;
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainViewModel] Webtoon page {item.PageIndex} load failed: {ex.Message}");
                        item.IsLoading = false;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Webtoon background pipeline error: {ex.Message}");
            }
        }, ct);

        return Task.CompletedTask;
    }


    [RelayCommand]
    public void DismissError()

    {
        IsError = false;
        ErrorMessage = string.Empty;
    }

    public void ShowError(string message)
    {
        ErrorMessage = message;
        IsError = true;
    }

    public void TriggerOverlayNotification()
    {
        IsPageOverlayVisible = true;
    }

    public void ShowToast(string message, int durationSeconds = 3)
    {
        ToastMessage = message;
        IsToastVisible = true;

        _toastTimer?.Stop();
        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(durationSeconds) };
        _toastTimer.Tick += (s, e) =>
        {
            _toastTimer?.Stop();
            IsToastVisible = false;
        };
        _toastTimer.Start();
    }

    #endregion

    #region Bookmarks

    private async Task LoadBookmarksAsync(long comicId)
    {
        Bookmarks.Clear();
        var list = await _repository.GetBookmarksForComicAsync(comicId);
        foreach (var b in list)
        {
            Bookmarks.Add(b);
        }
    }

    private async Task CheckIsCurrentPageBookmarkedAsync()
    {
        if (_currentComicEntity == null)
        {
            IsCurrentPageBookmarked = false;
            OnPropertyChanged(nameof(BookmarkGlyph));
            return;
        }

        IsCurrentPageBookmarked = Bookmarks.Any(b => b.PageIndex == CurrentPageIndex) ||
                                  await _repository.IsPageBookmarkedAsync(_currentComicEntity.Id, CurrentPageIndex);
        OnPropertyChanged(nameof(BookmarkGlyph));
    }

    [RelayCommand]
    public async Task ToggleBookmarkCurrentPageAsync()
    {
        if (CurrentComic == null) return;

        if (_currentComicEntity == null && !string.IsNullOrEmpty(_currentComicPath))
        {
            _currentComicEntity = await _repository.GetComicByPathAsync(_currentComicPath);
        }

        if (_currentComicEntity == null)
        {
            ShowToast("Cannot bookmark unindexed comic.");
            return;
        }

        int page = CurrentPageIndex;
        long comicId = _currentComicEntity.Id;

        var existing = Bookmarks.FirstOrDefault(b => b.PageIndex == page);
        if (existing != null)
        {
            await _repository.RemoveBookmarkAsync(existing.Id);
            Bookmarks.Remove(existing);
            IsCurrentPageBookmarked = false;
            OnPropertyChanged(nameof(BookmarkGlyph));
            ShowToast($"Bookmark removed for page {page + 1}");
        }
        else
        {
            var created = await _repository.AddBookmarkAsync(comicId, page);
            int insertIndex = 0;
            while (insertIndex < Bookmarks.Count && Bookmarks[insertIndex].PageIndex < page)
            {
                insertIndex++;
            }
            Bookmarks.Insert(insertIndex, created);
            IsCurrentPageBookmarked = true;
            OnPropertyChanged(nameof(BookmarkGlyph));
            ShowToast($"Bookmarked page {page + 1}");
        }
    }

    [RelayCommand]
    public async Task AddBookmarkWithNoteAsync(string? note)
    {
        if (CurrentComic == null || _currentComicEntity == null) return;

        int page = CurrentPageIndex;
        long comicId = _currentComicEntity.Id;

        var existing = Bookmarks.FirstOrDefault(b => b.PageIndex == page);
        if (existing != null)
        {
            await _repository.RemoveBookmarkAsync(existing.Id);
            Bookmarks.Remove(existing);
        }

        var created = await _repository.AddBookmarkAsync(comicId, page, note);
        int insertIndex = 0;
        while (insertIndex < Bookmarks.Count && Bookmarks[insertIndex].PageIndex < page)
        {
            insertIndex++;
        }
        Bookmarks.Insert(insertIndex, created);
        IsCurrentPageBookmarked = true;
        OnPropertyChanged(nameof(BookmarkGlyph));
        ShowToast($"Bookmark saved for page {page + 1}");
    }

    [RelayCommand]
    public async Task RemoveBookmarkAsync(BookmarkEntity? bookmark)
    {
        if (bookmark == null) return;
        await _repository.RemoveBookmarkAsync(bookmark.Id);
        Bookmarks.Remove(bookmark);
        await CheckIsCurrentPageBookmarkedAsync();
        ShowToast($"Bookmark removed for page {bookmark.PageNumber}");
    }

    [RelayCommand]
    public async Task JumpToBookmarkAsync(BookmarkEntity? bookmark)
    {
        if (bookmark == null || !HasComic) return;
        int target = Math.Clamp(bookmark.PageIndex, 0, TotalPages - 1);
        await GoToPageAsync(target);
    }

    #endregion

    #region Reading Progress

    private void QueueProgressSave()
    {
        if (CurrentComic == null || string.IsNullOrEmpty(_currentComicPath)) return;

        _progressSaveCts?.Cancel();
        _progressSaveCts = new CancellationTokenSource();
        var token = _progressSaveCts.Token;

        int page = CurrentPageIndex;
        int total = TotalPages;
        string path = _currentComicPath;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                {
                    await _repository.UpdateReadingProgressAsync(path, page, total);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Progress update error: {ex.Message}");
            }
        }, token);
    }

    public async Task FlushReadingProgressAsync()
    {
        _progressSaveCts?.Cancel();
        if (CurrentComic != null && !string.IsNullOrEmpty(_currentComicPath))
        {
            try
            {
                await _repository.UpdateReadingProgressAsync(_currentComicPath, CurrentPageIndex, TotalPages);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Progress flush error: {ex.Message}");
            }
        }

        // Final save of the active session, then close it (switching comics, leaving the reader or closing the app).
        StopSessionTracking();
        await SaveSessionCheckpointAsync();
        _sessionTracker.Stop();
    }

    partial void OnCurrentPageIndexChanged(int value)
    {
        if (!_sessionTracker.IsActive) return;
        _sessionTracker.RecordPageView(value);
        if (IsDoublePageMode && value + 1 < TotalPages)
        {
            _sessionTracker.RecordPageView(value + 1);
        }
    }

    /// <summary>Counts reading time while the user interacts without turning pages (zooming, panning, scrolling a long page).</summary>
    public void NotifyReadingActivity() => _sessionTracker.RecordActivity();

    private void StartSessionCheckpoints()
    {
        var queue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (queue == null) return;

        if (_sessionCheckpointTimer == null)
        {
            _sessionCheckpointTimer = queue.CreateTimer();
            _sessionCheckpointTimer.Interval = SessionCheckpointInterval;
            _sessionCheckpointTimer.IsRepeating = true;
            _sessionCheckpointTimer.Tick += async (_, _) => await SaveSessionCheckpointAsync();
        }

        _sessionCheckpointTimer.Start();
    }

    /// <summary>Stops periodic checkpoints (the session itself stays open until it is flushed).</summary>
    public void StopSessionTracking() => _sessionCheckpointTimer?.Stop();

    private async Task SaveSessionCheckpointAsync()
    {
        if (!_sessionTracker.HasMeaningfulData) return;

        await _sessionSaveLock.WaitAsync();
        try
        {
            if (!_sessionTracker.HasMeaningfulData) return;
            long id = await _repository.SaveReadingSessionAsync(
                _sessionTracker.SessionId,
                _sessionTracker.ComicId,
                _sessionTracker.StartUtc,
                _sessionTracker.EndUtc,
                _sessionTracker.ActiveSeconds,
                _sessionTracker.PagesRead);
            _sessionTracker.SessionId = id;
            ReadingStatsNotifier.NotifyChanged();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] Session checkpoint error: {ex.Message}");
        }
        finally
        {
            _sessionSaveLock.Release();
        }
    }


    #endregion

    #region Page Rendering

    private async Task RenderCurrentPageAsync()
    {
        if (CurrentComic == null || CurrentComic.PageCount == 0)
        {
            CurrentPageImage = null;
            SecondPageImage = null;
            return;
        }

        _pageLoadCts?.Cancel();
        _pageLoadCts = new CancellationTokenSource();
        var ct = _pageLoadCts.Token;

        try
        {
            var primaryData = await GetPageDataWithCacheAsync(CurrentPageIndex, ct);
            if (ct.IsCancellationRequested) return;

            var primarySource = await ImageHelper.CreateImageSourceAsync(primaryData, ColorSettings);

            // Double Page Spread logic:
            if (ViewMode == ViewMode.DoublePage && CurrentPageIndex + 1 < TotalPages)
            {
                var secondaryData = await GetPageDataWithCacheAsync(CurrentPageIndex + 1, ct);
                if (ct.IsCancellationRequested) return;

                var secondarySource = await ImageHelper.CreateImageSourceAsync(secondaryData, ColorSettings);

                if (ReadingDirection == ReadingDirection.LeftToRight)
                {
                    CurrentPageImage = primarySource;
                    SecondPageImage = secondarySource;
                }
                else
                {
                    // Manga / Right-to-Left: primary page is on the right, secondary on the left
                    CurrentPageImage = secondarySource;
                    SecondPageImage = primarySource;
                }
            }
            else
            {
                // Single page or Cover or lone final page
                CurrentPageImage = primarySource;
                SecondPageImage = null;
            }

            // Prefetch adjacent pages in background
            _ = PrefetchAdjacentPagesAsync(CurrentPageIndex, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ShowError($"Failed to render page {CurrentPageIndex + 1}: {ex.Message}");
        }
    }

    private async Task<ComicPageData> GetPageDataWithCacheAsync(int index, CancellationToken ct)
    {
        if (_pageCache.TryGetValue(index, out var cached))
        {
            return cached;
        }

        if (CurrentComic == null || index < 0 || index >= CurrentComic.PageCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var page = CurrentComic.Pages[index];
        var data = await page.GetPageDataAsync(ct);

        AddToCache(index, data);
        return data;
    }

    private void AddToCache(int index, ComicPageData data)
    {
        _pageCache[index] = data;
        _cacheEvictionQueue.Enqueue(index);

        while (_cacheEvictionQueue.Count > MaxCacheEntries)
        {
            int evictIndex = _cacheEvictionQueue.Dequeue();
            if (evictIndex != CurrentPageIndex &&
                !(ViewMode == ViewMode.DoublePage && evictIndex == CurrentPageIndex + 1))
            {
                _pageCache.TryRemove(evictIndex, out _);
            }
        }
    }

    private async Task PrefetchAdjacentPagesAsync(int currentIndex, CancellationToken ct)
    {
        if (CurrentComic == null) return;

        int[] prefetchIndices = { currentIndex + 1, currentIndex + 2, currentIndex - 1 };
        foreach (var idx in prefetchIndices)
        {
            if (ct.IsCancellationRequested) break;
            if (idx >= 0 && idx < CurrentComic.PageCount && !_pageCache.ContainsKey(idx))
            {
                try
                {
                    await GetPageDataWithCacheAsync(idx, ct);
                }
                catch { }
            }
        }
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasComic));
        OnPropertyChanged(nameof(HasNoComic));
        OnPropertyChanged(nameof(ComicTitle));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(PageDisplayString));
        OnPropertyChanged(nameof(CurrentPageNumber));
    }

    #endregion
}
