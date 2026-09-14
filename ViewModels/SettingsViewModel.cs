using System;
using System.Collections.ObjectModel;
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

    [ObservableProperty]
    private int _sortOptionIndex; // 0..6 (TitleAsc, TitleDesc, LastReadDesc, DateAddedDesc, DateAddedAsc, PageCountDesc, FileSizeDesc)

    [ObservableProperty]
    private double _brightness;

    [ObservableProperty]
    private double _contrast = 1.0;

    [ObservableProperty]
    private double _warmth;

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

        Brightness = settings.DefaultBrightness;
        Contrast = settings.DefaultContrast;
        Warmth = settings.DefaultWarmth;
        await RefreshWatchedFoldersAsync();
        RefreshCacheSize();
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

        var settings = new AppSettings
        {
            Theme = theme,
            DefaultFitMode = fit,
            DefaultReadingDirection = dir,
            DefaultViewMode = view,
            DefaultSortOption = SortOptionIndex,
            DefaultReadingPreset = preset,
            DefaultBrightness = Brightness,
            DefaultContrast = Contrast,
            DefaultWarmth = Warmth,
            DefaultNightMode = (ReadingPresetIndex == 1)
        };

        await _repository.SaveAppSettingsAsync(settings);
        ApplyThemeOverride(theme);
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
