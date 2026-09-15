using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Komik.Helpers;
using Komik.Models;
using Komik.Services;

namespace Komik.ViewModels;

/// <summary>Series covers, the Edit Series screen and the comic picker shared by Create and Add Comics.</summary>
public partial class LibraryViewModel
{
    // ───────────────────────── Covers ─────────────────────────

    private const string CoverOverridesSettingKey = "series.cover_overrides";
    private Dictionary<string, long> _coverOverrides = new(StringComparer.Ordinal);

    private static string CoverKeyOf(ComicSeriesGroup group) =>
        group.IsManual ? "manual:" + group.ManualSeriesId.ToString(System.Globalization.CultureInfo.InvariantCulture) : group.SeriesKey;

    private async Task LoadCoverOverridesAsync()
    {
        try
        {
            string? json = await _repository.GetSettingAsync(CoverOverridesSettingKey);
            _coverOverrides = string.IsNullOrWhiteSpace(json)
                ? new Dictionary<string, long>(StringComparer.Ordinal)
                : new Dictionary<string, long>(System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, long>>(json) ?? new(), StringComparer.Ordinal);
        }
        catch
        {
            _coverOverrides = new Dictionary<string, long>(StringComparer.Ordinal);
        }
    }

    private Task SaveCoverOverridesAsync() =>
        _repository.SetSettingAsync(CoverOverridesSettingKey, System.Text.Json.JsonSerializer.Serialize(_coverOverrides));

    private void ApplyCoverOverrides(IEnumerable<ComicSeriesGroup> groups)
    {
        foreach (var group in groups)
        {
            if (!_coverOverrides.TryGetValue(CoverKeyOf(group), out long comicId)) continue;
            var comic = group.Issues.FirstOrDefault(i => i.Id == comicId);
            if (comic != null && !string.IsNullOrEmpty(comic.ThumbnailPath)) group.CoverThumbnailPath = comic.ThumbnailPath;
        }
    }

    /// <summary>The comic whose cover a series shows (a chosen one, otherwise the automatic pick).</summary>
    public long? GetCoverComicId(ComicSeriesGroup group)
    {
        if (_coverOverrides.TryGetValue(CoverKeyOf(group), out long id) && group.Issues.Any(i => i.Id == id)) return id;
        return group.Issues.FirstOrDefault(i => !string.IsNullOrEmpty(i.ThumbnailPath) && i.ThumbnailPath == group.CoverThumbnailPath)?.Id;
    }

    public async Task SetSeriesCoverAsync(ComicSeriesGroup? group, ComicEntity? comic)
    {
        if (group == null || comic == null) return;
        if (string.IsNullOrEmpty(comic.ThumbnailPath))
        {
            ShowNotification("No Cover Yet", $"'{comic.Title}' has no cover image cached yet. Cache covers in Settings first.", InfoBarSeverity.Warning);
            return;
        }

        await LoadCoverOverridesAsync();
        _coverOverrides[CoverKeyOf(group)] = comic.Id;
        await SaveCoverOverridesAsync();
        group.CoverThumbnailPath = comic.ThumbnailPath;
        await UpdateSeriesGroupsAsync();
        ShowNotification("Cover Changed", $"'{group.SeriesName}' now uses the cover of '{comic.Title}'.", InfoBarSeverity.Success);
    }

    [RelayCommand]
    public async Task ResetSeriesCoverAsync(ComicSeriesGroup? group)
    {
        var target = group ?? SelectedSeriesGroup;
        if (target == null) return;
        await LoadCoverOverridesAsync();
        if (_coverOverrides.Remove(CoverKeyOf(target)))
        {
            await SaveCoverOverridesAsync();
            await UpdateSeriesGroupsAsync();
            ShowNotification("Cover Reset", $"'{target.SeriesName}' uses its first comic's cover again.", InfoBarSeverity.Informational);
        }
    }

    private async Task CarryCoverToManualAsync(ComicSeriesGroup from, long manualId)
    {
        await LoadCoverOverridesAsync();
        if (_coverOverrides.TryGetValue(CoverKeyOf(from), out long comicId))
        {
            _coverOverrides["manual:" + manualId.ToString(System.Globalization.CultureInfo.InvariantCulture)] = comicId;
            await SaveCoverOverridesAsync();
        }
    }

    // ───────────────────────── Comic picker (create / add) ─────────────────────────

    public ObservableCollection<ManualSeriesComicItem> FilteredCandidates { get; } = new();

    public List<string> CandidateStatusFilters { get; } = new() { "All", "Unread", "Reading", "Finished", "Favorites", "Picked" };
    public List<string> CandidateSortOptions { get; } = new() { "Title (A to Z)", "Newest added", "Recently read", "Most pages" };

    [ObservableProperty]
    private string _candidateStatusFilter = "All";

    [ObservableProperty]
    private int _candidateSortIndex;

    [ObservableProperty]
    private string _candidateScopeNote = string.Empty;

    partial void OnCandidateStatusFilterChanged(string value) => ApplyCandidateFilter();
    partial void OnCandidateSortIndexChanged(int value) => ApplyCandidateFilter();

    public string CandidateShownDisplay => FilteredCandidates.Count == ManualSeriesCandidates.Count
        ? $"{ManualSeriesCandidates.Count} comics"
        : $"{FilteredCandidates.Count} of {ManualSeriesCandidates.Count} shown";
    public bool HasNoCandidates => FilteredCandidates.Count == 0;
    public string CandidatesEmptyText => ManualSeriesCandidates.Count == 0
        ? "No comics are available here. Every comic already belongs somewhere."
        : "No comics match your search or filter.";

    private void ApplyCandidateFilter()
    {
        string query = ManualSeriesSearchQuery?.Trim() ?? string.Empty;
        IEnumerable<ManualSeriesComicItem> list = ManualSeriesCandidates.Where(item =>
            (query.Length == 0
             || item.Comic.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
             || item.Caption.Contains(query, StringComparison.OrdinalIgnoreCase)
             || (item.Comic.ParentFolder?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            && CandidateStatusFilter switch
            {
                "Unread" => !item.Comic.IsCompleted && item.Comic.LastReadPage <= 0,
                "Reading" => item.Comic.IsInProgress,
                "Finished" => item.Comic.IsCompleted,
                "Favorites" => item.Comic.IsFavorite,
                "Picked" => item.IsSelected,
                _ => true
            });

        list = CandidateSortIndex switch
        {
            1 => list.OrderByDescending(i => i.Comic.DateAdded),
            2 => list.OrderByDescending(i => i.Comic.LastReadAt ?? DateTime.MinValue),
            3 => list.OrderByDescending(i => i.Comic.PageCount),
            _ => list.OrderBy(i => i.Comic.Title, new NaturalSortComparer())
        };

        var shown = list.ToList();
        var visible = new HashSet<ManualSeriesComicItem>(shown);
        foreach (var item in ManualSeriesCandidates) item.IsVisible = visible.Contains(item);

        FilteredCandidates.Clear();
        foreach (var item in shown) FilteredCandidates.Add(item);
        OnPropertyChanged(nameof(CandidateShownDisplay));
        OnPropertyChanged(nameof(HasNoCandidates));
        OnPropertyChanged(nameof(CandidatesEmptyText));
    }

    /// <summary>
    /// Fills the picker. Creator collections only offer comics that aren't in any creator collection yet
    /// (hidden creators free their comics up); continuation series offer every comic not already in the series.
    /// </summary>
    private async Task BuildCandidatesAsync(bool creatorSection, ComicSeriesGroup? target)
    {
        ManualSeriesCandidates.Clear();
        FilteredCandidates.Clear();
        ManualSeriesSearchQuery = string.Empty;
        CandidateStatusFilter = "All";

        var allComics = await _repository.GetComicsAsync();
        var metadata = await _repository.GetAllComicMetadataAsync();
        var exclude = new HashSet<long>(target?.Issues.Select(i => i.Id) ?? Enumerable.Empty<long>());
        int alreadyInCreators = 0;

        if (creatorSection)
        {
            var manual = await _repository.GetManualSeriesAsync();
            var watched = await _repository.GetWatchedFoldersAsync();
            await LoadIgnoredSeriesKeysAsync();
            var options = new SeriesDetectionOptions
            {
                IgnoredSeriesKeys = new HashSet<string>(_ignoredSeriesKeys, StringComparer.Ordinal),
                RootFolders = watched.Select(w => w.Path).ToList()
            };
            var detection = await Task.Run(() => _seriesService.DetectAll(allComics, metadata, manual, options));
            foreach (var id in detection.Creators.SelectMany(g => g.Issues).Select(i => i.Id).Distinct())
            {
                if (exclude.Add(id)) alreadyInCreators++;
            }
        }

        foreach (var comic in allComics.Where(c => !c.IsMissing && !exclude.Contains(c.Id)).OrderBy(c => c.Title, new NaturalSortComparer()))
        {
            metadata.TryGetValue(comic.Id, out var meta);
            ManualSeriesCandidates.Add(new ManualSeriesComicItem(comic, ComicIdentityParser.Parse(comic, meta), isSelected: false));
        }

        CandidateScopeNote = creatorSection
            ? $"Showing comics that aren't in any creator collection yet{(alreadyInCreators > 0 ? $" ({alreadyInCreators} already belong to one)" : string.Empty)}. Hide a creator to free up its comics."
            : target != null
                ? "Showing every comic in your library that isn't in this series yet."
                : "Showing every comic in your library.";

        SelectedCandidateCount = 0;
        ApplyCandidateFilter();
    }

    // ───────────────────────── Edit series ─────────────────────────

    private ComicSeriesGroup? _editTarget;
    private readonly HashSet<long> _editRemovedIds = new();

    public ObservableCollection<EditSeriesComicItem> EditSeriesItems { get; } = new();

    [ObservableProperty]
    private bool _isEditSeriesDialogOpen;

    [ObservableProperty]
    private string _editSeriesName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditSeriesIsStory))]
    [NotifyPropertyChangedFor(nameof(EditSeriesKindHint))]
    [NotifyPropertyChangedFor(nameof(HasEditSeriesKindHint))]
    private bool _editSeriesIsCreator;

    /// <summary>
    /// A creator collection can become a continuation series, but a continuation series can't become a creator collection
    /// (creator collections only take comics that aren't in another creator collection).
    /// </summary>
    public bool EditSeriesCanBeCreator => _editTarget?.IsCreatorGroup == true;

    public string EditSeriesKindToolTip => EditSeriesCanBeCreator
        ? "Keep it in By Creator, or move it to Continuation Series"
        : "Continuation series stay continuation series. Only a By Creator collection can be moved here.";

    public string EditSeriesKindHint => !EditSeriesCanBeCreator
        ? "Continuation series can't be moved to By Creator."
        : !EditSeriesIsCreator
            ? "Moving to Continuation Series: once saved, it can't go back to By Creator."
            : string.Empty;

    public bool HasEditSeriesKindHint => EditSeriesKindHint.Length > 0;
    public string EditSeriesCreatorGlyph => EditSeriesCanBeCreator ? "\uE77B" : "\uE72E";

    [ObservableProperty]
    private bool _editSeriesAutoUpdate;

    public bool EditSeriesIsStory => !EditSeriesIsCreator;
    public string EditSeriesTitle => _editTarget?.IsManual == true ? "EDIT SERIES" : "EDIT & SAVE AS MANUAL";
    public string EditSeriesHint => _editTarget?.IsManual == true
        ? "Rename it, move it between sections, pick its cover and set the reading order. Changes apply when you save."
        : "This series was found automatically. Saving your changes turns it into a manual series you control.";
    public string EditSeriesCountDisplay => EditSeriesItems.Count == 1 ? "1 comic" : $"{EditSeriesItems.Count} comics";
    public string EditSeriesRemovedDisplay => _editRemovedIds.Count == 0 ? string.Empty : $"{_editRemovedIds.Count} removed on save";
    public string? EditSeriesCoverPath => EditSeriesItems.FirstOrDefault(i => i.IsCover)?.Comic.ThumbnailPath ?? EditSeriesItems.FirstOrDefault()?.Comic.ThumbnailPath;

    [RelayCommand]
    public void OpenEditSeries(ComicSeriesGroup? group)
    {
        var target = group ?? SelectedSeriesGroup;
        if (target == null) return;
        _editTarget = target;
        _editRemovedIds.Clear();
        OnPropertyChanged(nameof(EditSeriesCanBeCreator));
        OnPropertyChanged(nameof(EditSeriesKindToolTip));
        OnPropertyChanged(nameof(EditSeriesCreatorGlyph));
        EditSeriesName = target.SeriesName;
        EditSeriesIsCreator = target.IsCreatorGroup;
        EditSeriesAutoUpdate = target.IsManual ? target.IsAutoUpdate : true;

        long? coverId = GetCoverComicId(target);
        EditSeriesItems.Clear();
        foreach (var comic in target.Issues)
        {
            EditSeriesItems.Add(new EditSeriesComicItem(comic) { IsCover = coverId == comic.Id });
        }
        RenumberEditItems();
        OnPropertyChanged(nameof(EditSeriesTitle));
        OnPropertyChanged(nameof(EditSeriesHint));
        OnPropertyChanged(nameof(EditSeriesKindHint));
        OnPropertyChanged(nameof(HasEditSeriesKindHint));
        IsEditSeriesDialogOpen = true;
    }

    [RelayCommand]
    public void CloseEditSeries()
    {
        IsEditSeriesDialogOpen = false;
        EditSeriesItems.Clear();
        _editTarget = null;
    }

    [RelayCommand]
    public void SetEditKind(string? kind) =>
        EditSeriesIsCreator = EditSeriesCanBeCreator && string.Equals(kind, "creator", StringComparison.OrdinalIgnoreCase);

    public void MoveEditItem(EditSeriesComicItem item, int delta)
    {
        int index = EditSeriesItems.IndexOf(item);
        int target = Math.Clamp(index + delta, 0, EditSeriesItems.Count - 1);
        if (index < 0 || target == index) return;
        EditSeriesItems.Move(index, target);
        RenumberEditItems();
    }

    [RelayCommand]
    public void MoveEditItemUp(EditSeriesComicItem? item) { if (item != null) MoveEditItem(item, -1); }

    [RelayCommand]
    public void MoveEditItemDown(EditSeriesComicItem? item) { if (item != null) MoveEditItem(item, 1); }

    [RelayCommand]
    public void RemoveEditItem(EditSeriesComicItem? item)
    {
        if (item == null) return;
        EditSeriesItems.Remove(item);
        _editRemovedIds.Add(item.Comic.Id);
        if (item.IsCover && EditSeriesItems.Count > 0) EditSeriesItems[0].IsCover = true;
        RenumberEditItems();
    }

    [RelayCommand]
    public void SetEditCover(EditSeriesComicItem? item)
    {
        if (item == null) return;
        foreach (var i in EditSeriesItems) i.IsCover = ReferenceEquals(i, item);
        OnPropertyChanged(nameof(EditSeriesCoverPath));
    }

    [RelayCommand]
    public async Task SortEditItemsAutomaticallyAsync()
    {
        var metadata = await _repository.GetAllComicMetadataAsync();
        var ordered = SeriesDetectionService.OrderForReading(EditSeriesItems.Select(i => i.Comic), metadata, EditSeriesIsCreator);
        var byId = EditSeriesItems.ToDictionary(i => i.Comic.Id);
        EditSeriesItems.Clear();
        foreach (var comic in ordered) EditSeriesItems.Add(byId[comic.Id]);
        RenumberEditItems();
        ShowNotification("Reading Order Sorted", "Comics are in reading order (numbers, volumes and sequels).", InfoBarSeverity.Success);
    }

    /// <summary>Drag-and-drop reordering finished in the list.</summary>
    public void NotifyEditOrderChanged() => RenumberEditItems();

    private void RenumberEditItems()
    {
        for (int i = 0; i < EditSeriesItems.Count; i++) EditSeriesItems[i].Position = i + 1;
        OnPropertyChanged(nameof(EditSeriesCountDisplay));
        OnPropertyChanged(nameof(EditSeriesRemovedDisplay));
        OnPropertyChanged(nameof(EditSeriesCoverPath));
    }

    [RelayCommand]
    public async Task SaveEditSeriesAsync()
    {
        var target = _editTarget;
        if (target == null) return;
        string name = EditSeriesName?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            ShowNotification("Name Required", "Give the series a name before saving.", InfoBarSeverity.Warning);
            return;
        }
        if (EditSeriesItems.Count == 0)
        {
            ShowNotification("Series Is Empty", "Keep at least one comic, or delete the series instead.", InfoBarSeverity.Warning);
            return;
        }

        try
        {
            var ids = EditSeriesItems.Select(i => i.Comic.Id).ToList();
            var section = EditSeriesIsCreator && target.IsCreatorGroup ? SeriesSection.Creator : SeriesSection.Story;
            long manualId;
            string savedName = name;

            if (target.IsManual)
            {
                manualId = target.ManualSeriesId;
                if (!string.Equals(name, target.SeriesName, StringComparison.Ordinal))
                {
                    savedName = await _repository.RenameManualSeriesAsync(manualId, name);
                }
                await _repository.UpdateManualSeriesOptionsAsync(manualId, EditSeriesAutoUpdate, section);
                foreach (var removed in _editRemovedIds) await _repository.RemoveComicFromManualSeriesAsync(manualId, removed);
                await _repository.SetManualSeriesOrderAsync(manualId, ids);
            }
            else
            {
                manualId = await _repository.CreateManualSeriesAsync(name, ids, section, EditSeriesAutoUpdate, target.SeriesKey);
                foreach (var removed in _editRemovedIds) await _repository.RemoveComicFromManualSeriesAsync(manualId, removed);
                await CarryCoverToManualAsync(target, manualId);
            }

            await LoadCoverOverridesAsync();
            string coverKey = "manual:" + manualId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (EditSeriesItems.FirstOrDefault(i => i.IsCover) is { } cover) _coverOverrides[coverKey] = cover.Comic.Id;
            await SaveCoverOverridesAsync();

            IsEditSeriesDialogOpen = false;
            EditSeriesItems.Clear();
            _editTarget = null;

            if (IsCreatorSection != (section == SeriesSection.Creator)) IsCreatorSection = section == SeriesSection.Creator;
            await UpdateSeriesGroupsAsync();
            var fresh = SeriesGroups.FirstOrDefault(g => g.IsManual && g.ManualSeriesId == manualId);
            if (fresh != null)
            {
                SelectedSeriesGroup = fresh;
                IsSeriesDetailOpen = true;
            }

            ShowNotification("Series Saved", $"'{savedName}' is saved with {ids.Count} comic{(ids.Count == 1 ? "" : "s")}.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowNotification("Couldn't Save Series", ex.Message, InfoBarSeverity.Error);
        }
    }
}
