using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Komik.Models;

public partial class ComicSeriesGroup : ObservableObject
{
    [ObservableProperty]
    private string _seriesName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ComicEntity> _issues = new();

    [ObservableProperty]
    private string? _coverThumbnailPath;

    [ObservableProperty]
    private bool _isManual;

    [ObservableProperty]
    private long _manualSeriesId;

    public int IssueCount => Issues.Count;

    public int TotalPages => Issues.Sum(i => i.PageCount);

    public int CompletedCount => Issues.Count(i => i.IsCompleted);

    public int InProgressCount => Issues.Count(i => !i.IsCompleted && i.LastReadPage > 0);

    public int UnreadCount => Issues.Count(i => !i.IsCompleted && i.LastReadPage == 0);

    public double OverallProgress
    {
        get
        {
            if (Issues.Count == 0) return 0.0;
            return (double)CompletedCount / Issues.Count;
        }
    }

    public string ProgressPercentDisplay => $"{Math.Round(OverallProgress * 100)}%";

    public string IssueCountDisplay => Issues.Count == 1 ? "1 Issue" : $"{Issues.Count} Issues";

    public string FormatsDisplay => string.Join(", ", Issues.Select(i => i.Format.ToString().ToUpperInvariant()).Distinct());

    public ComicEntity? NextIssueToRead =>
        Issues.FirstOrDefault(i => !i.IsCompleted && i.LastReadPage > 0) ??
        Issues.FirstOrDefault(i => !i.IsCompleted) ??
        Issues.FirstOrDefault();

    public void RefreshProperties()
    {
        OnPropertyChanged(nameof(IssueCount));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(InProgressCount));
        OnPropertyChanged(nameof(UnreadCount));
        OnPropertyChanged(nameof(OverallProgress));
        OnPropertyChanged(nameof(ProgressPercentDisplay));
        OnPropertyChanged(nameof(IssueCountDisplay));
        OnPropertyChanged(nameof(FormatsDisplay));
        OnPropertyChanged(nameof(NextIssueToRead));
    }
}
