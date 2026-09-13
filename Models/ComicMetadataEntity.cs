using System;
using System.Collections.Generic;

namespace Komik.Models;

/// <summary>
/// Persisted extended metadata for a comic book (title, series, issue, credits, publisher, summary).
/// </summary>
public sealed class ComicMetadataEntity
{
    public long Id { get; set; }
    public long ComicId { get; set; }
    public string? Title { get; set; }
    public string? IssueNumber { get; set; }
    public string? SeriesName { get; set; }
    public string? Writers { get; set; }
    public string? Artists { get; set; }
    public string? Publisher { get; set; }
    public string? ReleaseDate { get; set; }
    public string? Summary { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public string FormattedCredits
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Writers)) parts.Add($"Writer: {Writers}");
            if (!string.IsNullOrWhiteSpace(Artists)) parts.Add($"Artist: {Artists}");
            return parts.Count > 0 ? string.Join(" • ", parts) : "No credits listed";
        }
    }

    public string FormattedSubtitle
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(SeriesName)) parts.Add(SeriesName);
            if (!string.IsNullOrWhiteSpace(IssueNumber)) parts.Add($"#{IssueNumber}");
            if (!string.IsNullOrWhiteSpace(ReleaseDate)) parts.Add($"({ReleaseDate})");
            return parts.Count > 0 ? string.Join(" ", parts) : string.Empty;
        }
    }
}
