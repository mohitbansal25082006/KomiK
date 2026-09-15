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

namespace Komik.ViewModels;

/// <summary>Tag screens: the library tag filter, a comic's tags and tag management. Built to stay quick with hundreds of tags.</summary>
public partial class LibraryViewModel
{
    private readonly List<TagChipItem> _allTagItems = new();

    public List<string> TagSortOptions { get; } = new() { "Name (A to Z)", "Most used", "Least used", "Newest first" };
    public List<string> TagUsageFilters { get; } = new() { "All", "Used", "Unused" };
    public List<string> ComicTagScopes { get; } = new() { "All tags", "On this comic", "Not on this comic" };

    // ── Library tag filter flyout ──

    public ObservableCollection<TagChipItem> TagFilterItems { get; } = new();

    [ObservableProperty]
    private string _tagFilterSearchText = string.Empty;

    partial void OnTagFilterSearchTextChanged(string value) => ApplyTagFilterList();

    public string TagFilterButtonText => string.IsNullOrEmpty(SelectedTag) ? "Tags" : $"Tag: {SelectedTag}";
    public bool HasTagFilterResults => TagFilterItems.Count > 0;
    public string TagFilterEmptyText => _allTagItems.Count == 0 ? "No tags yet. Create one in Manage Tags." : $"No tags match \"{TagFilterSearchText}\".";

    // ── Manage tags overlay ──

    public ObservableCollection<TagChipItem> ManageTagItems { get; } = new();

    [ObservableProperty]
    private bool _isManageTagsOpen;

    [ObservableProperty]
    private string _manageTagSearchText = string.Empty;

    [ObservableProperty]
    private int _manageTagSortIndex;

    [ObservableProperty]
    private string _manageTagUsageFilter = "All";

    [ObservableProperty]
    private string _newTagName = string.Empty;

    partial void OnManageTagSearchTextChanged(string value) => ApplyManageTagList();
    partial void OnManageTagSortIndexChanged(int value) => ApplyManageTagList();
    partial void OnManageTagUsageFilterChanged(string value) => ApplyManageTagList();

    public int TagTotalCount => _allTagItems.Count;
    public int UnusedTagCount => _allTagItems.Count(t => t.IsUnused);
    public bool HasUnusedTags => UnusedTagCount > 0;
    public string TagSummaryDisplay => _allTagItems.Count == 0
        ? "No tags yet"
        : $"{_allTagItems.Count} tag{(_allTagItems.Count == 1 ? "" : "s")} · {_allTagItems.Count(t => !t.IsUnused)} in use · {UnusedTagCount} unused";
    public string DeleteUnusedTagsText => $"Delete {UnusedTagCount} unused";
    public bool HasManageTagResults => ManageTagItems.Count > 0;
    public string ManageTagShownDisplay => $"{ManageTagItems.Count} of {_allTagItems.Count} shown";
    public string ManageTagEmptyText => _allTagItems.Count == 0
        ? "No tags yet. Type a name above to create your first tag."
        : "No tags match your search or filter.";

    // ── Comic tags overlay ──

    public ObservableCollection<TagChipItem> ComicTagItems { get; } = new();
    public ObservableCollection<TagChipItem> ComicAssignedTags { get; } = new();

    [ObservableProperty]
    private bool _isComicTagsOpen;

    [ObservableProperty]
    private ComicEntity? _tagEditingComic;

    [ObservableProperty]
    private string _comicTagSearchText = string.Empty;

    [ObservableProperty]
    private string _comicTagScope = "All tags";

    [ObservableProperty]
    private int _comicTagSortIndex;

    partial void OnComicTagSearchTextChanged(string value) => ApplyComicTagList();
    partial void OnComicTagScopeChanged(string value) => ApplyComicTagList();
    partial void OnComicTagSortIndexChanged(int value) => ApplyComicTagList();

    public bool CanCreateTagFromSearch
    {
        get
        {
            string name = ComicTagSearchText.Trim();
            return name.Length > 0 && !_allTagItems.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public string CreateTagFromSearchText => $"Create \"{ComicTagSearchText.Trim()}\" and add it";
    public bool CanCreateManagedTag => NewTagName.Trim().Length > 0;
    partial void OnNewTagNameChanged(string value) => OnPropertyChanged(nameof(CanCreateManagedTag));
    public bool HasComicTagResults => ComicTagItems.Count > 0;
    public bool ComicHasNoTags => ComicAssignedTags.Count == 0;
    public string ComicTagCountDisplay => ComicAssignedTags.Count == 1 ? "1 tag on this comic" : $"{ComicAssignedTags.Count} tags on this comic";
    public string ComicTagEmptyText => _allTagItems.Count == 0
        ? "No tags yet. Type a name above to create one."
        : string.IsNullOrWhiteSpace(ComicTagSearchText) ? "Nothing here with this filter." : $"No tags match \"{ComicTagSearchText.Trim()}\".";

    /// <summary>Reloads tag names and usage counts, keeping every open tag screen in step.</summary>
    public async Task RefreshTagUsageAsync()
    {
        var usage = await _repository.GetTagUsageAsync();
        var selected = TagEditingComic != null
            ? new HashSet<string>(await _repository.GetTagsForComicAsync(TagEditingComic.Id), StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _allTagItems.Clear();
        _tagCreationOrder.Clear();
        int order = 0;
        foreach (var (name, count) in usage)
        {
            _allTagItems.Add(new TagChipItem(name, count, selected.Contains(name)));
            _tagCreationOrder[name] = order;
            order++;
        }

        AvailableTags.Clear();
        foreach (var t in _allTagItems) AvailableTags.Add(t.Name);

        ApplyTagFilterList();
        ApplyManageTagList();
        ApplyComicTagList();
        NotifyTagSummary();
    }

    private readonly Dictionary<string, int> _tagCreationOrder = new(StringComparer.OrdinalIgnoreCase);

    private IEnumerable<TagChipItem> SortTags(IEnumerable<TagChipItem> tags, int sortIndex) => sortIndex switch
    {
        1 => tags.OrderByDescending(t => t.Count).ThenBy(t => t.Name, new NaturalSortComparer()),
        2 => tags.OrderBy(t => t.Count).ThenBy(t => t.Name, new NaturalSortComparer()),
        3 => tags.OrderByDescending(t => _tagCreationOrder.TryGetValue(t.Name, out var o) ? o : 0),
        _ => tags.OrderBy(t => t.Name, new NaturalSortComparer())
    };

    private static bool MatchesTag(TagChipItem tag, string query) =>
        query.Length == 0 || tag.Name.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static void Sync(ObservableCollection<TagChipItem> target, IEnumerable<TagChipItem> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }

    private void ApplyTagFilterList()
    {
        string q = TagFilterSearchText.Trim();
        Sync(TagFilterItems, _allTagItems.Where(t => MatchesTag(t, q))
            .OrderByDescending(t => string.Equals(t.Name, SelectedTag, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(t => t.Count > 0)
            .ThenBy(t => t.Name, new NaturalSortComparer()));
        OnPropertyChanged(nameof(HasTagFilterResults));
        OnPropertyChanged(nameof(TagFilterEmptyText));
        OnPropertyChanged(nameof(TagFilterButtonText));
    }

    private void ApplyManageTagList()
    {
        string q = ManageTagSearchText.Trim();
        var list = _allTagItems.Where(t => MatchesTag(t, q) && ManageTagUsageFilter switch
        {
            "Used" => !t.IsUnused,
            "Unused" => t.IsUnused,
            _ => true
        });
        Sync(ManageTagItems, SortTags(list, ManageTagSortIndex));
        OnPropertyChanged(nameof(HasManageTagResults));
        OnPropertyChanged(nameof(ManageTagShownDisplay));
        OnPropertyChanged(nameof(ManageTagEmptyText));
    }

    private void ApplyComicTagList()
    {
        string q = ComicTagSearchText.Trim();
        var list = _allTagItems.Where(t => MatchesTag(t, q) && ComicTagScope switch
        {
            "On this comic" => t.IsSelected,
            "Not on this comic" => !t.IsSelected,
            _ => true
        });
        Sync(ComicTagItems, SortTags(list, ComicTagSortIndex));
        Sync(ComicAssignedTags, _allTagItems.Where(t => t.IsSelected).OrderBy(t => t.Name, new NaturalSortComparer()));
        OnPropertyChanged(nameof(HasComicTagResults));
        OnPropertyChanged(nameof(ComicHasNoTags));
        OnPropertyChanged(nameof(ComicTagCountDisplay));
        OnPropertyChanged(nameof(ComicTagEmptyText));
        OnPropertyChanged(nameof(CanCreateTagFromSearch));
        OnPropertyChanged(nameof(CreateTagFromSearchText));
    }

    private void NotifyTagSummary()
    {
        OnPropertyChanged(nameof(TagTotalCount));
        OnPropertyChanged(nameof(UnusedTagCount));
        OnPropertyChanged(nameof(HasUnusedTags));
        OnPropertyChanged(nameof(TagSummaryDisplay));
        OnPropertyChanged(nameof(DeleteUnusedTagsText));
    }

    // ── Commands ──

    [RelayCommand]
    public void ApplyTagFilter(TagChipItem? tag)
    {
        FilterByTag(tag?.Name);
        if (IsSeriesView) CloseSeriesView();
        ApplyTagFilterList();
    }

    [RelayCommand]
    public async Task OpenManageTagsAsync()
    {
        ManageTagSearchText = string.Empty;
        ManageTagUsageFilter = "All";
        NewTagName = string.Empty;
        TagEditingComic = null;
        await RefreshTagUsageAsync();
        IsManageTagsOpen = true;
    }

    [RelayCommand]
    public void CloseManageTags() => IsManageTagsOpen = false;

    [RelayCommand]
    public async Task CreateManagedTagAsync()
    {
        string name = NewTagName.Trim();
        if (name.Length == 0) return;
        bool exists = _allTagItems.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        await _repository.CreateTagAsync(name);
        NewTagName = string.Empty;
        await RefreshTagUsageAsync();
        ShowNotification(exists ? "Tag Already Exists" : "Tag Created", exists ? $"'{name}' is already one of your tags." : $"Created tag '{name}'.", exists ? InfoBarSeverity.Informational : InfoBarSeverity.Success);
    }

    public async Task RenameTagAsync(TagChipItem tag, string newName)
    {
        string to = newName.Trim();
        if (to.Length == 0 || to == tag.Name) return;
        bool merge = _allTagItems.Any(t => !ReferenceEquals(t, tag) && string.Equals(t.Name, to, StringComparison.OrdinalIgnoreCase));
        await _repository.RenameTagAsync(tag.Name, to);
        if (string.Equals(SelectedTag, tag.Name, StringComparison.OrdinalIgnoreCase)) SelectedTag = to;
        await RefreshTagUsageAsync();
        await ReloadComicsAsync();
        ShowNotification(merge ? "Tags Merged" : "Tag Renamed", merge ? $"'{tag.Name}' was merged into '{to}'." : $"'{tag.Name}' is now '{to}'.", InfoBarSeverity.Success);
    }

    [RelayCommand]
    public async Task DeleteTagItemAsync(TagChipItem? tag)
    {
        if (tag == null) return;
        await DeleteTagAsync(tag.Name);
        await RefreshTagUsageAsync();
        ShowNotification("Tag Deleted", $"Deleted '{tag.Name}'" + (tag.Count > 0 ? $" from {tag.CountDisplay}." : "."), InfoBarSeverity.Informational);
    }

    [RelayCommand]
    public async Task DeleteUnusedTagsAsync()
    {
        var unused = _allTagItems.Where(t => t.IsUnused).Select(t => t.Name).ToList();
        foreach (var name in unused) await _repository.DeleteTagAsync(name);
        await RefreshTagsAndCollectionsAsync();
        await RefreshTagUsageAsync();
        ShowNotification("Unused Tags Deleted", unused.Count == 1 ? "Deleted 1 unused tag." : $"Deleted {unused.Count} unused tags.", InfoBarSeverity.Success);
    }

    [RelayCommand]
    public void ShowComicsWithTag(TagChipItem? tag)
    {
        if (tag == null) return;
        IsManageTagsOpen = false;
        ApplyTagFilter(tag);
    }

    [RelayCommand]
    public async Task OpenComicTagsAsync(ComicEntity? comic)
    {
        if (comic == null) return;
        TagEditingComic = comic;
        ComicTagSearchText = string.Empty;
        ComicTagScope = "All tags";
        await RefreshTagUsageAsync();
        IsComicTagsOpen = true;
    }

    [RelayCommand]
    public void CloseComicTags()
    {
        IsComicTagsOpen = false;
        TagEditingComic = null;
        _ = RefreshTagUsageAsync();
    }

    /// <summary>Adds or removes a tag on the comic being tagged (applies instantly).</summary>
    [RelayCommand]
    public async Task ToggleComicTagAsync(TagChipItem? tag)
    {
        if (tag == null || TagEditingComic == null) return;
        bool add = !tag.IsSelected;
        if (add) await AddTagToComicAsync(TagEditingComic, tag.Name);
        else await RemoveTagFromComicAsync(TagEditingComic, tag.Name);
        tag.IsSelected = add;
        tag.Count = Math.Max(0, tag.Count + (add ? 1 : -1));
        ApplyComicTagList();
        NotifyTagSummary();
    }

    [RelayCommand]
    public async Task CreateTagFromSearchAsync()
    {
        string name = ComicTagSearchText.Trim();
        if (name.Length == 0 || TagEditingComic == null) return;
        await AddTagToComicAsync(TagEditingComic, name);
        ComicTagSearchText = string.Empty;
        await RefreshTagUsageAsync();
    }
}
