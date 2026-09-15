using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Komik.Models;

public sealed class DailyReadingActivity
{
    public string Date { get; set; } = string.Empty; // YYYY-MM-DD (local)
    public int PagesRead { get; set; }
    public int DurationMinutes { get; set; }
    public int DurationSeconds { get; set; }

    /// <summary>0..1 relative to the busiest day in the displayed range (for bar charts).</summary>
    public double Intensity { get; set; }

    public bool IsToday { get; set; }

    public DateTime DateValue => DateTime.TryParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : DateTime.MinValue;
    public string DayLabel => DateValue == DateTime.MinValue ? Date : DateValue.ToString("ddd", CultureInfo.CurrentCulture).Substring(0, 1);
    public string ShortDateLabel => DateValue == DateTime.MinValue ? Date : DateValue.ToString("MMM d", CultureInfo.CurrentCulture);
    public double BarHeight => 6 + Intensity * 94; // px, for the 100px chart
    public string ToolTipText => PagesRead == 0 && DurationSeconds == 0
        ? $"{ShortDateLabel}: no reading"
        : $"{ShortDateLabel}: {PagesRead:N0} pages · {ReadingStatsSummary.FormatDuration(DurationSeconds)}";
}

public sealed class SeriesReadingStat
{
    public string SeriesName { get; set; } = string.Empty;
    public int PagesRead { get; set; }
    public int DurationMinutes { get; set; }
    public int DurationSeconds { get; set; }
    public int IssuesCompleted { get; set; }
    public int IssuesRead { get; set; }
    public int Rank { get; set; }
    public double Share { get; set; } // 0..1 of the top entry
    public string? CoverThumbnailPath { get; set; }

    public string FormattedPagesRead => $"{PagesRead:N0} pages";
    public string FormattedDuration => ReadingStatsSummary.FormatDuration(DurationSeconds > 0 ? DurationSeconds : DurationMinutes * 60);
    public string FormattedIssues => (IssuesRead == 1 ? "1 issue read" : $"{IssuesRead} issues read") + (IssuesCompleted > 0 ? $" · {IssuesCompleted} finished" : string.Empty);
    public string RankDisplay => $"#{Rank}";
    public double ShareBarWidth => Math.Max(4, Share * 180);
}

public sealed class RecentlyReadComic
{
    public long ComicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public int LastReadPage { get; set; }
    public int PageCount { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime LastReadAtUtc { get; set; }

    public double Progress => PageCount <= 0 ? 0 : IsCompleted ? 1 : Math.Clamp((LastReadPage + 1) / (double)PageCount, 0, 1);
    public string ProgressDisplay => IsCompleted ? "Finished" : $"Page {LastReadPage + 1} of {PageCount}";
    public string LastReadDisplay => ReadingStatsSummary.FormatRelative(LastReadAtUtc);
}

public sealed class HourlyReadingActivity
{
    public int Hour { get; set; }
    public int Seconds { get; set; }
    public double Intensity { get; set; }
    public string Label => Hour % 6 == 0 ? DateTime.Today.AddHours(Hour).ToString("htt", CultureInfo.InvariantCulture).ToLowerInvariant() : string.Empty;
    public double BarHeight => 4 + Intensity * 56;
    public string ToolTipText => $"{DateTime.Today.AddHours(Hour):h tt}: {ReadingStatsSummary.FormatDuration(Seconds)}";
}

public sealed class ReadingStatsSummary
{
    // ── Lifetime ──
    public int TotalPagesRead { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int TotalDurationSeconds { get; set; }
    public int TotalSessionsCount { get; set; }
    public int ComicsCompleted { get; set; }
    public int ComicsInProgress { get; set; }
    public int ComicsUnread { get; set; }
    public int LibraryComicCount { get; set; }
    public int DistinctComicsRead { get; set; }
    public int CurrentDailyStreak { get; set; }
    public int LongestDailyStreak { get; set; }
    public int ActiveDays { get; set; }
    public string TopSeriesName { get; set; } = "None";
    public int TopSeriesPages { get; set; }

    // ── Periods (local time) ──
    public int TodayPages { get; set; }
    public int TodaySeconds { get; set; }
    public int WeekPages { get; set; }
    public int WeekSeconds { get; set; }
    public int MonthPages { get; set; }
    public int MonthSeconds { get; set; }
    public int YearPages { get; set; }
    public int YearSeconds { get; set; }
    public int YearComicsFinished { get; set; }
    public string YearTopSeriesName { get; set; } = "None";
    public string BusiestDayLabel { get; set; } = "—";
    public int BusiestDayPages { get; set; }
    public string FavoriteWeekday { get; set; } = "—";
    public string FavoriteTimeOfDay { get; set; } = "—";

    // ── Derived ──
    public double AverageSessionMinutes { get; set; }
    public double PagesPerHour { get; set; }
    public double LibraryCompletion { get; set; }
    public bool HasReadingData => TotalSessionsCount > 0;
    public bool HasNoReadingData => !HasReadingData;
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public int Year { get; set; } = DateTime.Now.Year;

    public List<DailyReadingActivity> RecentActivity { get; set; } = new();
    public List<DailyReadingActivity> Last30Days { get; set; } = new();
    public List<HourlyReadingActivity> HourlyActivity { get; set; } = new();
    public List<SeriesReadingStat> TopSeries { get; set; } = new();
    public List<RecentlyReadComic> RecentlyRead { get; set; } = new();

    public string FormattedTotalTime => FormatDuration(TotalDurationSeconds > 0 ? TotalDurationSeconds : TotalDurationMinutes * 60);
    public string FormattedTodayTime => FormatDuration(TodaySeconds);
    public string FormattedWeekTime => FormatDuration(WeekSeconds);
    public string FormattedMonthTime => FormatDuration(MonthSeconds);
    public string FormattedYearTime => FormatDuration(YearSeconds);
    public string TotalPagesDisplay => TotalPagesRead.ToString("N0", CultureInfo.CurrentCulture);
    public string TodayPagesDisplay => TodayPages.ToString("N0", CultureInfo.CurrentCulture);
    public string WeekPagesDisplay => WeekPages.ToString("N0", CultureInfo.CurrentCulture);
    public string MonthPagesDisplay => MonthPages.ToString("N0", CultureInfo.CurrentCulture);
    public string YearPagesDisplay => YearPages.ToString("N0", CultureInfo.CurrentCulture);
    public string ComicsCompletedDisplay => ComicsCompleted.ToString("N0", CultureInfo.CurrentCulture);
    public string StreakDisplay => CurrentDailyStreak == 1 ? "1 day" : $"{CurrentDailyStreak} days";
    public string LongestStreakDisplay => LongestDailyStreak == 1 ? "1 day" : $"{LongestDailyStreak} days";
    public string AverageSessionDisplay => AverageSessionMinutes <= 0 ? "—" : FormatDuration((int)Math.Round(AverageSessionMinutes * 60));
    public string PagesPerHourDisplay => PagesPerHour <= 0 ? "—" : $"{PagesPerHour:N0} pages/hr";
    public string LibraryCompletionDisplay => $"{Math.Round(LibraryCompletion * 100)}%";
    public string LibraryBreakdownDisplay => $"{ComicsCompleted:N0} finished · {ComicsInProgress:N0} reading · {ComicsUnread:N0} unread";
    public string SessionsDisplay => TotalSessionsCount == 1 ? "1 session" : $"{TotalSessionsCount:N0} sessions";
    public string ActiveDaysDisplay => ActiveDays == 1 ? "1 active day" : $"{ActiveDays:N0} active days";
    public string DistinctComicsDisplay => DistinctComicsRead == 1 ? "1 comic opened" : $"{DistinctComicsRead:N0} comics opened";
    public string YearHeadline => $"Your {Year} in Komik";
    public string UpdatedDisplay => $"Live · updated {GeneratedAtUtc.ToLocalTime():t}";
    public string Last30DaysPagesDisplay => $"{Last30Days.Sum(d => d.PagesRead):N0} pages in the last 30 days";

    public static string FormatDuration(int totalSeconds)
    {
        if (totalSeconds <= 0) return "0m";
        if (totalSeconds < 60) return $"{totalSeconds}s";
        int totalMinutes = (int)Math.Round(totalSeconds / 60.0);
        int hours = totalMinutes / 60;
        int mins = totalMinutes % 60;
        return hours > 0 ? $"{hours}h {mins}m" : $"{mins}m";
    }

    public static string FormatRelative(DateTime utc)
    {
        if (utc == DateTime.MinValue) return string.Empty;
        TimeSpan ago = DateTime.UtcNow - utc;
        if (ago.TotalMinutes < 1) return "just now";
        if (ago.TotalMinutes < 60) return $"{(int)ago.TotalMinutes}m ago";
        if (ago.TotalHours < 24) return $"{(int)ago.TotalHours}h ago";
        if (ago.TotalDays < 2) return "yesterday";
        if (ago.TotalDays < 7) return $"{(int)ago.TotalDays} days ago";
        return utc.ToLocalTime().ToString("MMM d", CultureInfo.CurrentCulture);
    }
}
