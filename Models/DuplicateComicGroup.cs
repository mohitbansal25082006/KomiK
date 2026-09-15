using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Komik.Models;

/// <summary>How sure the detector is that the copies are the same book.</summary>
public enum DuplicateConfidence
{
    Possible = 60,
    Likely = 75,
    SameIssue = 90,
    Identical = 100
}

/// <summary>One copy inside a duplicate group, with the reasons it is (or isn't) the copy to keep.</summary>
public sealed partial class DuplicateCopyItem : ObservableObject
{
    public DuplicateCopyItem(ComicEntity comic)
    {
        Comic = comic;
    }

    public ComicEntity Comic { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KeepBadgeVisible), nameof(RoleText))]
    private bool _isRecommended;

    [ObservableProperty]
    private string _highlights = string.Empty;

    public bool KeepBadgeVisible => IsRecommended;
    public string RoleText => IsRecommended ? "Best copy" : "Extra copy";

    public string Title => Comic.Title;
    public string FilePath => Comic.FilePath;
    public string? ThumbnailPath => Comic.ThumbnailPath;
    public string FormatBadge => Comic.FormatBadge;
    public string PageCountFormatted => Comic.PageCountFormatted;
    public string FileSizeFormatted => Comic.FileSizeFormatted;
    public string ProgressText => Comic.ProgressText;
    public string FolderDisplay
    {
        get
        {
            try
            {
                return System.IO.Path.GetDirectoryName(Comic.FilePath) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}

public partial class DuplicateComicGroup : ObservableObject
{
    [ObservableProperty]
    private string _groupTitle = string.Empty;

    [ObservableProperty]
    private string _matchReason = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ComicEntity> _copies = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConfidenceLabel), nameof(ConfidencePercent), nameof(IsHighConfidence))]
    private DuplicateConfidence _confidence = DuplicateConfidence.Possible;

    public ObservableCollection<DuplicateCopyItem> CopyItems { get; } = new();

    public int CopyCount => Copies.Count;

    public ComicEntity? RecommendedKeep => CopyItems.FirstOrDefault(c => c.IsRecommended)?.Comic;

    public string ConfidenceLabel => Confidence switch
    {
        DuplicateConfidence.Identical => "Identical files",
        DuplicateConfidence.SameIssue => "Same issue",
        DuplicateConfidence.Likely => "Likely duplicate",
        _ => "Possible duplicate"
    };

    public int ConfidencePercent => (int)Confidence;

    public bool IsHighConfidence => Confidence >= DuplicateConfidence.SameIssue;

    /// <summary>Disk space freed by keeping only the recommended copy.</summary>
    public long ReclaimableBytes => CopyItems.Where(c => !c.IsRecommended).Sum(c => c.Comic.FileSize);

    public string ReclaimableDisplay => FormatBytes(ReclaimableBytes);

    public string CopyCountDisplay => Copies.Count == 1 ? "1 copy" : $"{Copies.Count} copies";

    public string FormatsDisplay => string.Join(" vs ", Copies.Select(c => c.FormatBadge).Distinct());

    public void Refresh()
    {
        OnPropertyChanged(nameof(CopyCount));
        OnPropertyChanged(nameof(RecommendedKeep));
        OnPropertyChanged(nameof(ReclaimableBytes));
        OnPropertyChanged(nameof(ReclaimableDisplay));
        OnPropertyChanged(nameof(CopyCountDisplay));
        OnPropertyChanged(nameof(FormatsDisplay));
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 KB";
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return unit <= 1 ? $"{Math.Ceiling(value)} {units[unit]}" : $"{value:0.#} {units[unit]}";
    }
}
