using System;

namespace Komik.Models;

/// <summary>
/// Represents a root folder watched and indexed by the Komik library.
/// </summary>
public sealed class WatchedFolder
{
    public long Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    public DateTime? LastScanned { get; set; }

    public string DateAddedFormatted => DateAdded.ToLocalTime().ToString("d");
    public string LastScannedFormatted => LastScanned.HasValue ? LastScanned.Value.ToLocalTime().ToString("g") : "Never";
}
