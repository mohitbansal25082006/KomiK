using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Komik.Helpers;

namespace Komik.Models;

public enum SeriesSource
{
    FileName,
    Metadata,
    Folder,
    Manual,
    Creator
}

/// <summary>Which half of the Series screen a group belongs to.</summary>
public enum SeriesSection
{
    Story,
    Creator
}

/// <summary>
/// One issue/volume inside a series, with its parsed number label and reading state for the series detail view.
/// </summary>
public sealed class SeriesIssueItem
{
    public SeriesIssueItem(ComicEntity comic, ComicIdentity identity, bool isNextUp)
    {
        Comic = comic;
        Identity = identity;
        IsNextUp = isNextUp;
    }

    public ComicEntity Comic { get; }
    public ComicIdentity Identity { get; }
    public bool IsNextUp { get; }

    public string NumberLabel => string.IsNullOrEmpty(Identity.NumberLabel) ? "—" : Identity.NumberLabel;
    public bool HasNumberLabel => !string.IsNullOrEmpty(Identity.NumberLabel);
    public string Title => Comic.Title;
    public string? ThumbnailPath => Comic.ThumbnailPath;
    public string FormatBadge => Comic.FormatBadge;
    public string PageCountFormatted => Comic.PageCountFormatted;
    public double ProgressBarValue => Comic.ProgressBarValue;
    public bool IsInProgress => Comic.IsInProgress;
    public bool IsCompleted => Comic.IsCompleted;
    public string StatusText => Comic.IsCompleted ? "Read" : Comic.IsInProgress ? $"{Comic.ReadingProgressPercentage:F0}%" : "New";
}

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

    /// <summary>Normalized key used to hide/restore auto-detected series.</summary>
    public string SeriesKey { get; set; } = string.Empty;

    public SeriesSource Source { get; set; } = SeriesSource.FileName;

    /// <summary>Creator collections: how many different works (series or standalone books) they hold.</summary>
    public int WorkCount { get; set; }

    /// <summary>Creators credited on this series (most frequent first).</summary>
    public IReadOnlyList<string> Creators { get; set; } = Array.Empty<string>();

    /// <summary>Continuation series or creator collection (manual series keep the section they were made in).</summary>
    public SeriesSection Section { get; set; } = SeriesSection.Story;

    /// <summary>Manual series only: keep pulling in newly detected comics of the same series / creator.</summary>
    [ObservableProperty]
    private bool _isAutoUpdate;

    /// <summary>The auto-detected group key a manual series was saved from (used to keep it updated).</summary>
    public string? SourceKey { get; set; }

    /// <summary>Manual series only: comics the user removed, which auto-update must never add back.</summary>
    public HashSet<long> ExcludedComicIds { get; } = new();

    public bool IsCreatorGroup => Section == SeriesSection.Creator || Source == SeriesSource.Creator;
    public bool IsManualNotAutoUpdating => IsManual && !IsAutoUpdate;
    public bool IsManualAutoUpdating => IsManual && IsAutoUpdate;
    public string ModeDisplay => !IsManual ? "AUTO" : IsAutoUpdate ? "MANUAL · AUTO-UPDATE" : "MANUAL";
    public string ModeToolTip => !IsManual
        ? "Found automatically. Save it as manual to fix its name and order."
        : IsAutoUpdate
            ? "Manual, and new matching comics are added automatically."
            : "Manual: only the comics you choose. Turn on auto-update to add new matching comics.";

    partial void OnIsAutoUpdateChanged(bool value)
    {
        OnPropertyChanged(nameof(IsManualNotAutoUpdating));
        OnPropertyChanged(nameof(IsManualAutoUpdating));
        OnPropertyChanged(nameof(ModeDisplay));
        OnPropertyChanged(nameof(ModeToolTip));
        OnPropertyChanged(nameof(SourceDisplay));
    }

    partial void OnIsManualChanged(bool value)
    {
        OnPropertyChanged(nameof(IsManualNotAutoUpdating));
        OnPropertyChanged(nameof(IsManualAutoUpdating));
        OnPropertyChanged(nameof(ModeDisplay));
        OnPropertyChanged(nameof(ModeToolTip));
        OnPropertyChanged(nameof(IsAutoDetected));
        OnPropertyChanged(nameof(CanSaveAsManual));
    }
    public bool HasCreators => !IsCreatorGroup && Creators.Count > 0;
    public string CreatorsDisplay => Creators.Count == 0 ? string.Empty : "by " + string.Join(", ", Creators);
    public string WorkCountDisplay => WorkCount == 1 ? "1 work" : $"{WorkCount} works";
    public bool CanSaveAsManual => !IsManual;
    public bool ShowGapInfo => !IsCreatorGroup;
    public string HideActionText => IsCreatorGroup ? "Hide Creator" : "Not a Series";

    /// <summary>Second line on cards: works for creators; credits, years or formats for series.</summary>
    public string CardSubtitle => IsCreatorGroup
        ? $"{WorkCountDisplay} · {TotalPages:N0} pages"
        : Creators.Count > 0 ? CreatorsDisplay
        : !string.IsNullOrEmpty(_yearRange) ? _yearRange
        : FormatsDisplay;

    /// <summary>Optional metadata used when parsing issue numbers (by comic id).</summary>
    public IReadOnlyDictionary<long, ComicMetadataEntity>? Metadata { get; set; }

    public ObservableCollection<SeriesIssueItem> IssueItems { get; } = new();

    private List<string> _missingIssues = new();
    private int _volumeCount;
    private string _yearRange = string.Empty;
    private ComicEntity? _nextIssue;
    private string _nextIssueLabel = string.Empty;
    private string _numberKindPlural = "Issues";

    public int IssueCount => Issues.Count;

    public int TotalPages => Issues.Sum(i => i.PageCount);

    public int CompletedCount => Issues.Count(i => i.IsCompleted);

    public int InProgressCount => Issues.Count(i => !i.IsCompleted && i.LastReadPage > 0);

    public int UnreadCount => Issues.Count(i => !i.IsCompleted && i.LastReadPage == 0);

    public int PagesRead => Issues.Sum(i => i.IsCompleted ? i.PageCount : i.LastReadPage > 0 ? Math.Min(i.PageCount, i.LastReadPage + 1) : 0);

    /// <summary>Page-weighted progress through the whole series (partially read issues count).</summary>
    public double OverallProgress
    {
        get
        {
            if (Issues.Count == 0) return 0.0;
            int total = TotalPages;
            if (total <= 0) return (double)CompletedCount / Issues.Count;
            return Math.Clamp((double)PagesRead / total, 0, 1);
        }
    }

    public string ProgressPercentDisplay => $"{Math.Round(OverallProgress * 100)}%";

    public string IssueCountDisplay => IsCreatorGroup
        ? (Issues.Count == 1 ? "1 comic" : $"{Issues.Count} comics")
        : Issues.Count == 1 ? $"1 {_numberKindPlural.TrimEnd('s')}" : $"{Issues.Count} {_numberKindPlural}";

    public string FormatsDisplay => string.Join(" · ", Issues.Select(i => i.FormatBadge).Distinct());

    public DateTime? LastReadAt => Issues.Where(i => i.LastReadAt.HasValue).Select(i => i.LastReadAt).DefaultIfEmpty(null).Max();

    public int VolumeCount => _volumeCount;
    public string YearRangeDisplay => _yearRange;
    public bool HasYearRange => !string.IsNullOrEmpty(_yearRange);
    public IReadOnlyList<string> MissingIssues => _missingIssues;
    public bool HasMissingIssues => _missingIssues.Count > 0;

    public string MissingIssuesDisplay
    {
        get
        {
            if (_missingIssues.Count == 0) return "Complete run, no gaps";
            var shown = _missingIssues.Take(8).ToList();
            string more = _missingIssues.Count > shown.Count ? $" +{_missingIssues.Count - shown.Count} more" : string.Empty;
            return $"Missing {string.Join(", ", shown)}{more}";
        }
    }

    public string MissingBadgeText => _missingIssues.Count == 1 ? "1 gap" : $"{_missingIssues.Count} gaps";

    public string StatusDisplay => Issues.Count == 0
        ? "Empty"
        : CompletedCount == Issues.Count ? "Caught up"
        : CompletedCount == 0 && InProgressCount == 0 ? "Not started"
        : "Reading";

    public bool IsCaughtUp => Issues.Count > 0 && CompletedCount == Issues.Count;
    public bool IsNotStarted => Issues.Count > 0 && CompletedCount == 0 && InProgressCount == 0;
    public bool IsReading => !IsCaughtUp && !IsNotStarted && Issues.Count > 0;

    public string SourceDisplay => Source switch
    {
        SeriesSource.Manual => IsCreatorGroup
            ? (IsAutoUpdate ? "Manual creator · auto-update" : "Manual creator")
            : (IsAutoUpdate ? "Manual series · auto-update" : "Manual series"),
        SeriesSource.Metadata => "From metadata",
        SeriesSource.Folder => "From folder",
        SeriesSource.Creator => "By creator",
        _ => "Auto-detected"
    };

    public string SourceGlyph => Source switch
    {
        SeriesSource.Manual => "",
        SeriesSource.Metadata => "",
        SeriesSource.Folder => "",
        SeriesSource.Creator => "",
        _ => ""
    };

    public bool IsAutoDetected => !IsManual;

    public ComicEntity? NextIssueToRead => _nextIssue;

    public string NextIssueDisplay => _nextIssue == null
        ? "Read"
        : IsCaughtUp ? "Read Again"
        : _nextIssue.IsInProgress ? $"Continue {_nextIssueLabel}".Trim()
        : IsNotStarted ? (string.IsNullOrEmpty(_nextIssueLabel) ? "Start Reading" : $"Start {_nextIssueLabel}")
        : string.IsNullOrEmpty(_nextIssueLabel) ? "Read Next" : $"Read {_nextIssueLabel}";

    public string DetailSubtitle
    {
        get
        {
            var parts = new List<string> { IssueCountDisplay };
            if (IsCreatorGroup) parts.Add(WorkCountDisplay);
            if (_volumeCount > 1) parts.Add($"{_volumeCount} volumes");
            if (HasYearRange) parts.Add(_yearRange);
            parts.Add($"{TotalPages:N0} pages");
            parts.Add($"{CompletedCount} of {Issues.Count} read");
            return string.Join(" · ", parts);
        }
    }

    public void RefreshProperties()
    {
        RebuildDerivedState();

        OnPropertyChanged(nameof(IssueCount));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(InProgressCount));
        OnPropertyChanged(nameof(UnreadCount));
        OnPropertyChanged(nameof(PagesRead));
        OnPropertyChanged(nameof(OverallProgress));
        OnPropertyChanged(nameof(ProgressPercentDisplay));
        OnPropertyChanged(nameof(IssueCountDisplay));
        OnPropertyChanged(nameof(FormatsDisplay));
        OnPropertyChanged(nameof(NextIssueToRead));
        OnPropertyChanged(nameof(NextIssueDisplay));
        OnPropertyChanged(nameof(LastReadAt));
        OnPropertyChanged(nameof(VolumeCount));
        OnPropertyChanged(nameof(YearRangeDisplay));
        OnPropertyChanged(nameof(HasYearRange));
        OnPropertyChanged(nameof(MissingIssues));
        OnPropertyChanged(nameof(HasMissingIssues));
        OnPropertyChanged(nameof(MissingIssuesDisplay));
        OnPropertyChanged(nameof(MissingBadgeText));
        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(IsCaughtUp));
        OnPropertyChanged(nameof(IsNotStarted));
        OnPropertyChanged(nameof(IsReading));
        OnPropertyChanged(nameof(SourceDisplay));
        OnPropertyChanged(nameof(SourceGlyph));
        OnPropertyChanged(nameof(IsAutoDetected));
        OnPropertyChanged(nameof(DetailSubtitle));
        OnPropertyChanged(nameof(IsCreatorGroup));
        OnPropertyChanged(nameof(HasCreators));
        OnPropertyChanged(nameof(CreatorsDisplay));
        OnPropertyChanged(nameof(WorkCountDisplay));
        OnPropertyChanged(nameof(CanSaveAsManual));
        OnPropertyChanged(nameof(ShowGapInfo));
        OnPropertyChanged(nameof(HideActionText));
        OnPropertyChanged(nameof(CardSubtitle));
        OnPropertyChanged(nameof(ModeDisplay));
        OnPropertyChanged(nameof(ModeToolTip));
        OnPropertyChanged(nameof(IsManualNotAutoUpdating));
        OnPropertyChanged(nameof(IsManualAutoUpdating));
    }

    private void RebuildDerivedState()
    {
        var identities = Issues.ToDictionary(i => i, i =>
        {
            ComicMetadataEntity? meta = null;
            Metadata?.TryGetValue(i.Id, out meta);
            return ComicIdentityParser.Parse(i, meta);
        });

        // Volumes / chapters / issues wording
        int volumeOnly = identities.Values.Count(x => x.Kind == ComicNumberKind.Volume);
        int chapters = identities.Values.Count(x => x.Kind == ComicNumberKind.Chapter);
        _numberKindPlural = volumeOnly > Issues.Count / 2 ? "Volumes" : chapters > Issues.Count / 2 ? "Chapters" : "Issues";

        _volumeCount = identities.Values.Where(x => x.Volume.HasValue).Select(x => x.Volume!.Value).Distinct().Count();

        // Manual creator collections count their works the same way the detector does (one per distinct title).
        if (IsManual && IsCreatorGroup)
        {
            WorkCount = identities.Values.Select(x => x.SeriesKey.Length > 0 ? x.SeriesKey : x.CleanTitle).Distinct(StringComparer.Ordinal).Count();
        }

        var years = identities.Values.Where(x => x.Year.HasValue).Select(x => x.Year!.Value).ToList();
        _yearRange = years.Count == 0 ? string.Empty
            : years.Min() == years.Max() ? years.Min().ToString(CultureInfo.InvariantCulture)
            : $"{years.Min()}–{years.Max()}";

        _missingIssues = IsCreatorGroup ? new List<string>() : FindMissingNumbers(identities.Values);

        // A book counts as read when any copy of it (same series/volume/number) was finished,
        // so duplicate copies never send "Read Next" back to an issue you already read.
        var ordered = Issues.ToList();
        var finishedBooks = new HashSet<string>(identities
            .Where(kv => kv.Key.IsCompleted && kv.Value.BookKey != null)
            .Select(kv => kv.Value.BookKey!), StringComparer.Ordinal);
        bool IsDone(ComicEntity c) => c.IsCompleted || (identities[c].BookKey is { } key && finishedBooks.Contains(key));

        // Next up: most recently read unfinished issue; otherwise the first unread issue after the last one finished.
        ComicEntity? next = ordered
            .Where(i => i.IsInProgress && !IsDone(i))
            .OrderByDescending(i => i.LastReadAt ?? DateTime.MinValue)
            .FirstOrDefault();

        if (next == null)
        {
            int lastCompleted = ordered.FindLastIndex(IsDone);
            next = ordered.Skip(lastCompleted + 1).FirstOrDefault(i => !IsDone(i))
                   ?? ordered.FirstOrDefault(i => !IsDone(i))
                   ?? ordered.FirstOrDefault();
        }

        _nextIssue = next;
        _nextIssueLabel = !IsCreatorGroup && next != null && identities.TryGetValue(next, out var nextId) ? nextId.NumberLabel : string.Empty;

        IssueItems.Clear();
        foreach (var issue in ordered)
        {
            IssueItems.Add(new SeriesIssueItem(issue, identities[issue], ReferenceEquals(issue, next) && !ordered.All(IsDone)));
        }
    }

    /// <summary>Gaps in whole-numbered regular issues/chapters, per volume (e.g. has #1, #2, #4 → "#3").</summary>
    public static List<string> FindMissingNumbers(IEnumerable<ComicIdentity> identities)
    {
        var result = new List<string>();
        var list = identities.ToList();
        bool hasUnnumberedFirst = list.Any(x => !x.HasNumber);
        var regular = list
            .Where(x => x.Issue.HasValue && x.Kind is ComicNumberKind.Issue or ComicNumberKind.Chapter)
            .GroupBy(x => x.Volume);

        foreach (var volume in regular)
        {
            var numbers = volume
                .Select(x => x.Issue!.Value)
                .Where(n => n >= 0 && Math.Abs(n - Math.Round(n)) < 0.001)
                .Select(n => (int)Math.Round(n))
                .Concat(hasUnnumberedFirst && !volume.Key.HasValue ? new[] { 1 } : Array.Empty<int>())
                .Distinct()
                .OrderBy(n => n)
                .ToList();
            if (numbers.Count < 2) continue;

            // A run that starts a little after #1 is probably missing its opening issues;
            // one that starts far in (e.g. #500+) is a deliberate partial collection.
            int first = numbers[0];
            if (first > 1 && first - 1 <= numbers.Count) first = 1;
            int last = numbers[^1];
            if (last - first > 400) continue;

            var present = new HashSet<int>(numbers);
            bool chapter = volume.All(x => x.Kind == ComicNumberKind.Chapter);
            string prefix = volume.Key.HasValue ? $"Vol. {ComicIdentity.FormatNumber(volume.Key.Value)} " : string.Empty;
            for (int n = first; n <= last; n++)
            {
                if (!present.Contains(n)) result.Add(chapter ? $"{prefix}Ch. {n}" : $"{prefix}#{n}");
            }
        }

        return result;
    }
}
