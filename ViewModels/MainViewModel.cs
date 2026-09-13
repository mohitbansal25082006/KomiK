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

    public ColorCorrectionSettings ColorSettings { get; } = new();

    public ObservableCollection<BookmarkEntity> Bookmarks { get; } = new();

    public MainViewModel(
        IComicLoaderService? loaderService = null,
        IFilePickerService? pickerService = null,
        ILibraryRepository? repository = null)
    {
        _loaderService = loaderService ?? new ComicLoaderService();
        _pickerService = pickerService ?? new FilePickerService();
        _repository = repository ?? new LibraryRepository();

        ColorSettings.PropertyChanged += (s, e) =>
        {
            if (HasComic)
            {
                _ = RenderCurrentPageAsync();
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

            if (settings.DefaultNightMode)
            {
                ColorSettings.IsNightMode = true;
                ColorSettings.Brightness = settings.DefaultBrightness != 0 ? settings.DefaultBrightness : -25;
                ColorSettings.Contrast = Math.Abs(settings.DefaultContrast - 1.0) > 0.05 ? settings.DefaultContrast : 1.1;
                ColorSettings.Warmth = settings.DefaultWarmth != 0 ? settings.DefaultWarmth : 15;
            }
            else
            {
                ColorSettings.IsNightMode = false;
                ColorSettings.Brightness = settings.DefaultBrightness;
                ColorSettings.Contrast = settings.DefaultContrast;
                ColorSettings.Warmth = settings.DefaultWarmth;
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

    public bool IsRtlMode => ReadingDirection == ReadingDirection.RightToLeft;

    public double CurrentPageNumber => CurrentPageIndex + 1;

    public string BookmarkGlyph => IsCurrentPageBookmarked ? "\uE8A4" : "\uE74E";

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

            NotifyStateChanged();
            await CheckIsCurrentPageBookmarkedAsync();
            await RenderCurrentPageAsync();

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
