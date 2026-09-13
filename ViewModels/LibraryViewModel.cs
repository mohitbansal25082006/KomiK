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

    private CancellationTokenSource? _searchDebounceCts;
    private CancellationTokenSource? _conversionCts;

    public event Action<string>? ComicSelectedForReading;

    [ObservableProperty]
    private ObservableCollection<ComicEntity> _comics = new();

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

    public bool ShowEmptyLibrary => !HasComics && !IsLoading && !IsScanning && !IsConverting && TotalComicCount == 0 && !IsFilterOrSearchActive;

    public bool ShowEmptyFilter => !HasComics && !IsLoading && !IsScanning && !IsConverting && (TotalComicCount > 0 || IsFilterOrSearchActive);

    public string EmptyFilterTitle
    {
        get
        {
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
        IFormatConversionService? conversionService = null)
    {
        _repository = repository ?? new LibraryRepository();
        _scannerService = scannerService ?? new LibraryScannerService(_repository);
        _pickerService = pickerService ?? new FilePickerService();
        _conversionService = conversionService ?? new FormatConversionService();

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

    public async Task InitializeAsync()
    {
        await _repository.InitializeAsync();
        try
        {
            var settings = await _repository.GetAppSettingsAsync();
            IsGridView = settings.DefaultViewMode != "List";
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
            NotifyComicsChanged();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoComics));
            OnPropertyChanged(nameof(ShowEmptyLibrary));
            OnPropertyChanged(nameof(ShowEmptyFilter));
            OnPropertyChanged(nameof(EmptyFilterTitle));
            OnPropertyChanged(nameof(EmptyFilterSubtitle));
            OnPropertyChanged(nameof(EmptyFilterGlyph));
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
