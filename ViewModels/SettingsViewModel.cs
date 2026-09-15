using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Komik.Models;
using Komik.Services;

namespace Komik.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ILibraryRepository _repository;
    private readonly IThumbnailService _thumbnailService;
    private readonly IFilePickerService _pickerService;
    private readonly ILibraryScannerService _scannerService;
    private readonly IComicLoaderService _loaderService;

    [ObservableProperty]
    private int _themeIndex; // 0: System/Default, 1: Light, 2: Dark

    [ObservableProperty]
    private int _fitModeIndex; // 0: Fit to Width, 1: Fit to Height, 2: Actual Size

    [ObservableProperty]
    private int _readingDirectionIndex; // 0: Left to Right, 1: Right to Left (Manga)

    [ObservableProperty]
    private int _viewModeIndex; // 0: Grid, 1: List

    /// <summary>How the reader opens every comic: 0 page by page, 1 two-page spread, 2 webtoon strip.</summary>
    [ObservableProperty]
    private int _readerViewModeIndex;

    [ObservableProperty]
    private int _sortOptionIndex; // 0..6 (TitleAsc, TitleDesc, LastReadDesc, DateAddedDesc, DateAddedAsc, PageCountDesc, FileSizeDesc)

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrightnessDisplay))]
    private double _brightness;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContrastPercent))]
    [NotifyPropertyChangedFor(nameof(ContrastDisplay))]
    private double _contrast = 1.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WarmthDisplay))]
    private double _warmth;

    /// <summary>The contrast slider works in percent (50 to 200); the reader uses a factor (0.5 to 2.0).</summary>
    public double ContrastPercent
    {
        get => Math.Round(Contrast * 100);
        set
        {
            double factor = Math.Round(Math.Clamp(value, 50, 200)) / 100.0;
            if (Math.Abs(factor - Contrast) > 0.001) Contrast = factor;
        }
    }

    public string BrightnessDisplay => Signed(Brightness);
    public string WarmthDisplay => Signed(Warmth);
    public string ContrastDisplay => $"{Math.Round(Contrast * 100)}%";

    private static string Signed(double value)
    {
        int v = (int)Math.Round(value);
        return v > 0 ? $"+{v}" : v.ToString(System.Globalization.CultureInfo.CurrentCulture);
    }

    partial void OnBrightnessChanged(double value) => QueuePreview();
    partial void OnContrastChanged(double value) => QueuePreview();
    partial void OnWarmthChanged(double value) => QueuePreview();

    /// <summary>A real cover from the library, drawn with the chosen page colors.</summary>
    [ObservableProperty]
    private Microsoft.UI.Xaml.Media.ImageSource? _colorPreviewImage;

    [ObservableProperty]
    private bool _hasColorPreview;

    private byte[]? _previewBytes;
    private CancellationTokenSource? _previewCts;

    private async Task LoadPreviewSourceAsync()
    {
        try
        {
            var comics = await _repository.GetComicsAsync();
            var withCover = comics.Where(c => !string.IsNullOrEmpty(c.ThumbnailPath) && File.Exists(c.ThumbnailPath)).ToList();
            if (withCover.Count == 0) return;
            var pick = withCover.OrderByDescending(c => c.LastReadAt ?? DateTime.MinValue).First();
            _previewBytes = await File.ReadAllBytesAsync(pick.ThumbnailPath!);
            QueuePreview();
        }
        catch
        {
            _previewBytes = null;
        }
    }

    private void QueuePreview()
    {
        if (_previewBytes == null) return;
        _previewCts?.Cancel();
        var cts = _previewCts = new CancellationTokenSource();
        App.DispatcherQueue?.TryEnqueue(async () =>
        {
            try
            {
                await Task.Delay(60, cts.Token);
                var colors = new ColorCorrectionSettings();
                colors.ApplyPreset(PresetFromIndex(ReadingPresetIndex));
                colors.Brightness = Brightness;
                colors.Contrast = Contrast;
                colors.Warmth = Warmth;
                var image = await Helpers.ImageHelper.CreateImageSourceAsync(new ComicPageData(_previewBytes), colors);
                if (!cts.IsCancellationRequested)
                {
                    ColorPreviewImage = image;
                    HasColorPreview = true;
                }
            }
            catch (OperationCanceledException) { }
            catch { }
        });
    }

    private static ReadingPreset PresetFromIndex(int index) => index switch
    {
        1 => ReadingPreset.NightMode,
        2 => ReadingPreset.Sepia,
        3 => ReadingPreset.HighContrast,
        4 => ReadingPreset.Grayscale,
        5 => ReadingPreset.Inverted,
        _ => ReadingPreset.Original
    };

    [ObservableProperty]
    private int _readingPresetIndex; // 0: Original, 1: NightMode, 2: Sepia, 3: HighContrast, 4: Grayscale, 5: Inverted

    partial void OnReadingPresetIndexChanged(int value)
    {
        switch (value)
        {
            case 1: // Night Mode
                Brightness = -25;
                Contrast = 1.1;
                Warmth = 18;
                break;
            case 2: // Sepia
                Brightness = -10;
                Contrast = 1.05;
                Warmth = 38;
                break;
            case 3: // High Contrast
                Brightness = 5;
                Contrast = 1.55;
                Warmth = 0;
                break;
            case 4: // Grayscale
                Brightness = 0;
                Contrast = 1.1;
                Warmth = 0;
                break;
            case 5: // Inverted
                Brightness = 0;
                Contrast = 1.0;
                Warmth = 0;
                break;
            case 0: // Original
            default:
                Brightness = 0;
                Contrast = 1.0;
                Warmth = 0;
                break;
        }
        QueuePreview();
    }

    [ObservableProperty]
    private string _thumbnailCacheDirectory = string.Empty;

    [ObservableProperty]
    private string _thumbnailCacheSizeText = "Calculating...";

    [ObservableProperty]
    private ObservableCollection<WatchedFolder> _watchedFolders = new();

    [ObservableProperty]
    private bool _isNotificationOpen;

    [ObservableProperty]
    private string _notificationTitle = string.Empty;

    [ObservableProperty]
    private string _notificationMessage = string.Empty;

    [ObservableProperty]
    private InfoBarSeverity _notificationSeverity = InfoBarSeverity.Informational;

    [ObservableProperty]
    private bool _isCaching;

    [ObservableProperty]
    private bool _isNotCaching = true;

    [ObservableProperty]
    private double _cachingProgress;

    [ObservableProperty]
    private double _cachingTotal = 1.0;

    [ObservableProperty]
    private bool _isCachingIndeterminate;

    [ObservableProperty]
    private string _cachingStatusText = string.Empty;

    public SettingsViewModel(
        ILibraryRepository? repository = null,
        IThumbnailService? thumbnailService = null,
        IFilePickerService? pickerService = null,
        ILibraryScannerService? scannerService = null,
        IComicLoaderService? loaderService = null)
    {
        _repository = repository ?? new LibraryRepository();
        _thumbnailService = thumbnailService ?? new ThumbnailService();
        _pickerService = pickerService ?? new FilePickerService();
        _scannerService = scannerService ?? new LibraryScannerService(_repository, thumbnailService: _thumbnailService);
        _loaderService = loaderService ?? new ComicLoaderService();

        ThumbnailCacheDirectory = _thumbnailService.ThumbnailDirectory;
    }

    public async Task InitializeAsync()
    {
        await _repository.InitializeAsync();
        var settings = await _repository.GetAppSettingsAsync();

        ThemeIndex = settings.Theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };

        FitModeIndex = settings.DefaultFitMode switch
        {
            "FitToWidth" => 0,
            "ActualSize" => 2,
            _ => 1
        };

        ReadingDirectionIndex = settings.DefaultReadingDirection switch
        {
            "RightToLeft" => 1,
            _ => 0
        };

        ViewModeIndex = settings.DefaultViewMode == "List" ? 1 : 0;
        ReaderViewModeIndex = settings.DefaultReaderViewMode switch
        {
            "DoublePage" => 1,
            "Webtoon" => 2,
            _ => 0
        };
        SortOptionIndex = Math.Clamp(settings.DefaultSortOption, 0, 6);

        ReadingPresetIndex = settings.DefaultReadingPreset switch
        {
            "NightMode" => 1,
            "Sepia" => 2,
            "HighContrast" => 3,
            "Grayscale" => 4,
            "Inverted" => 5,
            _ => 0
        };

        // Stored numbers win over the preset's own values (the preset setter above resets them).
        Brightness = Math.Clamp(settings.DefaultBrightness, -100, 100);
        Contrast = Math.Clamp(settings.DefaultContrast > 3 ? settings.DefaultContrast / 100.0 : settings.DefaultContrast, 0.5, 2.0);
        Warmth = Math.Clamp(settings.DefaultWarmth, -100, 100);
        await RefreshWatchedFoldersAsync();
        RefreshCacheSize();
        _ = LoadPreviewSourceAsync();
    }

    public async Task SaveSettingsAsync()
    {
        string theme = ThemeIndex switch
        {
            1 => "Light",
            2 => "Dark",
            _ => "Default"
        };

        string fit = FitModeIndex switch
        {
            1 => "FitToHeight",
            2 => "ActualSize",
            _ => "FitToWidth"
        };

        string dir = ReadingDirectionIndex == 1 ? "RightToLeft" : "LeftToRight";
        string view = ViewModeIndex == 1 ? "List" : "Grid";
        string preset = ReadingPresetIndex switch
        {
            1 => "NightMode",
            2 => "Sepia",
            3 => "HighContrast",
            4 => "Grayscale",
            5 => "Inverted",
            _ => "Original"
        };

        // Start from what is stored so window size, position and other keys are never reset.
        var settings = await _repository.GetAppSettingsAsync();
        settings.Theme = theme;
        settings.DefaultFitMode = fit;
        settings.DefaultReadingDirection = dir;
        settings.DefaultReaderViewMode = ReaderViewModeIndex switch { 1 => "DoublePage", 2 => "Webtoon", _ => "SinglePage" };
        settings.DefaultViewMode = view;
        settings.DefaultSortOption = SortOptionIndex;
        settings.DefaultReadingPreset = preset;
        settings.DefaultBrightness = Math.Round(Brightness);
        settings.DefaultContrast = Math.Round(Contrast, 2);
        settings.DefaultWarmth = Math.Round(Warmth);
        settings.DefaultNightMode = ReadingPresetIndex == 1;

        await _repository.SaveAppSettingsAsync(settings);
        ApplyThemeOverride(theme);
        LibraryViewModel.NotifySettingsChanged();
        SettingsSavedPulse++;
    }

    /// <summary>Changes each time settings are saved (drives the small "Saved" badge).</summary>
    [ObservableProperty]
    private int _settingsSavedPulse;

    // ───────────── Comics in watched folders that aren't in the library ─────────────

    private readonly List<UnindexedComicItem> _allUnindexed = new();
    public ObservableCollection<UnindexedComicItem> UnindexedComics { get; } = new();
    public List<string> UnindexedScopes { get; } = new() { "All", "Removed earlier", "Never added" };

    [ObservableProperty]
    private string _unindexedScope = "All";

    [ObservableProperty]
    private string _unindexedSearchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotFindingUnindexed))]
    private bool _isFindingUnindexed;

    [ObservableProperty]
    private bool _hasSearchedUnindexed;

    [ObservableProperty]
    private string _unindexedStatusText = "Look through your watched folders for comics that aren't in the library.";

    [ObservableProperty]
    private double _unindexedProgress;

    [ObservableProperty]
    private double _unindexedProgressTotal = 1;

    public bool IsNotFindingUnindexed => !IsFindingUnindexed;
    public bool HasUnindexedComics => UnindexedComics.Count > 0;
    public bool ShowUnindexedEmpty => HasSearchedUnindexed && !IsFindingUnindexed && UnindexedComics.Count == 0;
    public int SelectedUnindexedCount => _allUnindexed.Count(i => i.IsSelected);
    public string AddUnindexedText => SelectedUnindexedCount == 1 ? "Add 1 comic" : $"Add {SelectedUnindexedCount} comics";
    public string UnindexedEmptyText => _allUnindexed.Count == 0
        ? "Every comic in your watched folders is already in the library."
        : "Nothing matches your search or filter.";

    partial void OnUnindexedScopeChanged(string value) => ApplyUnindexedFilter();
    partial void OnUnindexedSearchTextChanged(string value) => ApplyUnindexedFilter();
    partial void OnHasSearchedUnindexedChanged(bool value) => OnPropertyChanged(nameof(ShowUnindexedEmpty));

    private void ApplyUnindexedFilter()
    {
        string q = UnindexedSearchText.Trim();
        UnindexedComics.Clear();
        foreach (var item in _allUnindexed.Where(i =>
                     (q.Length == 0 || i.FileName.Contains(q, StringComparison.OrdinalIgnoreCase) || i.FolderName.Contains(q, StringComparison.OrdinalIgnoreCase))
                     && UnindexedScope switch { "Removed earlier" => i.WasRemoved, "Never added" => i.IsNew, _ => true }))
        {
            UnindexedComics.Add(item);
        }
        NotifyUnindexed();
    }

    private void NotifyUnindexed()
    {
        OnPropertyChanged(nameof(HasUnindexedComics));
        OnPropertyChanged(nameof(ShowUnindexedEmpty));
        OnPropertyChanged(nameof(SelectedUnindexedCount));
        OnPropertyChanged(nameof(AddUnindexedText));
        OnPropertyChanged(nameof(UnindexedEmptyText));
    }

    [RelayCommand]
    public async Task FindUnindexedComicsAsync()
    {
        if (IsFindingUnindexed) return;
        IsFindingUnindexed = true;
        UnindexedStatusText = "Looking through your watched folders...";
        try
        {
            var folders = await _repository.GetWatchedFoldersAsync();
            if (folders.Count == 0)
            {
                UnindexedStatusText = "Add a watched folder first.";
                _allUnindexed.Clear();
                ApplyUnindexedFilter();
                return;
            }

            var sources = await _scannerService.FindComicSourcesAsync(folders.Select(f => f.Path));
            var inLibrary = new HashSet<string>((await _repository.GetComicsAsync()).Select(c => c.FilePath), StringComparer.OrdinalIgnoreCase);
            var removed = await _repository.GetRemovedComicPathsAsync();

            foreach (var item in _allUnindexed) item.PropertyChanged -= Unindexed_PropertyChanged;
            _allUnindexed.Clear();
            foreach (var (path, format) in sources.Where(s => !inLibrary.Contains(s.Path)).OrderBy(s => s.Path, StringComparer.OrdinalIgnoreCase))
            {
                string? thumb = null;
                try
                {
                    thumb = new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp" }
                        .Select(ext => _thumbnailService.GetThumbnailPathForComic(path, ext))
                        .FirstOrDefault(File.Exists);
                }
                catch { }
                var item = new UnindexedComicItem(path, format, removed.Contains(path), thumb);
                item.PropertyChanged += Unindexed_PropertyChanged;
                _allUnindexed.Add(item);
            }

            int removedCount = _allUnindexed.Count(i => i.WasRemoved);
            UnindexedStatusText = _allUnindexed.Count == 0
                ? $"All {sources.Count} comics in your watched folders are in the library."
                : $"Found {_allUnindexed.Count} comic{(_allUnindexed.Count == 1 ? "" : "s")} not in the library ({removedCount} removed earlier, {_allUnindexed.Count - removedCount} never added).";
            HasSearchedUnindexed = true;
            ApplyUnindexedFilter();
        }
        catch (Exception ex)
        {
            UnindexedStatusText = $"Couldn't look through the folders: {ex.Message}";
        }
        finally
        {
            IsFindingUnindexed = false;
            OnPropertyChanged(nameof(ShowUnindexedEmpty));
        }
    }

    private void Unindexed_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UnindexedComicItem.IsSelected))
        {
            OnPropertyChanged(nameof(SelectedUnindexedCount));
            OnPropertyChanged(nameof(AddUnindexedText));
        }
    }

    [RelayCommand]
    public void SelectAllUnindexed(string? select)
    {
        bool value = !bool.TryParse(select, out bool parsed) || parsed;
        foreach (var item in UnindexedComics) item.IsSelected = value;
        NotifyUnindexed();
    }

    [RelayCommand]
    public async Task AddSelectedUnindexedAsync()
    {
        var chosen = _allUnindexed.Where(i => i.IsSelected).ToList();
        if (chosen.Count == 0 || IsFindingUnindexed) return;

        IsFindingUnindexed = true;
        UnindexedProgress = 0;
        UnindexedProgressTotal = chosen.Count;
        UnindexedStatusText = $"Adding {chosen.Count} comic{(chosen.Count == 1 ? "" : "s")}...";
        try
        {
            var progress = new Progress<int>(done =>
            {
                UnindexedProgress = done;
                UnindexedStatusText = $"Adding comics ({done}/{chosen.Count})...";
            });
            int added = await Task.Run(() => _scannerService.IndexComicsAsync(chosen.Select(c => (c.Path, c.Format)).ToList(), progress));

            foreach (var item in chosen)
            {
                item.PropertyChanged -= Unindexed_PropertyChanged;
                _allUnindexed.Remove(item);
            }
            ApplyUnindexedFilter();
            LibraryViewModel.NotifyLibraryThumbnailsChanged();
            RefreshCacheSize();
            int failed = chosen.Count - added;
            UnindexedStatusText = failed > 0 ? $"Added {added}. {failed} couldn't be opened." : $"Added {added} comic{(added == 1 ? "" : "s")} back to the library.";
            ShowNotification("Comics Added", failed > 0
                    ? $"Added {added} comic{(added == 1 ? "" : "s")}. {failed} couldn't be opened and were skipped."
                    : $"Added {added} comic{(added == 1 ? "" : "s")} to your library.",
                failed > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Couldn't Add Comics", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsFindingUnindexed = false;
            UnindexedProgress = 0;
        }
    }

    [RelayCommand]
    public async Task ExportBackupAsync()
    {
        try
        {
            string defaultName = $"Komik_Backup_{DateTime.Now:yyyyMMdd_HHmmss}";
            string? filePath = await _pickerService.PickSaveBackupFileAsync(defaultName);
            if (string.IsNullOrWhiteSpace(filePath)) return;

            string json = await _repository.ExportLibraryBackupJsonAsync();
            await File.WriteAllTextAsync(filePath, json);
            ShowNotification("Backup Exported", $"Library backup successfully saved to '{Path.GetFileName(filePath)}'.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Backup Export Failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    public async Task ImportBackupAsync()
    {
        try
        {
            string? filePath = await _pickerService.PickOpenBackupFileAsync();
            if (string.IsNullOrWhiteSpace(filePath)) return;

            string json = await File.ReadAllTextAsync(filePath);
            var (comicsRestored, bookmarksRestored, tagsRestored) = await _repository.ImportLibraryBackupJsonAsync(json, overwriteExisting: true);
            RefreshCacheSize();
            LibraryViewModel.NotifyLibraryThumbnailsChanged();
            ShowNotification("Backup Restored", $"Successfully restored {comicsRestored} comics, {bookmarksRestored} bookmarks, and {tagsRestored} tags from backup.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Backup Restore Failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    public void ApplyThemeOverride(string theme)
    {
        var elementTheme = theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        MainWindow.Instance?.SetTheme(theme);
        if (App.Window?.Content is FrameworkElement root)
        {
            root.RequestedTheme = elementTheme;
        }
    }

    [RelayCommand]
    public void ResetColorSettings()
    {
        Brightness = 0.0;
        Contrast = 1.0;
        Warmth = 0.0;
        ReadingPresetIndex = 0;
        _ = SaveSettingsAsync();
    }

    [RelayCommand]
    public async Task AddFolderAsync()
    {
        string? folder = await _pickerService.PickComicFolderAsync();
        if (string.IsNullOrWhiteSpace(folder)) return;

        await _repository.AddWatchedFolderAsync(folder);
        await RefreshWatchedFoldersAsync();
        ShowNotification("Scanning Folder", $"Scanning '{folder}' for comics...", InfoBarSeverity.Informational);

        _ = Task.Run(async () =>
        {
            try
            {
                await _scannerService.ScanFolderAsync(folder);
                App.DispatcherQueue?.TryEnqueue(() =>
                {
                    RefreshCacheSize();
                    ShowNotification("Scan Complete", $"Finished indexing comics from '{Path.GetFileName(folder)}'.", InfoBarSeverity.Success);
                });
            }
            catch (Exception ex)
            {
                App.DispatcherQueue?.TryEnqueue(() =>
                {
                    ShowNotification("Scan Error", ex.Message, InfoBarSeverity.Error);
                });
            }
        });
    }

    [RelayCommand]
    public async Task RemoveFolderAsync(WatchedFolder folder)
    {
        if (folder == null) return;
        await _repository.RemoveComicsInWatchedFolderAsync(folder.Path);
        await _repository.RemoveWatchedFolderAsync(folder.Id);
        WatchedFolders.Remove(folder);
        RefreshCacheSize();
        ShowNotification("Folder Removed", $"Removed '{folder.Path}' and its indexed comics from library.", InfoBarSeverity.Informational);
    }

    [RelayCommand]
    public void OpenCacheFolder()
    {
        try
        {
            if (Directory.Exists(ThumbnailCacheDirectory))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", ThumbnailCacheDirectory) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            ShowNotification("Explorer Error", ex.Message, InfoBarSeverity.Error);
        }
    }

    public async Task ClearThumbnailCacheAsync()
    {
        try
        {
            _thumbnailService.ClearCache();
            await _repository.ClearAllComicThumbnailsAsync();
            RefreshCacheSize();
            LibraryViewModel.NotifyLibraryThumbnailsChanged();
            ShowNotification("Cache Cleared", "Cover thumbnail cache has been cleared.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Clear Cache Error", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    public async Task ClearThumbnailCache()
    {
        await ClearThumbnailCacheAsync();
    }

    public async Task CacheAllCoversAsync(CancellationToken cancellationToken = default)
    {
        if (IsCaching) return;

        try
        {
            IsCaching = true;
            IsNotCaching = false;
            IsCachingIndeterminate = true;
            CachingStatusText = "Reading library comics...";

            var comics = await _repository.GetComicsAsync();
            if (comics.Count == 0)
            {
                ShowNotification("Library Empty", "No comics found in library to cache.", InfoBarSeverity.Informational);
                return;
            }

            CachingTotal = comics.Count;
            CachingProgress = 0;
            IsCachingIndeterminate = false;

            int processed = 0;
            int cachedCount = 0;

            foreach (var comic in comics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;
                CachingProgress = processed;
                CachingStatusText = $"Caching covers ({processed}/{comics.Count}): {comic.Title}";

                try
                {
                    bool fileExists = comic.Format == ComicSourceType.Folder
                        ? Directory.Exists(comic.FilePath)
                        : File.Exists(comic.FilePath);

                    if (!fileExists)
                    {
                        continue;
                    }

                    using var comicObj = await _loaderService.LoadComicAsync(comic.FilePath, cancellationToken);
                    if (comicObj.PageCount > 0)
                    {
                        var firstPageData = await comicObj.Pages[0].GetPageDataAsync(cancellationToken);
                        string? thumbPath = await _thumbnailService.SaveThumbnailAsync(comic.FilePath, firstPageData);
                        if (!string.IsNullOrEmpty(thumbPath))
                        {
                            comic.ThumbnailPath = thumbPath;
                            await _repository.UpdateThumbnailPathAsync(comic.Id, thumbPath);
                            cachedCount++;
                        }
                    }
                }
                catch
                {
                    // Continue caching remaining comics even if one archive has issues
                }
            }

            RefreshCacheSize();
            LibraryViewModel.NotifyLibraryThumbnailsChanged();
            ShowNotification(
                "Covers Cached",
                $"Cover thumbnails successfully generated and cached for {cachedCount} comic{(cachedCount == 1 ? "" : "s")}.",
                InfoBarSeverity.Success);
        }
        catch (OperationCanceledException)
        {
            ShowNotification("Caching Stopped", "Cover caching was cancelled.", InfoBarSeverity.Warning);
        }
        catch (Exception ex)
        {
            ShowNotification("Caching Error", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsCaching = false;
            IsNotCaching = true;
            IsCachingIndeterminate = false;
            CachingStatusText = string.Empty;
            RefreshCacheSize();
        }
    }

    public async Task RefreshWatchedFoldersAsync()
    {
        var list = await _repository.GetWatchedFoldersAsync();
        WatchedFolders.Clear();
        foreach (var item in list)
        {
            WatchedFolders.Add(item);
        }
    }

    public void RefreshCacheSize()
    {
        long bytes = _thumbnailService.GetCacheSizeBytes();
        if (bytes < 1024)
        {
            ThumbnailCacheSizeText = $"{bytes} B";
        }
        else if (bytes < 1024 * 1024)
        {
            ThumbnailCacheSizeText = $"{bytes / 1024.0:F1} KB";
        }
        else
        {
            ThumbnailCacheSizeText = $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
    }

    private void ShowNotification(string title, string message, InfoBarSeverity severity)
    {
        NotificationTitle = title;
        NotificationMessage = message;
        NotificationSeverity = severity;
        IsNotificationOpen = true;
    }
}
