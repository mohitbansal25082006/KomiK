using System;

namespace Komik.Models;

/// <summary>
/// Consolidated application preferences persisted locally in SQLite.
/// </summary>
public sealed class AppSettings
{
    public string Theme { get; set; } = "Default"; // Default (System), Light, Dark
    public string DefaultFitMode { get; set; } = "FitToHeight";
    public string DefaultReadingDirection { get; set; } = "LeftToRight";
    public string DefaultViewMode { get; set; } = "Grid";
    public int DefaultSortOption { get; set; } = 0; // LibrarySortOption.TitleAscending
    public double DefaultBrightness { get; set; } = 0.0;
    public double DefaultContrast { get; set; } = 1.0;
    public double DefaultWarmth { get; set; } = 0.0;
    public bool DefaultNightMode { get; set; } = false;
}
