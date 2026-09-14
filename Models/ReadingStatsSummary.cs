using System;
using System.Collections.Generic;

namespace Komik.Models;

public sealed class DailyReadingActivity
{
    public string Date { get; set; } = string.Empty; // YYYY-MM-DD
    public int PagesRead { get; set; }
    public int DurationMinutes { get; set; }
}

public sealed class SeriesReadingStat
{
    public string SeriesName { get; set; } = string.Empty;
    public int PagesRead { get; set; }
    public int DurationMinutes { get; set; }
    public int IssuesCompleted { get; set; }

    public string FormattedPagesRead => $"{PagesRead:N0} pages read";
    public string FormattedDuration => DurationMinutes >= 60
        ? $"{DurationMinutes / 60}h {DurationMinutes % 60}m read"
        : $"{DurationMinutes} mins read";
    public string FormattedIssues => IssuesCompleted == 1 ? "1 issue" : $"{IssuesCompleted} issues";
}

public sealed class ReadingStatsSummary
{
    public int TotalPagesRead { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int TotalSessionsCount { get; set; }
    public int ComicsCompleted { get; set; }
    public int CurrentDailyStreak { get; set; }
    public int LongestDailyStreak { get; set; }
    public string TopSeriesName { get; set; } = "None";
    public int TopSeriesPages { get; set; }
    public string FormattedTotalTime
    {
        get
        {
            int hours = TotalDurationMinutes / 60;
            int mins = TotalDurationMinutes % 60;
            if (hours > 0)
            {
                return $"{hours}h {mins}m";
            }
            return $"{mins}m";
        }
    }
    public List<DailyReadingActivity> RecentActivity { get; set; } = new();
    public List<SeriesReadingStat> TopSeries { get; set; } = new();
}
