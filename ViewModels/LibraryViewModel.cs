using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Komik.Helpers;
using Komik.Models;
using Komik.Services;


namespace Komik.ViewModels;

public sealed class SortOptionItem
{
    public string DisplayName { get; }
    public LibrarySortOption Option { get; }

    public SortOptionItem(string displayName, LibrarySortOption option)
    {
        DisplayName = displayName;
        Option = option;
    }
}

public partial class LibraryViewModel : ObservableObject
{
    private readonly ILibraryRepository _repository;
    private readonly ILibraryScannerService _scannerService;
    private readonly IFilePickerService _pickerService;
    private readonly IFormatConversionService _conversionService;
    private readonly IDuplicateDetectionService _duplicateService;
    private readonly ISeriesDetectionService _seriesService = new SeriesDetectionService();
    private const string IgnoredSeriesSettingKey = "series.ignored_keys";
    private HashSet<string> _ignoredSeriesKeys = new(StringComparer.Ordinal);
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _statsRefreshTimer;
    private bool _statsRefreshQueued;
    private int _seriesUpdateVersion;
    private int _duplicateScanVersion;

    private CancellationTokenSource? _searchDebounceCts;
    private CancellationTokenSource? _conversionCts;

    public event Action<string>? ComicSelectedForReading;

    [ObservableProperty]
    private ObservableCollection<ComicEntity> _comics = new();

    public ObservableCollection<ComicSeriesGroup> SeriesGroups { get; } = new();

    [ObservableProperty]
    private bool _isSeriesView;

    partial void OnIsSeriesViewChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowComicsGrid));
        OnPropertyChanged(nameof(ShowComicsList));
        OnPropertyChanged(nameof(ShowSeriesGrid));
        OnPropertyChanged(nameof(ShowEmptyFilter));
        OnPropertyChanged(nameof(EmptyFilterTitle));
        OnPropertyChanged(nameof(EmptyFilterSubtitle));
        OnPropertyChanged(nameof(EmptyFilterGlyph));
    }

    [ObservableProperty]
    private ComicSeriesGroup? _selectedSeriesGroup;

    [ObservableProperty]
    private bool _isSeriesDetailOpen;

    [ObservableProperty]
    private bool _isCreateSeriesDialogOpen;

    [ObservableProperty]
    private bool _isAddComicsToSeriesDialogOpen;

    [ObservableProperty]
    private string _seriesSearchText = string.Empty;

    public string SeriesGroupsCountDisplay => IsCreatorSection
        ? (SeriesGroups.Count == 1 ? "1 Creator" : $"{SeriesGroups.Count} Creators")
        : (SeriesGroups.Count == 1 ? "1 Series" : $"{SeriesGroups.Count} Series");

    /// <summary>Series page sections: story continuations, or everything grouped by author/artist.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStorySection))]
    [NotifyPropertyChangedFor(nameof(SeriesGroupsCountDisplay))]
    private bool _isCreatorSection;

    public bool IsStorySection => !IsCreatorSection;

    [ObservableProperty]
    private int _storySeriesCount;

    [ObservableProperty]
    private int _creatorGroupCount;

    partial void OnIsCreatorSectionChanged(bool value)
    {
        OnPropertyChanged(nameof(NewGroupButtonText));
        _ = UpdateSeriesGroupsAsync();
    }

    [RelayCommand]
    public void ShowStorySeries() => IsCreatorSection = false;

    [RelayCommand]
    public void ShowCreatorGroups() => IsCreatorSection = true;

    partial void OnSeriesSearchTextChanged(string value)
    {
        _ = UpdateSeriesGroupsAsync();
    }

    [ObservableProperty]
    private string _newSeriesName = string.Empty;

    [ObservableProperty]
    private string _manualSeriesSearchQuery = string.Empty;

    [ObservableProperty]
    private int _selectedCandidateCount;

    public ObservableCollection<ManualSeriesComicItem> ManualSeriesCandidates { get; } = new();

    partial void OnManualSeriesSearchQueryChanged(string value)
    {
        string query = value?.Trim() ?? string.Empty;
        foreach (var item in ManualSeriesCandidates)
        {
            item.IsVisible = string.IsNullOrWhiteSpace(query) ||
                             (item.Comic.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || item.Caption.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
    }

    public ObservableCollection<DuplicateComicGroup> DuplicateGroups { get; } = new();

    [ObservableProperty]
    private bool _isDuplicateManagerOpen;

    [ObservableProperty]
    private bool _isScanningDuplicates;

    public int DuplicateCount => DuplicateGroups.Count;
    public bool HasDuplicates => DuplicateGroups.Count > 0;
    public int DuplicateExtraCopies => DuplicateGroups.Sum(g => Math.Max(0, g.CopyCount - 1));
    public string DuplicateSummaryDisplay => DuplicateGroups.Count == 0
        ? "No duplicates"
        : $"{DuplicateGroups.Count} group{(DuplicateGroups.Count == 1 ? "" : "s")} · {DuplicateExtraCopies} extra cop{(DuplicateExtraCopies == 1 ? "y" : "ies")} · {DuplicateComicGroup.FormatBytes(DuplicateGroups.Sum(g => g.ReclaimableBytes))} reclaimable";

    public List<string> SeriesStatusFilters { get; } = new() { "All Series", "Reading", "Not Started", "Caught Up", "Has Gaps" };

    [ObservableProperty]
    private string _selectedSeriesStatusFilter = "All Series";

    partial void OnSelectedSeriesStatusFilterChanged(string value)
    {
        _ = UpdateSeriesGroupsAsync();
    }

    public int HiddenSeriesCount => _ignoredSeriesKeys.Count;
    public bool HasHiddenSeries => _ignoredSeriesKeys.Count > 0;
    public string HiddenSeriesDisplay => _ignoredSeriesKeys.Count == 1 ? "Restore 1 hidden" : $"Restore {_ignoredSeriesKeys.Count} hidden";

    [ObservableProperty]
    private bool _isStatsLoading;

    [ObservableProperty]
    private bool _isStatsDialogOpen;

    [ObservableProperty]
    private ReadingStatsSummary? _statsSummary;

    [ObservableProperty]
    private ObservableCollection<WatchedFolder> _watchedFolders = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableTags = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableCollections = new();

    [ObservableProperty]
    private ComicEntity? _selectedComic;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    private bool _isGridView = true;

    [ObservableProperty]
    private string _searchText = string.Empty;


    [ObservableProperty]
    private bool _filterFavoritesOnly;

    [ObservableProperty]
    private bool _filterInProgressOnly;

    [ObservableProperty]
    private bool _filterUnreadOnly;

    [ObservableProperty]
    private bool _filterCompletedOnly;

    [ObservableProperty]
    private string? _selectedTag;

    [ObservableProperty]
    private string? _selectedCollection;

    [ObservableProperty]
    private SortOptionItem _selectedSortOption;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _scanStatusMessage = string.Empty;

    [ObservableProperty]
    private int _totalComicCount;

    [ObservableProperty]
    private int _favoritesCount;

    [ObservableProperty]
    private int _inProgressCount;

    [ObservableProperty]
    private int _unreadCount;

    [ObservableProperty]
    private int _completedCount;

    private bool _isAddingFolder;
    private bool _isAddingFile;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isConverting;

    [ObservableProperty]
    private string _conversionStatusMessage = string.Empty;

    [ObservableProperty]
    private double _conversionProgressPercentage;

    [ObservableProperty]
    private bool _isConversionIndeterminate = true;

    [ObservableProperty]
    private bool _isNotificationOpen;

    [ObservableProperty]
    private string _notificationTitle = string.Empty;

    [ObservableProperty]
    private string _notificationMessage = string.Empty;

    [ObservableProperty]
    private InfoBarSeverity _notificationSeverity = InfoBarSeverity.Informational;

    public IReadOnlyList<SortOptionItem> SortOptions { get; }

    public bool IsListView => !IsGridView;
    public bool HasComics => Comics.Count > 0;
    public bool HasNoComics => !HasComics && !IsLoading && !IsScanning && !IsConverting;

    public bool IsFilterOrSearchActive =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        FilterFavoritesOnly ||
        FilterInProgressOnly ||
        FilterUnreadOnly ||
        FilterCompletedOnly ||
        !string.IsNullOrEmpty(SelectedTag) ||
        !string.IsNullOrEmpty(SelectedCollection);

    public bool ShowEmptyLibrary => TotalComicCount == 0 && !IsLoading && !IsScanning && !IsConverting && !IsFilterOrSearchActive;

    public bool ShowEmptyFilter => !IsLoading && !IsScanning && !IsConverting &&
        ((!IsSeriesView && !HasComics && (TotalComicCount > 0 || IsFilterOrSearchActive)) ||
         (IsSeriesView && SeriesGroups.Count == 0 && (TotalComicCount > 0 || !string.IsNullOrWhiteSpace(SeriesSearchText))));

    public bool ShowComicsGrid => !IsSeriesView && IsGridView && HasComics && !ShowEmptyFilter && !ShowEmptyLibrary;
    public bool ShowComicsList => !IsSeriesView && IsListView && HasComics && !ShowEmptyFilter && !ShowEmptyLibrary;
    public bool ShowSeriesGrid => IsSeriesView && SeriesGroups.Count > 0 && !ShowEmptyFilter && !ShowEmptyLibrary;

    public string EmptyFilterTitle
    {
        get
        {
            if (IsSeriesView && SeriesGroups.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(SeriesSearchText))
                    return $"No series match \"{SeriesSearchText}\"";
                return "No Series Found";
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
                return $"No comics match \"{SearchText}\"";
            if (FilterFavoritesOnly)
                return "No Favorite Comics";
            if (FilterInProgressOnly)
                return "No Comics in Progress";
            if (FilterUnreadOnly)
                return "No Unread Comics";
            if (FilterCompletedOnly)
                return "No Completed Comics";
            if (!string.IsNullOrEmpty(SelectedTag))
                return $"No Comics Tagged \"{SelectedTag}\"";
            if (!string.IsNullOrEmpty(SelectedCollection))
                return $"No Comics in \"{SelectedCollection}\"";
            return "No Comics Found";
        }
    }

    public string EmptyFilterSubtitle
    {
        get
        {
            if (IsSeriesView && SeriesGroups.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(SeriesSearchText))
                    return "Try adjusting your search terms or create a new custom series.";
                return "Create a custom manual series using 'New Series' or add comics that share a title.";
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
                return "Try searching with a different title, creator, or keyword.";
            if (FilterFavoritesOnly)
                return "You haven't marked any comics as favorite yet. Click the heart icon on any comic card to add it here.";
            if (FilterInProgressOnly)
                return "You have no comics in progress. Start reading any comic and your reading position will appear here.";
            if (FilterUnreadOnly)
                return "You're all caught up! You have started or completed all the comics in your library.";
            if (FilterCompletedOnly)
                return "No completed comics yet. Finished comics will appear here when you reach the last page or mark them as done.";
            if (!string.IsNullOrEmpty(SelectedTag))
                return $"No comics currently have the \"{SelectedTag}\" tag. Right-click any comic card to manage its tags.";
            if (!string.IsNullOrEmpty(SelectedCollection))
                return "No comics found in this collection.";
            return "No comics match your current filter criteria.";
        }
    }

    public string EmptyFilterGlyph
    {
        get
        {
            if (IsSeriesView && SeriesGroups.Count == 0)
                return "\uE8B9";
            if (!string.IsNullOrWhiteSpace(SearchText))
                return "\uE721"; // Search
            if (FilterFavoritesOnly)
                return "\uEB51"; // Heart / Favorite
            if (FilterInProgressOnly)
                return "\uE736"; // Reading / Open Book
            if (FilterUnreadOnly)
                return "\uE8A5"; // Document / Unread
            if (FilterCompletedOnly)
                return "\uE73E"; // Checkmark / Completed
            if (!string.IsNullOrEmpty(SelectedTag))
                return "\uE8EC"; // Tag
            return "\uE71C"; // Filter
        }
    }

    public LibraryViewModel(
        ILibraryRepository? repository = null,
        ILibraryScannerService? scannerService = null,
        IFilePickerService? pickerService = null,
        IFormatConversionService? conversionService = null,
        IDuplicateDetectionService? duplicateService = null)
    {
        _repository = repository ?? new LibraryRepository();
        _scannerService = scannerService ?? new LibraryScannerService(_repository);
        _pickerService = pickerService ?? new FilePickerService();
        _conversionService = conversionService ?? new FormatConversionService();
        _duplicateService = duplicateService ?? new DuplicateDetectionService();

        SortOptions = new List<SortOptionItem>
        {
            new("Title (A to Z)", LibrarySortOption.TitleAscending),
            new("Title (Z to A)", LibrarySortOption.TitleDescending),
            new("Recently Read", LibrarySortOption.LastReadDescending),
            new("Date Added (Newest)", LibrarySortOption.DateAddedDescending),
            new("Date Added (Oldest)", LibrarySortOption.DateAddedAscending),
            new("Page Count (Most)", LibrarySortOption.PageCountDescending),
            new("File Size (Largest)", LibrarySortOption.FileSizeDescending)
        };

        _selectedSortOption = SortOptions[0];

        _scannerService.ProgressChanged += Scanner_ProgressChanged;
        _conversionService.ProgressChanged += Conversion_ProgressChanged;
        LibraryThumbnailsChanged += OnLibraryThumbnailsChanged;
    }

    public static event Action? LibraryThumbnailsChanged;

    public static void NotifyLibraryThumbnailsChanged()
    {
        LibraryThumbnailsChanged?.Invoke();
    }

    private void OnLibraryThumbnailsChanged()
    {
        var dq = App.DispatcherQueue;
        if (dq != null && !dq.HasThreadAccess)
        {
            dq.TryEnqueue(() => _ = ReloadComicsAsync());
        }
        else
        {
            _ = ReloadComicsAsync();
        }
    }

    private bool _hasInitialized;

    public string NewGroupButtonText => IsCreatorSection ? "New Creator" : "New Series";

    /// <param name="refreshOnly">Returning to the page: reload data but keep the current view, section and open overlays.</param>
    public async Task InitializeAsync(bool refreshOnly = false)
    {
        await _repository.InitializeAsync();
        if (refreshOnly && _hasInitialized)
        {
            await RefreshTagsAndCollectionsAsync();
            await ReloadComicsAsync();
            return;
        }
        _hasInitialized = true;
        try
        {
            var settings = await _repository.GetAppSettingsAsync();
            IsGridView = settings.DefaultViewMode != "List";
            IsSeriesView = settings.IsSeriesViewDefault;
            if (settings.DefaultSortOption >= 0 && settings.DefaultSortOption < SortOptions.Count)
            {
                SelectedSortOption = SortOptions[settings.DefaultSortOption];
            }
        }

        catch (Exception ex)
        {
            Debug.WriteLine($"[LibraryViewModel] Failed to apply settings: {ex.Message}");
        }

        await RefreshTagsAndCollectionsAsync();
        await RefreshWatchedFoldersAsync();
        await ReloadComicsAsync();
    }

    private void Scanner_ProgressChanged(object? sender, ScanProgressEventArgs e)
    {
        // Safe to call from background thread
        Microsoft.UI.Dispatching.DispatcherQueue? dq = App.DispatcherQueue;
        if (dq != null && !dq.HasThreadAccess)
        {
            dq.TryEnqueue(() => HandleScanProgress(e));
        }
        else
        {
            HandleScanProgress(e);
        }
    }

    private void HandleScanProgress(ScanProgressEventArgs e)
    {
        ScanStatusMessage = e.StatusMessage;
        IsScanning = !e.IsCompleted;

        if (e.IsCompleted)
        {
            _ = ReloadComicsAsync();
            _ = RefreshWatchedFoldersAsync();
        }
    }

    private void Conversion_ProgressChanged(object? sender, ConversionProgressEventArgs e)
    {
        Microsoft.UI.Dispatching.DispatcherQueue? dq = App.DispatcherQueue;
        if (dq != null && !dq.HasThreadAccess)
        {
            dq.TryEnqueue(() => HandleConversionProgress(e));
        }
        else
        {
            HandleConversionProgress(e);
        }
    }

    private void HandleConversionProgress(ConversionProgressEventArgs e)
    {
        ConversionStatusMessage = e.StatusMessage;
        ConversionProgressPercentage = e.Percentage;
        IsConversionIndeterminate = e.TotalPages <= 0;
    }

    [RelayCommand]
    public void CancelConversion()
    {
        _conversionCts?.Cancel();
    }

    [RelayCommand]
    public void DismissNotification()
    {
        IsNotificationOpen = false;
    }

    public void ShowNotification(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        Microsoft.UI.Dispatching.DispatcherQueue? dq = App.DispatcherQueue;
        if (dq != null && !dq.HasThreadAccess)
        {
            dq.TryEnqueue(() =>
            {
                NotificationTitle = title;
                NotificationMessage = message;
                NotificationSeverity = severity;
                IsNotificationOpen = true;
            });
        }
        else
        {
            NotificationTitle = title;
            NotificationMessage = message;
            NotificationSeverity = severity;
            IsNotificationOpen = true;
        }
    }

    public async Task<string?> ConvertComicAsync(
        string sourcePath,
        string? destinationPath,
        bool addToLibrary,
        bool removeOldComicFromLibrary = false,
        long oldComicId = 0)
    {
        if (IsConverting)
        {
            ShowNotification("Conversion Busy", "A format conversion is already in progress. Please wait for it to finish.", InfoBarSeverity.Warning);
            return null;
        }

        string sourceDisplayName = string.IsNullOrWhiteSpace(sourcePath) ? "Comic" : Path.GetFileName(sourcePath);
        if (string.IsNullOrWhiteSpace(sourceDisplayName)) sourceDisplayName = sourcePath;

        IsConverting = true;
        IsConversionIndeterminate = true;
        ConversionProgressPercentage = 0;
        ConversionStatusMessage = $"Preparing to convert '{sourceDisplayName}'...";
        _conversionCts = new CancellationTokenSource();

        try
        {
            string dest = string.IsNullOrWhiteSpace(destinationPath)
                ? FormatConversionService.GenerateDefaultDestinationPath(sourcePath)
                : destinationPath;

            string resultPath = await Task.Run(async () =>
            {
                return await _conversionService.ConvertToCbzAsync(sourcePath, dest, _conversionCts.Token);
            }, _conversionCts.Token);

            if (removeOldComicFromLibrary && oldComicId > 0)
            {
                await _repository.RemoveComicAsync(oldComicId);
            }

            if (addToLibrary)
            {
                await _scannerService.IndexSingleComicAsync(resultPath);
            }

            await ReloadComicsAsync();

            ShowNotification(
                "Conversion Complete",
                $"Successfully converted '{sourceDisplayName}' to '{Path.GetFileName(resultPath)}'.",
                InfoBarSeverity.Success);

            return resultPath;
        }
        catch (OperationCanceledException)
        {
            ShowNotification(
                "Conversion Cancelled",
                $"Conversion of '{sourceDisplayName}' was cancelled.",
                InfoBarSeverity.Informational);
            return null;
        }
        catch (Exception ex)
        {
            ShowNotification(
                "Conversion Failed",
                $"Cannot convert '{sourceDisplayName}': {ex.Message}",
                InfoBarSeverity.Error);
            return null;
        }
        finally
        {
            IsConverting = false;
            _conversionCts?.Dispose();
            _conversionCts = null;
            OnPropertyChanged(nameof(HasNoComics));
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, token);
                if (token.IsCancellationRequested) return;

                App.DispatcherQueue?.TryEnqueue(async () =>
                {
                    await ReloadComicsAsync();
                });
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    partial void OnSelectedSortOptionChanged(SortOptionItem value)
    {
        _ = ReloadComicsAsync();
    }

    partial void OnFilterFavoritesOnlyChanged(bool value)
    {
        _ = ReloadComicsAsync();
    }

    partial void OnFilterInProgressOnlyChanged(bool value)
    {
        _ = ReloadComicsAsync();
    }

    partial void OnFilterUnreadOnlyChanged(bool value)
    {
        _ = ReloadComicsAsync();
    }

    partial void OnSelectedTagChanged(string? value)
    {
        _ = ReloadComicsAsync();
    }

    partial void OnSelectedCollectionChanged(string? value)
    {
        _ = ReloadComicsAsync();
    }

    partial void OnFilterCompletedOnlyChanged(bool value)
    {
        _ = ReloadComicsAsync();
    }

    [RelayCommand]
    public async Task AddComicFileAsync()
    {
        if (_isAddingFile) return;
        _isAddingFile = true;
        try
        {
            string? filePath = await _pickerService.PickComicFileAsync();
            if (string.IsNullOrEmpty(filePath)) return;

            IsScanning = true;
            ScanStatusMessage = $"Adding '{Path.GetFileName(filePath)}'...";

            var comic = await Task.Run(async () =>
            {
                return await _scannerService.IndexSingleComicAsync(filePath);
            });

            await ReloadComicsAsync();

            if (comic != null)
            {
                ShowNotification("Comic Added", $"Successfully added '{comic.Title}' to library.", InfoBarSeverity.Success);
            }
            else
            {
                ShowNotification("Comic Added", $"Added '{Path.GetFileName(filePath)}' to library.", InfoBarSeverity.Success);
            }
        }
        catch (Exception ex)
        {
            ShowNotification("Add Comic Failed", $"Failed to add comic: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            IsScanning = false;
            _isAddingFile = false;
        }
    }

    [RelayCommand]
    public void SetViewLayout(bool isGrid)
    {
        IsGridView = isGrid;
        OnPropertyChanged(nameof(IsListView));
        _ = Task.Run(async () =>
        {
            try
            {
                var settings = await _repository.GetAppSettingsAsync();
                settings.DefaultViewMode = isGrid ? "Grid" : "List";
                await _repository.SaveAppSettingsAsync(settings);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LibraryViewModel] Failed to save view mode: {ex.Message}");
            }
        });
    }

    [RelayCommand]
    public async Task AddFolderAsync()
    {
        if (_isAddingFolder) return;
        _isAddingFolder = true;
        try
        {
            string? folder = await _pickerService.PickComicFolderAsync();
            if (string.IsNullOrEmpty(folder)) return;

            IsScanning = true;
            ScanStatusMessage = $"Scanning '{Path.GetFileName(folder)}'...";

            _ = Task.Run(async () =>
            {
                try
                {
                    await _scannerService.ScanFolderAsync(folder);
                }
                catch (Exception ex)
                {
                    App.DispatcherQueue?.TryEnqueue(() =>
                    {
                        IsScanning = false;
                        ScanStatusMessage = $"Scan error: {ex.Message}";
                    });
                }
            });
        }
        finally
        {
            _isAddingFolder = false;
        }
    }

    [RelayCommand]
    public Task RescanLibraryAsync()
    {
        if (IsScanning) return Task.CompletedTask;

        IsScanning = true;
        ScanStatusMessage = "Rescanning library...";

        return Task.Run(async () =>
        {
            try
            {
                await _scannerService.RescanAllAsync();
            }
            catch (Exception ex)
            {
                App.DispatcherQueue?.TryEnqueue(() =>
                {
                    IsScanning = false;
                    ScanStatusMessage = $"Rescan error: {ex.Message}";
                });
            }
        });
    }

    [RelayCommand]
    public void FilterAll()
    {
        SearchText = string.Empty;
        FilterFavoritesOnly = false;
        FilterInProgressOnly = false;
        FilterUnreadOnly = false;
        FilterCompletedOnly = false;
        SelectedTag = null;
        SelectedCollection = null;
    }

    [RelayCommand]
    public void FilterFavorites()
    {
        FilterFavoritesOnly = true;
        FilterInProgressOnly = false;
        FilterUnreadOnly = false;
        FilterCompletedOnly = false;
        SelectedTag = null;
        SelectedCollection = null;
    }

    [RelayCommand]
    public void FilterContinueReading()
    {
        FilterInProgressOnly = true;
        FilterFavoritesOnly = false;
        FilterUnreadOnly = false;
        FilterCompletedOnly = false;
        SelectedTag = null;
        SelectedCollection = null;

        var recent = SortOptions.FirstOrDefault(s => s.Option == LibrarySortOption.LastReadDescending);
        if (recent != null)
        {
            SelectedSortOption = recent;
        }
    }

    [RelayCommand]
    public void FilterUnread()
    {
        FilterUnreadOnly = true;
        FilterFavoritesOnly = false;
        FilterInProgressOnly = false;
        FilterCompletedOnly = false;
        SelectedTag = null;
        SelectedCollection = null;
    }

    [RelayCommand]
    public void FilterCompleted()
    {
        FilterCompletedOnly = true;
        FilterFavoritesOnly = false;
        FilterInProgressOnly = false;
        FilterUnreadOnly = false;
        SelectedTag = null;
        SelectedCollection = null;
    }

    [RelayCommand]
    public void FilterByTag(string? tag)
    {
        SelectedTag = tag;
        FilterFavoritesOnly = false;
        FilterInProgressOnly = false;
        FilterUnreadOnly = false;
        FilterCompletedOnly = false;
    }

    [RelayCommand]
    public void FilterByCollection(string? collection)
    {
        SelectedCollection = collection;
        FilterFavoritesOnly = false;
        FilterInProgressOnly = false;
        FilterUnreadOnly = false;
        FilterCompletedOnly = false;
    }

    [RelayCommand]
    public async Task ToggleCompletedStatusAsync(ComicEntity comic)
    {
        bool newCompleted = !comic.IsCompleted;
        comic.IsCompleted = newCompleted;
        await _repository.SetCompletedStatusAsync(comic.Id, newCompleted);

        InProgressCount = await _repository.GetInProgressCountAsync();
        UnreadCount = await _repository.GetUnreadCountAsync();
        CompletedCount = await _repository.GetCompletedCountAsync();

        if ((FilterInProgressOnly && newCompleted) || (FilterUnreadOnly && newCompleted) || (FilterCompletedOnly && !newCompleted))
        {
            Comics.Remove(comic);
            NotifyComicsChanged();
        }
    }

    [RelayCommand]
    public async Task ToggleFavoriteAsync(ComicEntity comic)
    {
        bool newFav = !comic.IsFavorite;
        comic.IsFavorite = newFav;
        await _repository.SetFavoriteAsync(comic.Id, newFav);

        FavoritesCount = await _repository.GetFavoritesCountAsync();
        OnPropertyChanged(nameof(FavoritesCount));

        if (FilterFavoritesOnly && !newFav)
        {
            Comics.Remove(comic);
            NotifyComicsChanged();
        }
    }

    [RelayCommand]
    public async Task RemoveFromLibraryAsync(ComicEntity comic)
    {
        await _repository.RemoveComicAsync(comic.Id);
        Comics.Remove(comic);
        TotalComicCount = await _repository.GetTotalComicCountAsync();
        FavoritesCount = await _repository.GetFavoritesCountAsync();
        NotifyComicsChanged();
    }

    [RelayCommand]
    public void ShowInExplorer(ComicEntity comic)
    {
        if (string.IsNullOrEmpty(comic.FilePath)) return;

        try
        {
            if (File.Exists(comic.FilePath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{comic.FilePath}\"") { UseShellExecute = true });
            }
            else if (Directory.Exists(comic.FilePath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{comic.FilePath}\"") { UseShellExecute = true });
            }
        }
        catch { }
    }

    [RelayCommand]
    public void OpenComic(ComicEntity comic)
    {
        if (comic == null || string.IsNullOrEmpty(comic.FilePath)) return;
        ComicSelectedForReading?.Invoke(comic.FilePath);
    }

    public async Task AddTagToComicAsync(ComicEntity comic, string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName)) return;
        await _repository.AddTagToComicAsync(comic.Id, tagName);
        if (!comic.Tags.Contains(tagName.Trim()))
        {
            comic.Tags.Add(tagName.Trim());
        }
        await RefreshTagsAndCollectionsAsync();
    }

    public async Task RemoveTagFromComicAsync(ComicEntity comic, string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName)) return;
        await _repository.RemoveTagFromComicAsync(comic.Id, tagName);
        comic.Tags.Remove(tagName.Trim());
        await RefreshTagsAndCollectionsAsync();
    }

    public async Task AddCollectionToComicAsync(ComicEntity comic, string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)) return;
        await _repository.AddToCollectionAsync(comic.Id, collectionName);
        if (!comic.Collections.Contains(collectionName.Trim()))
        {
            comic.Collections.Add(collectionName.Trim());
        }
        await RefreshTagsAndCollectionsAsync();
    }

    public async Task RemoveCollectionFromComicAsync(ComicEntity comic, string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)) return;
        await _repository.RemoveFromCollectionAsync(comic.Id, collectionName);
        comic.Collections.Remove(collectionName.Trim());
        await RefreshTagsAndCollectionsAsync();
    }

    public async Task CreateTagAsync(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName)) return;
        await _repository.CreateTagAsync(tagName.Trim());
        await RefreshTagsAndCollectionsAsync();
    }

    public async Task DeleteTagAsync(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName)) return;
        await _repository.DeleteTagAsync(tagName.Trim());
        if (SelectedTag == tagName.Trim())
        {
            SelectedTag = null;
        }
        await RefreshTagsAndCollectionsAsync();
        await ReloadComicsAsync();
    }

    public async Task CreateCollectionAsync(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)) return;
        await _repository.CreateCollectionAsync(collectionName.Trim());
        await RefreshTagsAndCollectionsAsync();
    }

    public async Task DeleteCollectionAsync(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName)) return;
        await _repository.DeleteCollectionAsync(collectionName.Trim());
        if (SelectedCollection == collectionName.Trim())
        {
            SelectedCollection = null;
        }
        await RefreshTagsAndCollectionsAsync();
        await ReloadComicsAsync();
    }

    public async Task RestartComicAsync(ComicEntity comic)
    {
        comic.LastReadPage = 0;
        comic.IsCompleted = false;
        comic.LastReadAt = DateTime.UtcNow;
        await _repository.UpdateReadingProgressAsync(comic.FilePath, 0, comic.PageCount);
        InProgressCount = await _repository.GetInProgressCountAsync();
        UnreadCount = await _repository.GetUnreadCountAsync();
        NotifyComicsChanged();
    }

    public async Task ReloadComicsAsync()
    {
        IsLoading = true;
        try
        {
            var filter = new LibraryFilter
            {
                SearchQuery = SearchText,
                FavoritesOnly = FilterFavoritesOnly,
                InProgressOnly = FilterInProgressOnly,
                UnreadOnly = FilterUnreadOnly,
                CompletedOnly = FilterCompletedOnly,
                SelectedTag = SelectedTag,
                SelectedCollection = SelectedCollection
            };

            var list = await _repository.GetComicsAsync(filter, SelectedSortOption?.Option ?? LibrarySortOption.TitleAscending);

            Comics.Clear();
            foreach (var item in list)
            {
                Comics.Add(item);
            }

            TotalComicCount = await _repository.GetTotalComicCountAsync();
            FavoritesCount = await _repository.GetFavoritesCountAsync();
            InProgressCount = await _repository.GetInProgressCountAsync();
            UnreadCount = await _repository.GetUnreadCountAsync();
            CompletedCount = await _repository.GetCompletedCountAsync();
            await UpdateSeriesGroupsAsync();
            _ = ScanDuplicatesAsync();
            NotifyComicsChanged();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoComics));
            OnPropertyChanged(nameof(ShowEmptyLibrary));
            OnPropertyChanged(nameof(ShowEmptyFilter));
            OnPropertyChanged(nameof(ShowComicsGrid));
            OnPropertyChanged(nameof(ShowComicsList));
            OnPropertyChanged(nameof(ShowSeriesGrid));
            OnPropertyChanged(nameof(EmptyFilterTitle));
            OnPropertyChanged(nameof(EmptyFilterSubtitle));
            OnPropertyChanged(nameof(EmptyFilterGlyph));
        }
    }

    public void UpdateSeriesGroups()
    {
        _ = UpdateSeriesGroupsAsync();
    }

    public async Task UpdateSeriesGroupsAsync()
    {
        int version = ++_seriesUpdateVersion;

        var manualSeries = await _repository.GetManualSeriesAsync();
        var allComics = await _repository.GetComicsAsync();
        var metadata = await _repository.GetAllComicMetadataAsync();
        var watched = await _repository.GetWatchedFoldersAsync();
        await LoadIgnoredSeriesKeysAsync();

        var options = new SeriesDetectionOptions
        {
            MinIssues = !string.IsNullOrWhiteSpace(SeriesSearchText) ? 1 : 2,
            IgnoredSeriesKeys = new HashSet<string>(_ignoredSeriesKeys, StringComparer.Ordinal),
            RootFolders = watched.Select(w => w.Path).ToList()
        };

        // Auto-updating manual series first pull in newly detected comics of the group they follow.
        if (manualSeries.Any(m => m.IsAutoUpdate))
        {
            var automatic = await Task.Run(() => _seriesService.DetectAll(allComics, metadata, null, new SeriesDetectionOptions { RootFolders = options.RootFolders }));
            bool changed = false;
            foreach (var manual in manualSeries.Where(m => m.IsAutoUpdate))
            {
                var additions = SeriesDetectionService.FindAutoUpdateAdditions(manual, automatic);
                if (additions.Count == 0) continue;
                try
                {
                    await _repository.AddComicsToManualSeriesAsync(manual.ManualSeriesId, additions.Select(a => a.Id));
                    var ordered = SeriesDetectionService.OrderForReading(manual.Issues.Concat(additions), metadata, manual.Section == SeriesSection.Creator);
                    await _repository.SetManualSeriesOrderAsync(manual.ManualSeriesId, ordered.Select(c => c.Id).ToList());
                    changed = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LibraryViewModel] Auto-update of '{manual.SeriesName}' failed: {ex.Message}");
                }
            }
            if (version != _seriesUpdateVersion) return;
            if (changed) manualSeries = await _repository.GetManualSeriesAsync();
        }

        var detection = await Task.Run(() => _seriesService.DetectAll(allComics, metadata, manualSeries, options));
        if (version != _seriesUpdateVersion) return; // a newer refresh started meanwhile

        StorySeriesCount = detection.Series.Count;
        CreatorGroupCount = detection.Creators.Count;
        var allGroups = IsCreatorSection ? detection.Creators : detection.Series;

        if (!string.IsNullOrWhiteSpace(SeriesSearchText))
        {
            string query = SeriesSearchText.Trim();
            allGroups = allGroups.Where(g =>
                g.SeriesName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                g.Issues.Any(i => i.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        allGroups = SelectedSeriesStatusFilter switch
        {
            "Reading" => allGroups.Where(g => g.IsReading).ToList(),
            "Not Started" => allGroups.Where(g => g.IsNotStarted).ToList(),
            "Caught Up" => allGroups.Where(g => g.IsCaughtUp).ToList(),
            "Has Gaps" => allGroups.Where(g => g.HasMissingIssues).ToList(),
            _ => allGroups
        };


        var sortOption = SelectedSortOption?.Option ?? LibrarySortOption.TitleAscending;
        IEnumerable<ComicSeriesGroup> sortedGroups = sortOption switch
        {
            LibrarySortOption.TitleDescending => allGroups.OrderByDescending(g => g.SeriesName, new NaturalSortComparer()),
            LibrarySortOption.LastReadDescending => allGroups.OrderByDescending(g => g.LastReadAt ?? DateTime.MinValue),
            LibrarySortOption.DateAddedDescending => allGroups.OrderByDescending(g => g.Issues.Max(i => i.DateAdded)),
            LibrarySortOption.DateAddedAscending => allGroups.OrderBy(g => g.Issues.Min(i => i.DateAdded)),
            LibrarySortOption.PageCountDescending => allGroups.OrderByDescending(g => g.TotalPages),
            LibrarySortOption.FileSizeDescending => allGroups.OrderByDescending(g => g.Issues.Sum(i => i.FileSize)),
            _ when IsCreatorSection => allGroups.OrderByDescending(g => g.WorkCount).ThenByDescending(g => g.IssueCount).ThenBy(g => g.SeriesName, new NaturalSortComparer()),
            _ => allGroups.OrderBy(g => g.SeriesName, new NaturalSortComparer())
        };

        SeriesGroups.Clear();
        foreach (var group in sortedGroups)
        {
            SeriesGroups.Add(group);
        }

        // Keep an open detail view pointed at the refreshed instance of the same series.
        if (IsSeriesDetailOpen && SelectedSeriesGroup != null)
        {
            var current = SelectedSeriesGroup;
            var refreshed = SeriesGroups.FirstOrDefault(g =>
                (g.IsManual && current.IsManual && g.ManualSeriesId == current.ManualSeriesId) ||
                (!g.IsManual && !current.IsManual && g.SeriesKey == current.SeriesKey));
            if (refreshed != null) SelectedSeriesGroup = refreshed;
        }

        OnPropertyChanged(nameof(SeriesGroupsCountDisplay));
        OnPropertyChanged(nameof(ShowSeriesGrid));
        OnPropertyChanged(nameof(ShowEmptyFilter));
        OnPropertyChanged(nameof(HiddenSeriesCount));
        OnPropertyChanged(nameof(HasHiddenSeries));
        OnPropertyChanged(nameof(HiddenSeriesDisplay));
    }

    private async Task LoadIgnoredSeriesKeysAsync()
    {
        try
        {
            string? json = await _repository.GetSettingAsync(IgnoredSeriesSettingKey);
            var keys = string.IsNullOrWhiteSpace(json)
                ? new List<string>()
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            _ignoredSeriesKeys = new HashSet<string>(keys.Where(k => !string.IsNullOrWhiteSpace(k)), StringComparer.Ordinal);
        }
        catch
        {
            _ignoredSeriesKeys = new HashSet<string>(StringComparer.Ordinal);
        }
    }

    private Task SaveIgnoredSeriesKeysAsync() =>
        _repository.SetSettingAsync(IgnoredSeriesSettingKey, System.Text.Json.JsonSerializer.Serialize(_ignoredSeriesKeys.OrderBy(k => k).ToList()));

    [RelayCommand]
    public async Task HideSeriesAsync(ComicSeriesGroup? group)
    {
        if (group == null || group.IsManual || string.IsNullOrEmpty(group.SeriesKey)) return;
        _ignoredSeriesKeys.Add(group.SeriesKey);
        await SaveIgnoredSeriesKeysAsync();
        if (ReferenceEquals(SelectedSeriesGroup, group)) CloseSeriesDetail();
        await UpdateSeriesGroupsAsync();
        ShowNotification("Series Hidden", $"'{group.SeriesName}' won't be grouped automatically. Restore it from the Series toolbar.", InfoBarSeverity.Informational);
    }

    [RelayCommand]
    public async Task RestoreHiddenSeriesAsync()
    {
        if (_ignoredSeriesKeys.Count == 0) return;
        int count = _ignoredSeriesKeys.Count;
        _ignoredSeriesKeys.Clear();
        await SaveIgnoredSeriesKeysAsync();
        await UpdateSeriesGroupsAsync();
        ShowNotification("Series Restored", count == 1 ? "1 hidden series is back." : $"{count} hidden series are back.", InfoBarSeverity.Success);
    }

    [RelayCommand]
    public async Task SaveSeriesAsManualAsync(ComicSeriesGroup? group)
    {
        if (group == null || group.IsManual || group.Issues.Count == 0) return;
        try
        {
            var ids = group.Issues.Select(i => i.Id).ToHashSet();
            var existing = await _repository.GetManualSeriesAsync();
            var same = existing.FirstOrDefault(m => m.Issues.Count == ids.Count && m.Issues.All(i => ids.Contains(i.Id)));
            if (same != null)
            {
                ShowNotification("Already Saved", $"These comics are already saved as the manual series '{same.SeriesName}'.", InfoBarSeverity.Informational);
                return;
            }

            var section = group.IsCreatorGroup ? SeriesSection.Creator : SeriesSection.Story;
            long id = await _repository.CreateManualSeriesAsync(group.SeriesName, group.Issues.Select(i => i.Id), section, autoUpdate: false, sourceKey: group.SeriesKey);
            await UpdateSeriesGroupsAsync();
            ReopenManualDetail(id);
            ShowNotification(group.IsCreatorGroup ? "Creator Saved" : "Series Saved",
                $"'{group.SeriesName}' is now manual with {group.Issues.Count} comics. Turn on Auto-update to keep adding new matches, or make it automatic again anytime.",
                InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Couldn't Save Series", ex.Message, InfoBarSeverity.Error);
        }
    }

    private void ReopenManualDetail(long manualId)
    {
        var fresh = SeriesGroups.FirstOrDefault(g => g.IsManual && g.ManualSeriesId == manualId);
        if (fresh != null && IsSeriesDetailOpen) SelectedSeriesGroup = fresh;
        else if (IsSeriesDetailOpen) CloseSeriesDetail();
    }

    /// <summary>Turns a manual series back into automatic grouping (comics stay in the library).</summary>
    [RelayCommand]
    public async Task MakeSeriesAutomaticAsync(ComicSeriesGroup? group)
    {
        var target = group ?? SelectedSeriesGroup;
        if (target == null || !target.IsManual) return;
        try
        {
            await _repository.DeleteManualSeriesAsync(target.ManualSeriesId);
            if (!string.IsNullOrEmpty(target.SourceKey) && _ignoredSeriesKeys.Remove(target.SourceKey))
            {
                await SaveIgnoredSeriesKeysAsync();
            }
            if (IsSeriesDetailOpen) CloseSeriesDetail();
            await UpdateSeriesGroupsAsync();
            ShowNotification("Back to Automatic", $"'{target.SeriesName}' is grouped automatically again and follows your library.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Couldn't Switch to Automatic", ex.Message, InfoBarSeverity.Error);
        }
    }

    /// <summary>Manual series: switch auto-update (adding newly detected matching comics) on or off.</summary>
    [RelayCommand]
    public async Task ToggleSeriesAutoUpdateAsync(ComicSeriesGroup? group)
    {
        var target = group ?? SelectedSeriesGroup;
        if (target == null || !target.IsManual) return;
        bool enable = !target.IsAutoUpdate;
        try
        {
            await _repository.UpdateManualSeriesOptionsAsync(target.ManualSeriesId, enable, target.Section);
            target.IsAutoUpdate = enable;
            int before = target.Issues.Count;
            await UpdateSeriesGroupsAsync();
            ReopenManualDetail(target.ManualSeriesId);
            int after = SeriesGroups.FirstOrDefault(g => g.IsManual && g.ManualSeriesId == target.ManualSeriesId)?.Issues.Count ?? before;
            string added = enable && after > before ? $" {after - before} new comic{(after - before == 1 ? " was" : "s were")} added." : string.Empty;
            ShowNotification(enable ? "Auto-update On" : "Auto-update Off",
                enable ? $"'{target.SeriesName}' will pick up new matching comics automatically.{added}" : $"'{target.SeriesName}' now only changes when you edit it.",
                InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Couldn't Change Auto-update", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    public void OpenSeriesView()
    {
        IsSeriesView = true;
        _ = UpdateSeriesGroupsAsync();
    }

    [RelayCommand]
    public void CloseSeriesView()
    {
        IsSeriesView = false;
        SeriesSearchText = string.Empty;
    }

    [RelayCommand]
    public void ToggleSeriesView()
    {
        IsSeriesView = !IsSeriesView;
        if (IsSeriesView)
        {
            _ = UpdateSeriesGroupsAsync();
        }
        _ = Task.Run(async () =>
        {
            try
            {
                var settings = await _repository.GetAppSettingsAsync();
                settings.IsSeriesViewDefault = IsSeriesView;
                await _repository.SaveAppSettingsAsync(settings);
            }
            catch { }
        });
    }

    [RelayCommand]
    public void OpenSeriesDetail(ComicSeriesGroup? group)
    {
        if (group == null) return;
        SelectedSeriesGroup = group;
        IsSeriesDetailOpen = true;
    }

    [RelayCommand]
    public void CloseSeriesDetail()
    {
        IsSeriesDetailOpen = false;
        SelectedSeriesGroup = null;
    }

    [RelayCommand]
    public void PlaySeriesNext(ComicSeriesGroup? group)
    {
        if (group == null) return;
        var next = group.NextIssueToRead;
        if (next != null)
        {
            OpenComic(next);
        }
    }

    /// <summary>Create dialog: true builds a creator collection, false a continuation series.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewSeriesIsStory))]
    [NotifyPropertyChangedFor(nameof(NewSeriesNamePlaceholder))]
    [NotifyPropertyChangedFor(nameof(NewSeriesDialogTitle))]
    [NotifyPropertyChangedFor(nameof(NewSeriesSmartPickText))]
    private bool _newSeriesIsCreator;

    public bool NewSeriesIsStory => !NewSeriesIsCreator;
    public string NewSeriesNamePlaceholder => NewSeriesIsCreator ? "Creator name, e.g. an author, artist or circle" : "Series name, e.g. Saga or Berserk";
    public string NewSeriesDialogTitle => NewSeriesIsCreator ? "NEW CREATOR COLLECTION" : "NEW CONTINUATION SERIES";
    public string NewSeriesSmartPickText => NewSeriesIsCreator ? "Pick everything by this creator" : "Pick every book of this series";

    [ObservableProperty]
    private bool _newSeriesAutoUpdate = true;

    public string SelectedCandidateCountDisplay => SelectedCandidateCount == 1 ? "1 comic selected" : $"{SelectedCandidateCount} comics selected";

    partial void OnSelectedCandidateCountChanged(int value) => OnPropertyChanged(nameof(SelectedCandidateCountDisplay));

    [RelayCommand]
    public async Task OpenCreateSeriesDialogAsync()
    {
        NewSeriesName = string.Empty;
        ManualSeriesSearchQuery = string.Empty;
        ManualSeriesCandidates.Clear();
        NewSeriesIsCreator = IsCreatorSection;
        NewSeriesAutoUpdate = true;

        var allComics = await _repository.GetComicsAsync();
        var metadata = await _repository.GetAllComicMetadataAsync();
        foreach (var comic in allComics.Where(c => !c.IsMissing).OrderBy(c => c.Title, new NaturalSortComparer()))
        {
            metadata.TryGetValue(comic.Id, out var meta);
            ManualSeriesCandidates.Add(new ManualSeriesComicItem(comic, ComicIdentityParser.Parse(comic, meta), isSelected: false));
        }
        SelectedCandidateCount = 0;
        IsCreateSeriesDialogOpen = true;
    }

    [RelayCommand]
    public void SetNewSeriesKind(string? kind) => NewSeriesIsCreator = string.Equals(kind, "creator", StringComparison.OrdinalIgnoreCase);

    /// <summary>Selects every comic whose parsed series (or credited creator) matches the typed name.</summary>
    [RelayCommand]
    public void SmartSelectCandidates()
    {
        string name = (string.IsNullOrWhiteSpace(NewSeriesName) ? ManualSeriesSearchQuery : NewSeriesName)?.Trim() ?? string.Empty;
        string key = ComicIdentityParser.MakeKey(name);
        if (key.Length < 2)
        {
            ShowNotification("Type a Name First", NewSeriesIsCreator ? "Enter the creator's name, then pick their comics." : "Enter the series name, then pick its books.", InfoBarSeverity.Warning);
            return;
        }

        int picked = 0;
        foreach (var item in ManualSeriesCandidates)
        {
            bool match = NewSeriesIsCreator
                ? item.Identity.CreatorKeys.Any(c => c == key || (key.Length >= 4 && (c.Contains(key) || key.Contains(c) && c.Length >= 4)))
                : item.Identity.SeriesKey == key
                  || (key.Length >= 5 && item.Identity.SeriesKey.StartsWith(key, StringComparison.Ordinal))
                  || (key.Length >= 7 && SeriesParserHelper.CalculateTypoSimilarity(item.Identity.SeriesKey, key) >= 0.9);
            if (match && !item.IsSelected)
            {
                item.IsSelected = true;
                picked++;
            }
        }
        SelectedCandidateCount = ManualSeriesCandidates.Count(c => c.IsSelected);
        ShowNotification(picked == 0 ? "No Matches" : "Comics Picked",
            picked == 0 ? $"Nothing in your library matches '{name}'. Pick comics by hand instead." : $"Selected {picked} comic{(picked == 1 ? "" : "s")} matching '{name}'.",
            picked == 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
    }

    [RelayCommand]
    public void CloseCreateSeriesDialog()
    {
        IsCreateSeriesDialogOpen = false;
        ManualSeriesCandidates.Clear();
        NewSeriesName = string.Empty;
        ManualSeriesSearchQuery = string.Empty;
        SelectedCandidateCount = 0;
    }

    [RelayCommand]
    public void ToggleCandidateSelection(ManualSeriesComicItem? item)
    {
        if (item == null) return;
        item.IsSelected = !item.IsSelected;
        SelectedCandidateCount = ManualSeriesCandidates.Count(c => c.IsSelected);
    }

    [RelayCommand]
    public void SelectAllCandidates()
    {
        foreach (var item in ManualSeriesCandidates)
        {
            if (item.IsVisible) item.IsSelected = true;
        }
        SelectedCandidateCount = ManualSeriesCandidates.Count(c => c.IsSelected);
    }

    [RelayCommand]
    public void ClearCandidateSelection()
    {
        foreach (var item in ManualSeriesCandidates)
        {
            item.IsSelected = false;
        }
        SelectedCandidateCount = 0;
    }

    [RelayCommand]
    public async Task SaveManualSeriesAsync()
    {
        string name = NewSeriesName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowNotification("Series Name Required", "Please enter a name for the new series.", InfoBarSeverity.Warning);
            return;
        }

        var selectedIds = ManualSeriesCandidates.Where(c => c.IsSelected).Select(c => c.Comic.Id).ToList();
        if (selectedIds.Count == 0)
        {
            ShowNotification("No Comics Selected", "Please select at least one comic to add to this series.", InfoBarSeverity.Warning);
            return;
        }

        try
        {
            var selectedComics = ManualSeriesCandidates.Where(c => c.IsSelected).Select(c => c.Comic).ToList();
            var metadata = await _repository.GetAllComicMetadataAsync();
            var ordered = SeriesDetectionService.OrderForReading(selectedComics, metadata, NewSeriesIsCreator);
            var section = NewSeriesIsCreator ? SeriesSection.Creator : SeriesSection.Story;
            await _repository.CreateManualSeriesAsync(name, ordered.Select(c => c.Id), section, NewSeriesAutoUpdate, sourceKey: null);
            IsCreateSeriesDialogOpen = false;
            IsCreatorSection = NewSeriesIsCreator;
            if (!IsSeriesView) IsSeriesView = true;
            await ReloadComicsAsync();
            ShowNotification(NewSeriesIsCreator ? "Creator Collection Created" : "Series Created",
                $"'{name}' has {selectedIds.Count} comic{(selectedIds.Count == 1 ? "" : "s")}{(NewSeriesAutoUpdate ? " and will auto-update with new matches" : "")}.",
                InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Create Series Failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    public async Task DeleteManualSeriesAsync(ComicSeriesGroup? group)
    {
        var target = group ?? SelectedSeriesGroup;
        if (target == null || !target.IsManual) return;

        try
        {
            await _repository.DeleteManualSeriesAsync(target.ManualSeriesId);
            if (IsSeriesDetailOpen && SelectedSeriesGroup == target)
            {
                IsSeriesDetailOpen = false;
                SelectedSeriesGroup = null;
            }
            await ReloadComicsAsync();
            ShowNotification("Series Deleted", $"Successfully deleted series '{target.SeriesName}'.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Delete Series Failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    public async Task RemoveComicFromManualSeriesAsync(ComicEntity? comic)
    {
        if (comic == null || SelectedSeriesGroup == null || !SelectedSeriesGroup.IsManual) return;

        try
        {
            await _repository.RemoveComicFromManualSeriesAsync(SelectedSeriesGroup.ManualSeriesId, comic.Id);
            SelectedSeriesGroup.Issues.Remove(comic);
            SelectedSeriesGroup.RefreshProperties();
            if (SelectedSeriesGroup.Issues.Count == 0)
            {
                IsSeriesDetailOpen = false;
                SelectedSeriesGroup = null;
            }
            await ReloadComicsAsync();
            ShowNotification("Comic Removed", $"Removed '{comic.Title}' from series.", InfoBarSeverity.Informational);
        }
        catch (Exception ex)
        {
            ShowNotification("Remove Failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    public async Task OpenAddComicsToExistingSeriesAsync()
    {
        if (SelectedSeriesGroup == null) return;

        ManualSeriesSearchQuery = string.Empty;
        ManualSeriesCandidates.Clear();

        var existingIds = new HashSet<long>(SelectedSeriesGroup.Issues.Select(i => i.Id));
        var allComics = await _repository.GetComicsAsync();
        foreach (var comic in allComics)
        {
            if (!existingIds.Contains(comic.Id) && !comic.IsMissing)
            {
                var candidate = new ManualSeriesComicItem(comic, ComicIdentityParser.Parse(comic), isSelected: false);
                ManualSeriesCandidates.Add(candidate);
            }
        }
        SelectedCandidateCount = 0;
        IsAddComicsToSeriesDialogOpen = true;
    }

    [RelayCommand]
    public void CloseAddComicsToSeriesDialog()
    {
        IsAddComicsToSeriesDialogOpen = false;
        ManualSeriesCandidates.Clear();
        ManualSeriesSearchQuery = string.Empty;
        SelectedCandidateCount = 0;
    }

    [RelayCommand]
    public async Task SaveComicsToExistingSeriesAsync()
    {
        if (SelectedSeriesGroup == null) return;

        var selectedComics = ManualSeriesCandidates.Where(c => c.IsSelected).Select(c => c.Comic).ToList();
        if (selectedComics.Count == 0)
        {
            ShowNotification("No Comics Selected", "Please select at least one comic to add to this series.", InfoBarSeverity.Warning);
            return;
        }

        try
        {
            long seriesId;
            if (SelectedSeriesGroup.IsManual && SelectedSeriesGroup.ManualSeriesId > 0)
            {
                seriesId = SelectedSeriesGroup.ManualSeriesId;
                await _repository.AddComicsToManualSeriesAsync(seriesId, selectedComics.Select(c => c.Id));
            }
            else
            {
                // Upgrade auto-detected series into a persistent manual series
                var allIds = SelectedSeriesGroup.Issues.Select(i => i.Id).Concat(selectedComics.Select(c => c.Id)).Distinct();
                seriesId = await _repository.CreateManualSeriesAsync(SelectedSeriesGroup.SeriesName, allIds,
                    SelectedSeriesGroup.IsCreatorGroup ? SeriesSection.Creator : SeriesSection.Story, autoUpdate: false, sourceKey: SelectedSeriesGroup.SeriesKey);
                SelectedSeriesGroup.ManualSeriesId = seriesId;
                SelectedSeriesGroup.IsManual = true;
            }

            foreach (var comic in selectedComics)
            {
                if (!SelectedSeriesGroup.Issues.Any(i => i.Id == comic.Id))
                {
                    SelectedSeriesGroup.Issues.Add(comic);
                }
            }

            var sortedIssues = SelectedSeriesGroup.Issues.OrderBy(i => i.Title, new NaturalSortComparer()).ToList();
            SelectedSeriesGroup.Issues.Clear();
            foreach (var issue in sortedIssues)
            {
                SelectedSeriesGroup.Issues.Add(issue);
            }
            SelectedSeriesGroup.RefreshProperties();

            IsAddComicsToSeriesDialogOpen = false;
            await UpdateSeriesGroupsAsync();
            ShowNotification("Comics Added", $"Successfully added {selectedComics.Count} comic(s) to '{SelectedSeriesGroup.SeriesName}'.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Add Comics Failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    public void OpenDuplicateManager()
    {
        IsDuplicateManagerOpen = true;
        _ = ScanDuplicatesAsync();
    }

    [RelayCommand]
    public void CloseDuplicateManager()
    {
        IsDuplicateManagerOpen = false;
    }

    [RelayCommand]
    public async Task ScanDuplicatesAsync()
    {
        int version = ++_duplicateScanVersion;
        IsScanningDuplicates = true;
        try
        {
            var allComics = await _repository.GetComicsAsync();
            var ignored = await _repository.GetIgnoredDuplicatePairsAsync();
            var metadata = await _repository.GetAllComicMetadataAsync();
            var dupes = await Task.Run(() => _duplicateService.FindDuplicates(allComics, ignored, metadata, compareFileContents: true));
            if (version != _duplicateScanVersion) return;

            DuplicateGroups.Clear();
            foreach (var d in dupes)
            {
                DuplicateGroups.Add(d);
            }
            NotifyDuplicatesChanged();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LibraryViewModel] Duplicate scan failed: {ex.Message}");
        }
        finally
        {
            if (version == _duplicateScanVersion) IsScanningDuplicates = false;
        }
    }

    private void NotifyDuplicatesChanged()
    {
        OnPropertyChanged(nameof(DuplicateCount));
        OnPropertyChanged(nameof(HasDuplicates));
        OnPropertyChanged(nameof(DuplicateExtraCopies));
        OnPropertyChanged(nameof(DuplicateSummaryDisplay));
    }

    /// <summary>Keeps the recommended copy, carries reading progress and favorite over, and removes the others.</summary>
    public async Task<int> KeepBestCopyAsync(DuplicateComicGroup? group, bool deleteFiles)
    {
        if (group?.RecommendedKeep is not { } keep) return 0;
        var extras = group.Copies.Where(c => !ReferenceEquals(c, keep)).ToList();
        if (extras.Count == 0) return 0;

        await MergeReadingStateAsync(keep, extras);

        int removed = 0;
        foreach (var extra in extras)
        {
            try
            {
                await _repository.DeleteComicAsync(extra.Id, deleteFiles);
                Comics.Remove(extra);
                removed++;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LibraryViewModel] Remove duplicate failed: {ex.Message}");
            }
        }

        DuplicateGroups.Remove(group);
        NotifyDuplicatesChanged();
        return removed;
    }

    public async Task ResolveDuplicateGroupAsync(DuplicateComicGroup? group, bool deleteFiles)
    {
        if (group == null) return;
        int removed = await KeepBestCopyAsync(group, deleteFiles);
        await AfterDuplicateCleanupAsync();
        string where = deleteFiles ? "moved to the Recycle Bin" : "removed from the library";
        ShowNotification("Duplicates Resolved",
            $"Kept the best copy of '{group.GroupTitle}'. {removed} extra cop{(removed == 1 ? "y" : "ies")} {where}.",
            InfoBarSeverity.Success);
    }

    public async Task ResolveAllDuplicatesAsync(bool deleteFiles, bool highConfidenceOnly)
    {
        var targets = DuplicateGroups.Where(g => !highConfidenceOnly || g.IsHighConfidence).ToList();
        int removed = 0;
        foreach (var group in targets)
        {
            removed += await KeepBestCopyAsync(group, deleteFiles);
        }

        await AfterDuplicateCleanupAsync();
        string where = deleteFiles ? "moved to the Recycle Bin" : "removed from the library";
        ShowNotification("Library Cleaned Up",
            $"Resolved {targets.Count} duplicate group{(targets.Count == 1 ? "" : "s")}. {removed} extra cop{(removed == 1 ? "y" : "ies")} {where}.",
            InfoBarSeverity.Success);
    }

    public async Task RemoveDuplicateCopyAsync(ComicEntity? comic, bool deleteFile)
    {
        if (comic == null) return;
        try
        {
            var group = DuplicateGroups.FirstOrDefault(g => g.Copies.Contains(comic));
            var keep = group?.Copies.Where(c => !ReferenceEquals(c, comic)).OrderByDescending(DuplicateDetectionService.KeepScore).FirstOrDefault();
            if (keep != null) await MergeReadingStateAsync(keep, new[] { comic });

            await _repository.DeleteComicAsync(comic.Id, deleteFile);
            Comics.Remove(comic);

            if (group != null)
            {
                group.Copies.Remove(comic);
                var item = group.CopyItems.FirstOrDefault(c => ReferenceEquals(c.Comic, comic));
                if (item != null) group.CopyItems.Remove(item);
                if (group.Copies.Count < 2)
                {
                    DuplicateGroups.Remove(group);
                }
                else if (!group.CopyItems.Any(c => c.IsRecommended))
                {
                    var best = group.CopyItems.OrderByDescending(c => DuplicateDetectionService.KeepScore(c.Comic)).First();
                    best.IsRecommended = true;
                }
                group.Refresh();
            }

            NotifyDuplicatesChanged();
            await AfterDuplicateCleanupAsync();
            ShowNotification(deleteFile ? "Copy Recycled" : "Copy Removed",
                deleteFile ? $"'{comic.Title}' was moved to the Recycle Bin." : $"'{comic.Title}' was removed from the library (the file stays on disk).",
                InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Removal Failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    /// <summary>The kept copy inherits the furthest reading progress and any favorite flag from the removed copies.</summary>
    private async Task MergeReadingStateAsync(ComicEntity keep, IEnumerable<ComicEntity> removed)
    {
        try
        {
            var others = removed.ToList();
            if (others.Any(o => o.IsFavorite) && !keep.IsFavorite)
            {
                await _repository.SetFavoriteAsync(keep.Id, true);
                keep.IsFavorite = true;
            }

            static double ProgressOf(ComicEntity c) =>
                c.IsCompleted ? 1.0 : c.PageCount > 0 && c.LastReadPage > 0 ? (c.LastReadPage + 1) / (double)c.PageCount : 0;

            var furthest = others.OrderByDescending(ProgressOf).FirstOrDefault();
            if (furthest == null || ProgressOf(furthest) <= ProgressOf(keep)) return;

            if (furthest.IsCompleted)
            {
                await _repository.SetCompletedStatusAsync(keep.Id, true);
                keep.IsCompleted = true;
            }
            else if (keep.PageCount > 0)
            {
                int page = Math.Clamp((int)Math.Round(ProgressOf(furthest) * keep.PageCount) - 1, 0, keep.PageCount - 1);
                await _repository.UpdateReadingProgressAsync(keep.FilePath, page, keep.PageCount);
                keep.LastReadPage = page;
                keep.LastReadAt = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LibraryViewModel] Merge reading state failed: {ex.Message}");
        }
    }

    /// <summary>Reloads the grid, counts, series and duplicate groups so removed copies vanish instantly everywhere.</summary>
    private async Task AfterDuplicateCleanupAsync()
    {
        await ReloadComicsAsync();
        if (IsSeriesDetailOpen && SelectedSeriesGroup != null && SelectedSeriesGroup.Issues.Count == 0)
        {
            CloseSeriesDetail();
        }
    }

    [RelayCommand]
    public async Task DismissDuplicateGroupAsync(DuplicateComicGroup? group)
    {
        if (group == null) return;

        try
        {
            for (int i = 0; i < group.Copies.Count; i++)
            {
                for (int j = i + 1; j < group.Copies.Count; j++)
                {
                    await _repository.IgnoreDuplicatePairAsync(group.Copies[i].Id, group.Copies[j].Id);
                }
            }

            DuplicateGroups.Remove(group);
            NotifyDuplicatesChanged();
            ShowNotification("Kept All Copies", $"'{group.GroupTitle}' won't be flagged again.", InfoBarSeverity.Informational);
        }
        catch (Exception ex)
        {
            ShowNotification("Error", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    public Task DeleteDuplicateCopyAsync(ComicEntity? comic) => RemoveDuplicateCopyAsync(comic, deleteFile: true);

    [RelayCommand]
    public async Task OpenReadingStatsAsync()
    {
        IsStatsDialogOpen = true;
        ReadingStatsNotifier.StatsChanged -= OnReadingStatsChanged;
        ReadingStatsNotifier.StatsChanged += OnReadingStatsChanged;
        StartStatsRefreshTimer();
        await RefreshReadingStatsAsync();
    }

    [RelayCommand]
    public async Task RefreshReadingStatsAsync()
    {
        IsStatsLoading = true;
        try
        {
            StatsSummary = await _repository.GetReadingStatsSummaryAsync();
        }
        catch (Exception ex)
        {
            ShowNotification("Stats Error", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsStatsLoading = false;
        }
    }

    [RelayCommand]
    public void CloseReadingStats()
    {
        IsStatsDialogOpen = false;
        ReadingStatsNotifier.StatsChanged -= OnReadingStatsChanged;
        _statsRefreshTimer?.Stop();
    }

    private void OnReadingStatsChanged()
    {
        var dq = App.DispatcherQueue;
        if (dq == null || _statsRefreshQueued) return;
        _statsRefreshQueued = true;
        dq.TryEnqueue(async () =>
        {
            _statsRefreshQueued = false;
            if (IsStatsDialogOpen) await RefreshReadingStatsAsync();
        });
    }

    private void StartStatsRefreshTimer()
    {
        var dq = App.DispatcherQueue;
        if (dq == null) return;
        if (_statsRefreshTimer == null)
        {
            _statsRefreshTimer = dq.CreateTimer();
            // Keeps "today", streaks and "x minutes ago" correct while the dashboard stays open.
            _statsRefreshTimer.Interval = TimeSpan.FromSeconds(30);
            _statsRefreshTimer.IsRepeating = true;
            _statsRefreshTimer.Tick += async (_, _) =>
            {
                if (IsStatsDialogOpen) await RefreshReadingStatsAsync();
            };
        }
        _statsRefreshTimer.Start();
    }

    [RelayCommand]
    public async Task ExportLibraryDataAsync(string destinationFilePath)
    {
        if (string.IsNullOrWhiteSpace(destinationFilePath)) return;
        try
        {
            string json = await _repository.ExportLibraryBackupJsonAsync();
            await File.WriteAllTextAsync(destinationFilePath, json);
            ShowNotification("Backup Exported", $"Library backup successfully saved to {Path.GetFileName(destinationFilePath)}.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Export Failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    public async Task ImportLibraryDataAsync(string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath)) return;
        try
        {
            string json = await File.ReadAllTextAsync(sourceFilePath);
            var result = await _repository.ImportLibraryBackupJsonAsync(json, overwriteExisting: true);
            await ReloadComicsAsync();
            ShowNotification("Restore Complete", $"Restored progress for {result.comicsRestored} comic(s), {result.bookmarksRestored} bookmark(s), and {result.tagsRestored} tag(s).", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Import Failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    public async Task RefreshWatchedFoldersAsync()
    {
        var folders = await _repository.GetWatchedFoldersAsync();
        WatchedFolders.Clear();
        foreach (var f in folders)
        {
            WatchedFolders.Add(f);
        }
    }

    public async Task RefreshTagsAndCollectionsAsync()
    {
        var tags = await _repository.GetAllTagsAsync();
        AvailableTags.Clear();
        foreach (var t in tags)
        {
            AvailableTags.Add(t);
        }

        var collections = await _repository.GetCollectionFoldersAsync();
        AvailableCollections.Clear();
        foreach (var c in collections)
        {
            AvailableCollections.Add(c);
        }
    }

    public ILibraryRepository Repository => _repository;

    public async Task<ComicMetadataEntity?> GetComicMetadataAsync(long comicId)
    {
        return await _repository.GetMetadataForComicAsync(comicId);
    }

    public async Task SaveComicMetadataAsync(ComicMetadataEntity metadata, ComicEntity? comic = null)
    {
        await _repository.SaveComicMetadataAsync(metadata);
        if (comic != null && !string.IsNullOrWhiteSpace(metadata.Title) && metadata.Title != comic.Title)
        {
            comic.Title = metadata.Title;
            await _repository.UpdateComicTitleAsync(comic.Id, metadata.Title);
        }
    }

    private void NotifyComicsChanged()
    {
        OnPropertyChanged(nameof(HasComics));
        OnPropertyChanged(nameof(HasNoComics));
        OnPropertyChanged(nameof(IsFilterOrSearchActive));
        OnPropertyChanged(nameof(ShowEmptyLibrary));
        OnPropertyChanged(nameof(ShowEmptyFilter));
        OnPropertyChanged(nameof(EmptyFilterTitle));
        OnPropertyChanged(nameof(EmptyFilterSubtitle));
        OnPropertyChanged(nameof(EmptyFilterGlyph));
    }
}

