namespace Komik.Models;

/// <summary>
/// Represents a user-defined tag in the library.
/// </summary>
public sealed class TagItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Represents a user-defined collection/series group in the library.
/// </summary>
public sealed class CollectionItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
