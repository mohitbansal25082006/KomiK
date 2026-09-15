using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Komik.Helpers;
using Komik.Models;

namespace Komik.Services;

/// <summary>A stored reading session row.</summary>
public sealed record ReadingSessionRecord(long ComicId, DateTime StartUtc, int DurationSeconds, int PagesRead);

/// <summary>Which series a comic belongs to, as shown on the Series screen.</summary>
public sealed record SeriesMembership(string Key, string Name, int TotalIssues);

/// <summary>
/// Turns raw reading sessions and library state into <see cref="ReadingStatsSummary"/>.
/// Pure and time-zone aware: every "day" is a local calendar day, and nothing is estimated or invented.
/// </summary>
public static class ReadingStatsCalculator
{
    public static ReadingStatsSummary Calculate(
        IReadOnlyList<ReadingSessionRecord> sessions,
        IReadOnlyList<ComicEntity> comics,
        IReadOnlyDictionary<long, ComicMetadataEntity>? metadata,
        DateTime nowUtc,
        TimeZoneInfo? zone = null,
        IReadOnlyDictionary<long, SeriesMembership>? membership = null)
    {
        zone ??= TimeZoneInfo.Local;
        DateTime nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc), zone);
        DateTime today = nowLocal.Date;

        DateTime LocalOf(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(ToUtc(utc), zone);

        var comicById = comics.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());
        var valid = sessions.Where(s => s.DurationSeconds > 0 || s.PagesRead > 0).ToList();

        var summary = new ReadingStatsSummary
        {
            GeneratedAtUtc = nowUtc,
            Year = today.Year,
            TotalSessionsCount = valid.Count,
            TotalPagesRead = valid.Sum(s => Math.Max(0, s.PagesRead)),
            TotalDurationSeconds = valid.Sum(s => Math.Max(0, s.DurationSeconds)),
            LibraryComicCount = comics.Count,
            ComicsCompleted = comics.Count(c => c.IsCompleted),
            ComicsInProgress = comics.Count(c => !c.IsCompleted && c.LastReadPage > 0),
            ComicsUnread = comics.Count(c => !c.IsCompleted && c.LastReadPage <= 0),
            DistinctComicsRead = valid.Select(s => s.ComicId).Distinct().Count()
        };
        summary.TotalDurationMinutes = (int)Math.Round(summary.TotalDurationSeconds / 60.0);
        summary.LibraryCompletion = comics.Count == 0 ? 0 : (double)summary.ComicsCompleted / comics.Count;
        summary.YearComicsFinished = comics.Count(c => c.IsCompleted && c.LastReadAt.HasValue && LocalOf(ToUtc(c.LastReadAt.Value)).Year == today.Year);

        // ── Per local day ──
        var byDay = valid
            .GroupBy(s => LocalOf(s.StartUtc).Date)
            .ToDictionary(g => g.Key, g => (Pages: g.Sum(s => s.PagesRead), Seconds: g.Sum(s => s.DurationSeconds)));

        summary.ActiveDays = byDay.Count;

        DateTime weekStart = today.AddDays(-(((int)today.DayOfWeek - (int)CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek + 7) % 7));
        foreach (var (day, totals) in byDay)
        {
            if (day == today)
            {
                summary.TodayPages += totals.Pages;
                summary.TodaySeconds += totals.Seconds;
            }
            if (day >= weekStart && day <= today)
            {
                summary.WeekPages += totals.Pages;
                summary.WeekSeconds += totals.Seconds;
            }
            if (day.Year == today.Year && day.Month == today.Month)
            {
                summary.MonthPages += totals.Pages;
                summary.MonthSeconds += totals.Seconds;
            }
            if (day.Year == today.Year)
            {
                summary.YearPages += totals.Pages;
                summary.YearSeconds += totals.Seconds;
            }
        }

        // Last 30 days (zero-filled, oldest first) and the legacy 14-day list (newest first).
        int maxPages30 = 0;
        for (int i = 29; i >= 0; i--)
        {
            DateTime day = today.AddDays(-i);
            byDay.TryGetValue(day, out var totals);
            maxPages30 = Math.Max(maxPages30, totals.Pages);
            summary.Last30Days.Add(new DailyReadingActivity
            {
                Date = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                PagesRead = totals.Pages,
                DurationSeconds = totals.Seconds,
                DurationMinutes = (int)Math.Round(totals.Seconds / 60.0),
                IsToday = day == today
            });
        }

        int maxSeconds30 = summary.Last30Days.Max(d => d.DurationSeconds);
        foreach (var d in summary.Last30Days)
        {
            // Bars follow pages; days with time but no page turns (e.g. one long page) still show.
            double byPages = maxPages30 > 0 ? (double)d.PagesRead / maxPages30 : 0;
            double bySeconds = maxSeconds30 > 0 ? (double)d.DurationSeconds / maxSeconds30 : 0;
            d.Intensity = Math.Max(byPages, bySeconds * 0.35);
        }

        summary.RecentActivity = byDay
            .OrderByDescending(k => k.Key)
            .Take(14)
            .Select(k => new DailyReadingActivity
            {
                Date = k.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                PagesRead = k.Value.Pages,
                DurationSeconds = k.Value.Seconds,
                DurationMinutes = (int)Math.Round(k.Value.Seconds / 60.0),
                IsToday = k.Key == today
            })
            .ToList();

        // ── Streaks ──
        var days = byDay.Keys.OrderBy(d => d).ToList();
        if (days.Count > 0)
        {
            var set = days.ToHashSet();
            DateTime anchor = set.Contains(today) ? today : set.Contains(today.AddDays(-1)) ? today.AddDays(-1) : DateTime.MinValue;
            int current = 0;
            if (anchor != DateTime.MinValue)
            {
                for (DateTime d = anchor; set.Contains(d); d = d.AddDays(-1)) current++;
            }
            summary.CurrentDailyStreak = current;

            int longest = 0, run = 0;
            DateTime? prev = null;
            foreach (var d in days)
            {
                run = prev.HasValue && d == prev.Value.AddDays(1) ? run + 1 : 1;
                longest = Math.Max(longest, run);
                prev = d;
            }
            summary.LongestDailyStreak = Math.Max(longest, current);

            var busiest = byDay.OrderByDescending(k => k.Value.Pages).ThenByDescending(k => k.Value.Seconds).First();
            summary.BusiestDayLabel = busiest.Key.ToString("ddd, MMM d yyyy", CultureInfo.CurrentCulture);
            summary.BusiestDayPages = busiest.Value.Pages;
        }

        // ── Habits ──
        if (valid.Count > 0)
        {
            summary.AverageSessionMinutes = valid.Average(s => s.DurationSeconds) / 60.0;
            summary.PagesPerHour = summary.TotalDurationSeconds >= 60
                ? summary.TotalPagesRead / (summary.TotalDurationSeconds / 3600.0)
                : 0;

            var weekday = valid
                .GroupBy(s => LocalOf(s.StartUtc).DayOfWeek)
                .OrderByDescending(g => g.Sum(s => s.DurationSeconds))
                .First().Key;
            summary.FavoriteWeekday = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(weekday);

            var hourly = new int[24];
            foreach (var s in valid)
            {
                hourly[LocalOf(s.StartUtc).Hour] += Math.Max(1, s.DurationSeconds);
            }
            int maxHour = hourly.Max();
            for (int h = 0; h < 24; h++)
            {
                summary.HourlyActivity.Add(new HourlyReadingActivity
                {
                    Hour = h,
                    Seconds = hourly[h],
                    Intensity = maxHour > 0 ? (double)hourly[h] / maxHour : 0
                });
            }

            int peak = Array.IndexOf(hourly, maxHour);
            summary.FavoriteTimeOfDay = peak switch
            {
                >= 5 and < 12 => "Morning reader",
                >= 12 and < 17 => "Afternoon reader",
                >= 17 and < 22 => "Evening reader",
                _ => "Night owl"
            };
        }

        // ── Series (grouped with the same parser as the Series view) ──
        var seriesStats = new Dictionary<string, SeriesAccumulator>(StringComparer.Ordinal);
        var identityCache = new Dictionary<long, ComicIdentity>();

        ComicIdentity IdentityOf(ComicEntity comic)
        {
            if (!identityCache.TryGetValue(comic.Id, out var identity))
            {
                ComicMetadataEntity? meta = null;
                metadata?.TryGetValue(comic.Id, out meta);
                identity = ComicIdentityParser.Parse(comic, meta);
                identityCache[comic.Id] = identity;
            }
            return identity;
        }

        // Library size of each parsed series, for "opened 3 of 12" when no Series-screen grouping is supplied.
        var keyCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        if (membership == null)
        {
            foreach (var c in comics)
            {
                var id = IdentityOf(c);
                string k = id.SeriesKey.Length > 0 ? id.SeriesKey : ComicIdentityParser.MakeKey(c.Title);
                keyCounts[k] = keyCounts.TryGetValue(k, out var n) ? n + 1 : 1;
            }
        }

        foreach (var session in valid)
        {
            if (!comicById.TryGetValue(session.ComicId, out var comic)) continue;
            string key;
            string name;
            int total;
            if (membership != null && membership.TryGetValue(comic.Id, out var member))
            {
                key = "series:" + member.Key;
                name = member.Name;
                total = member.TotalIssues;
            }
            else if (membership != null)
            {
                key = "book:" + comic.Id.ToString(CultureInfo.InvariantCulture);
                name = comic.Title;
                total = 1;
            }
            else
            {
                var identity = IdentityOf(comic);
                key = identity.SeriesKey.Length > 0 ? identity.SeriesKey : ComicIdentityParser.MakeKey(comic.Title);
                name = string.IsNullOrWhiteSpace(identity.SeriesName) ? comic.Title : identity.SeriesName;
                total = keyCounts.TryGetValue(key, out var n) ? n : 1;
            }

            if (!seriesStats.TryGetValue(key, out var acc))
            {
                acc = new SeriesAccumulator(name) { TotalIssues = Math.Max(1, total) };
                seriesStats[key] = acc;
            }
            if (session.StartUtc > acc.LastReadUtc) acc.LastReadUtc = session.StartUtc;

            acc.Pages += session.PagesRead;
            acc.Seconds += session.DurationSeconds;
            acc.ComicIds.Add(comic.Id);
            if (LocalOf(session.StartUtc).Year == today.Year) acc.YearSeconds += session.DurationSeconds;
            acc.Cover ??= comic.ThumbnailPath;
        }

        // Every comic of each ranked series (not only the ones with sessions), for page-accurate progress.
        var membersByKey = new Dictionary<string, List<ComicEntity>>(StringComparer.Ordinal);
        foreach (var c in comics)
        {
            string key;
            if (membership != null)
            {
                key = membership.TryGetValue(c.Id, out var m) ? "series:" + m.Key : "book:" + c.Id.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                var id = IdentityOf(c);
                key = id.SeriesKey.Length > 0 ? id.SeriesKey : ComicIdentityParser.MakeKey(c.Title);
            }
            if (!membersByKey.TryGetValue(key, out var list)) membersByKey[key] = list = new List<ComicEntity>();
            list.Add(c);
        }

        static int PagesReadOf(ComicEntity c) =>
            c.PageCount <= 0 ? 0 : c.IsCompleted ? c.PageCount : c.LastReadPage > 0 ? Math.Min(c.PageCount, c.LastReadPage + 1) : 0;

        foreach (var (key, acc) in seriesStats)
        {
            if (!membersByKey.TryGetValue(key, out var members)) continue;
            acc.SeriesPagesRead = members.Sum(PagesReadOf);
            acc.SeriesTotalPages = members.Sum(m => Math.Max(0, m.PageCount));
        }

        var ranked = seriesStats.Values
            .OrderByDescending(s => s.Pages)
            .ThenByDescending(s => s.Seconds)
            .Take(20)
            .ToList();

        int topPages = ranked.Count > 0 ? Math.Max(1, ranked[0].Pages) : 1;
        for (int i = 0; i < ranked.Count; i++)
        {
            var s = ranked[i];
            summary.TopSeries.Add(new SeriesReadingStat
            {
                Rank = i + 1,
                SeriesName = s.Name,
                PagesRead = s.Pages,
                DurationSeconds = s.Seconds,
                DurationMinutes = (int)Math.Round(s.Seconds / 60.0),
                IssuesRead = s.ComicIds.Count,
                IssuesCompleted = s.ComicIds.Count(id => comicById.TryGetValue(id, out var c) && c.IsCompleted),
                Share = (double)s.Pages / topPages,
                CoverThumbnailPath = s.Cover,
                TotalIssues = Math.Max(s.TotalIssues, s.ComicIds.Count),
                IsSeries = Math.Max(s.TotalIssues, s.ComicIds.Count) > 1,
                LastReadUtc = s.LastReadUtc,
                SeriesPagesRead = s.SeriesPagesRead,
                SeriesTotalPages = s.SeriesTotalPages
            });
        }

        if (summary.TopSeries.Count > 0)
        {
            summary.TopSeriesName = summary.TopSeries[0].SeriesName;
            summary.TopSeriesPages = summary.TopSeries[0].PagesRead;
        }

        var yearTop = seriesStats.Values.Where(s => s.YearSeconds > 0).OrderByDescending(s => s.YearSeconds).FirstOrDefault();
        if (yearTop != null) summary.YearTopSeriesName = yearTop.Name;

        // ── Recently read ──
        summary.RecentlyRead = comics
            .Where(c => c.LastReadAt.HasValue && (c.LastReadPage > 0 || c.IsCompleted))
            .OrderByDescending(c => c.LastReadAt)
            .Take(6)
            .Select(c => new RecentlyReadComic
            {
                ComicId = c.Id,
                Title = c.Title,
                ThumbnailPath = c.ThumbnailPath,
                LastReadPage = c.LastReadPage,
                PageCount = c.PageCount,
                IsCompleted = c.IsCompleted,
                LastReadAtUtc = ToUtc(c.LastReadAt!.Value)
            })
            .ToList();

        return summary;
    }

    /// <summary>Repository timestamps are parsed as local time; sessions are stored as UTC. Normalize both.</summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value
    };

    private sealed class SeriesAccumulator
    {
        public SeriesAccumulator(string name) => Name = name;
        public string Name { get; }
        public int Pages { get; set; }
        public int Seconds { get; set; }
        public int YearSeconds { get; set; }
        public HashSet<long> ComicIds { get; } = new();
        public string? Cover { get; set; }
        public int TotalIssues { get; set; } = 1;
        public DateTime LastReadUtc { get; set; } = DateTime.MinValue;
        public int SeriesPagesRead { get; set; }
        public int SeriesTotalPages { get; set; }
    }
}
