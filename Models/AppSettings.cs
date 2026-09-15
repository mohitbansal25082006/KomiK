using System;

namespace Komik.Models;

/// <summary>
/// Consolidated application preferences persisted locally in SQLite.
/// </summary>
public sealed class AppSettings
{
    public string Theme { get; set; } = "Light"; // Light (default), Dark or Default (follow Windows)
    public string DefaultFitMode { get; set; } = "FitToHeight";
    public string DefaultReadingDirection { get; set; } = "LeftToRight";
    public string DefaultReaderViewMode { get; set; } = "SinglePage"; // SinglePage, DoublePage or Webtoon
    public string DefaultViewMode { get; set; } = "Grid";
    public int DefaultSortOption { get; set; } = 0; // LibrarySortOption.TitleAscending
    public double DefaultBrightness { get; set; } = 0.0;
    public double DefaultContrast { get; set; } = 1.0;
    public double DefaultWarmth { get; set; } = 0.0;
    public bool DefaultNightMode { get; set; } = false;
    public string DefaultReadingPreset { get; set; } = "Original";
    public int WindowWidth { get; set; } = 1240;
    public int WindowHeight { get; set; } = 820;
    public int WindowX { get; set; } = -1;
    public int WindowY { get; set; } = -1;
    public bool IsMaximized { get; set; } = false;
    public bool IsSeriesViewDefault { get; set; } = false;
}
