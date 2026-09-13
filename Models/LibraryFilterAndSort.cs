namespace Komik.Models;

/// <summary>
/// Specifies how comics in the library should be ordered.
/// </summary>
public enum LibrarySortOption
{
    TitleAscending,
    TitleDescending,
    DateAddedDescending,
    DateAddedAscending,
    PageCountDescending,
    FileSizeDescending,
    LastReadDescending
}

/// <summary>
/// Filter criteria applied when querying the library.
/// </summary>
public sealed class LibraryFilter
{
    public string? SearchQuery { get; set; }
    public bool FavoritesOnly { get; set; }
    public bool InProgressOnly { get; set; }
    public bool UnreadOnly { get; set; }
    public bool CompletedOnly { get; set; }
    public bool IncludeMissing { get; set; }
    public string? SelectedTag { get; set; }
    public string? SelectedCollection { get; set; }
    public ComicSourceType? SelectedFormat { get; set; }
}
