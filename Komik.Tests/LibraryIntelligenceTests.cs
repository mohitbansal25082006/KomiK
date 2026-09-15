using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Komik.Helpers;
using Komik.Models;
using Komik.Services;

namespace Komik.Tests;

/// <summary>
/// Series detection, duplicate detection and live reading statistics (v1.1.0 library intelligence).
/// </summary>
public static class LibraryIntelligenceTests
{
    public static async Task RunAllAsync(Func<string, Func<Task>, Task> runTest, string tempDir)
    {
        await runTest("ComicIdentityParser Parses Series, Volumes, Issues, Chapters and Years", () =>
        {
            void Expect(string name, string series, double? volume, double? issue, ComicNumberKind kind, int? year = null)
            {
                var id = ComicIdentityParser.Parse(name);
                if (!string.Equals(id.SeriesName, series, StringComparison.Ordinal) || id.Volume != volume || id.Issue != issue || id.Kind != kind || id.Year != year)
                {
                    throw new Exception($"'{name}' → series='{id.SeriesName}' vol={id.Volume} issue={id.Issue} kind={id.Kind} year={id.Year}; expected '{series}' {volume} {issue} {kind} {year}");
                }
            }

            Expect("Batman (2016) #05.cbz", "Batman", null, 5, ComicNumberKind.Issue, 2016);
            Expect("Spider-Man Vol. 2 Issue 12 (Digital-Empire).cbr", "Spider-Man", 2, 12, ComicNumberKind.Issue);
            Expect("One Piece Chapter 1000.pdf", "One Piece", null, 1000, ComicNumberKind.Chapter);
            Expect("Invincible 042.cbz", "Invincible", null, 42, ComicNumberKind.Issue);
            Expect("Saga v03 017 (2014) (Digital) (Zone-Empire).cbz", "Saga", 3, 17, ComicNumberKind.Issue, 2014);
            Expect("Berserk Vol. 41.cbz", "Berserk", 41, null, ComicNumberKind.Volume);
            Expect("Chainsaw_Man_v01_c003.cbz", "Chainsaw Man", 1, 3, ComicNumberKind.Chapter);
            Expect("[Group] Kaguya-sama - Chapter 150.5.cbz", "Kaguya-sama", null, 150.5, ComicNumberKind.Chapter);
            Expect("Batman Annual 2 (2017).cbz", "Batman", null, 2, ComicNumberKind.Annual, 2017);
            Expect("Mr. Robot 5.cbz", "Mr. Robot", null, 5, ComicNumberKind.Issue);
            Expect("2000 AD 1234.cbz", "2000 AD", null, 1234, ComicNumberKind.Issue);
            Expect("The Walking Dead 100 - Something to Fear.cbz", "The Walking Dead", null, 100, ComicNumberKind.Issue);
            Expect("Watchmen.cbz", "Watchmen", null, null, ComicNumberKind.None);

            // Metadata beats the file name.
            var meta = new ComicMetadataEntity { SeriesName = "Amazing Spider-Man", IssueNumber = "300" };
            var fromMeta = ComicIdentityParser.Parse("asm_scan_final.cbz", null, meta);
            if (fromMeta.SeriesName != "Amazing Spider-Man" || fromMeta.Issue != 300 || !fromMeta.FromMetadata)
                throw new Exception("Metadata series/issue was not applied");

            if (ComicIdentityParser.MakeKey("The Spider-Man & Friends") != "spidermanandfriends")
                throw new Exception($"Key normalization wrong: {ComicIdentityParser.MakeKey("The Spider-Man & Friends")}");
            if (ComicIdentityParser.MakeKey("Pokémon") != ComicIdentityParser.MakeKey("Pokemon"))
                throw new Exception("Accents should not change the key");
            return Task.CompletedTask;
        });

        await runTest("SeriesDetectionService Groups, Orders, Merges Typos and Finds Gaps", () =>
        {
            long id = 1;
            ComicEntity C(string file, bool completed = false, int lastPage = 0, int pages = 24) => new()
            {
                Id = id++,
                Title = Path.GetFileNameWithoutExtension(file),
                FilePath = Path.Combine(@"C:\Comics", file),
                Format = ComicSourceType.ZipArchive,
                PageCount = pages,
                IsCompleted = completed,
                LastReadPage = lastPage,
                LastReadAt = completed || lastPage > 0 ? DateTime.UtcNow.AddMinutes(-id) : null
            };

            var comics = new List<ComicEntity>
            {
                C("Saga 003.cbz"), C("Saga 001.cbz", completed: true), C("Saga #2 (Digital).cbz", completed: true), C("Saga 005.cbz"),
                C("Invincible 10.cbz"), C("Invincibel 11.cbz"),
                C("Batman 001.cbz"), C("Superman 001.cbz"),
                C("Batman 2099 001.cbz"), C("Batman 1989 002.cbz"),
                C(@"Sandman Omnibus\Preludes and Nocturnes.cbz"), C(@"Sandman Omnibus\The Doll's House.cbz"),
                C("Watchmen.cbz"), C("Maus.cbz"),
                C("Berserk v02.cbz"), C("Berserk v01.cbz", lastPage: 100, pages: 200)
            };

            var service = new SeriesDetectionService();
            var groups = service.DetectSeries(comics);
            ComicSeriesGroup? Find(string name) => groups.FirstOrDefault(g => g.SeriesName.Equals(name, StringComparison.OrdinalIgnoreCase));

            var saga = Find("Saga") ?? throw new Exception("Saga series missing: " + string.Join(" | ", groups.Select(g => g.SeriesName)));
            if (saga.Issues.Count != 4) throw new Exception($"Saga should have 4 issues, has {saga.Issues.Count}");
            string order = string.Join(",", saga.Issues.Select(i => ComicIdentityParser.Parse(i).Issue));
            if (order != "1,2,3,5") throw new Exception($"Saga order wrong: {order}");
            if (!saga.HasMissingIssues || saga.MissingIssues.Count != 1 || saga.MissingIssues[0] != "#4") throw new Exception($"Saga gap wrong: {saga.MissingIssuesDisplay}");
            if (saga.NextIssueToRead?.Title != "Saga 003") throw new Exception($"Saga next issue wrong: {saga.NextIssueToRead?.Title}");

            var invincible = groups.FirstOrDefault(g => g.Issues.Any(i => i.Title.StartsWith("Invincibel")));
            if (invincible == null || invincible.Issues.Count != 2) throw new Exception("Typo 'Invincibel' was not merged with 'Invincible'");

            if (Find("Batman") != null) throw new Exception("A single Batman #1 must not form a series");
            if (groups.Any(g => g.Issues.Any(i => i.Title.Contains("2099")) && g.Issues.Any(i => i.Title.Contains("1989"))))
                throw new Exception("Batman 2099 and Batman 1989 must stay apart");

            var sandman = Find("Sandman Omnibus") ?? throw new Exception("Folder series 'Sandman Omnibus' missing");
            if (sandman.Source != SeriesSource.Folder || sandman.Issues.Count != 2) throw new Exception("Folder series built incorrectly");
            if (groups.Any(g => g.Issues.Any(i => i.Title == "Watchmen"))) throw new Exception("Standalone books in a generic folder must not become a series");

            var berserk = Find("Berserk") ?? throw new Exception("Berserk volumes missing");
            if (berserk.IssueCountDisplay != "2 Volumes") throw new Exception($"Volume wording wrong: {berserk.IssueCountDisplay}");
            if (Math.Abs(berserk.OverallProgress - (101.0 / 224.0)) > 0.001) throw new Exception($"Page-weighted progress wrong: {berserk.OverallProgress}");

            // Hidden series and manual series
            var hidden = service.DetectSeries(comics, null, null, new SeriesDetectionOptions { IgnoredSeriesKeys = new HashSet<string> { "saga" } });
            if (hidden.Any(g => g.SeriesKey == "saga")) throw new Exception("Ignored series key was still grouped");

            var manual = new ComicSeriesGroup { SeriesName = "My Saga Picks", IsManual = true, ManualSeriesId = 9 };
            manual.Issues.Add(comics[0]);
            manual.Issues.Add(comics[3]);
            var withManual = service.DetectSeries(comics, null, new[] { manual });
            if (!withManual.Any(g => g.IsManual && g.Issues.Count == 2)) throw new Exception("Manual series missing");
            var autoSaga = withManual.FirstOrDefault(g => !g.IsManual && g.SeriesKey == "saga");
            if (autoSaga == null || autoSaga.Issues.Count != 2) throw new Exception("Manual series should claim its issues from auto detection");
            return Task.CompletedTask;
        });

        await runTest("DuplicateDetectionService Scores Copies, Avoids False Positives, Recommends Best Copy", () =>
        {
            string dir = Path.Combine(tempDir, "dupes");
            Directory.CreateDirectory(dir);
            byte[] payload = Enumerable.Range(0, 300_000).Select(i => (byte)(i * 31 % 251)).ToArray();
            string a = Path.Combine(dir, "Hellboy Seed of Destruction.cbz");
            string b = Path.Combine(dir, "hb_seed_backup.cbz");
            File.WriteAllBytes(a, payload);
            File.WriteAllBytes(b, payload);

            var comics = new List<ComicEntity>
            {
                new() { Id = 1, Title = "The Amazing Spider-Man 101 (2020)", FilePath = @"C:\Comics\The Amazing Spider-Man 101 (2020).cbz", PageCount = 22, FileSize = 5000 },
                new() { Id = 2, Title = "The Amazing Spider-Man 102 (2020)", FilePath = @"C:\Comics\The Amazing Spider-Man 102 (2020).cbz", PageCount = 22, FileSize = 5001 },
                new() { Id = 3, Title = "Hellboy Seed of Destruction", FilePath = a, PageCount = 120, FileSize = payload.Length },
                new() { Id = 4, Title = "hb_seed_backup", FilePath = b, PageCount = 120, FileSize = payload.Length, LastReadPage = 50 },
                new() { Id = 5, Title = "Saga #7 (Digital)", FilePath = @"C:\Comics\Saga #7 (Digital).cbr", Format = ComicSourceType.RarArchive, PageCount = 30, FileSize = 9000 },
                new() { Id = 6, Title = "Saga 007", FilePath = @"D:\Backup\Saga 007.cbz", PageCount = 30, FileSize = 8000, IsFavorite = true },
                new() { Id = 7, Title = "Preludes", FilePath = @"C:\Comics\Sandman\Preludes.cbz", PageCount = 40, FileSize = 1 },
                new() { Id = 8, Title = "Nocturnes", FilePath = @"C:\Comics\Sandman\Nocturnes.cbz", PageCount = 44, FileSize = 2 },
                new() { Id = 9, Title = "X-Men 1 (1991)", FilePath = @"C:\Comics\X-Men 1 (1991).cbz", PageCount = 40, FileSize = 3 },
                new() { Id = 10, Title = "X-Men 1 (2019)", FilePath = @"C:\Comics\X-Men 1 (2019).cbz", PageCount = 40, FileSize = 4 },
            };

            var groups = new DuplicateDetectionService().FindDuplicates(comics, new HashSet<(long, long)>());
            bool Grouped(long x, long y) => groups.Any(g => g.Copies.Any(c => c.Id == x) && g.Copies.Any(c => c.Id == y));

            if (Grouped(1, 2)) throw new Exception("Issue #101 and #102 must never be duplicates");
            if (Grouped(7, 8)) throw new Exception("Different un-numbered books in the same folder must not be duplicates");
            if (Grouped(9, 10)) throw new Exception("Same issue number from different years (volumes) must not be duplicates");

            var identical = groups.FirstOrDefault(g => g.Copies.Any(c => c.Id == 3)) ?? throw new Exception("Identical files not detected");
            if (identical.Confidence != DuplicateConfidence.Identical) throw new Exception($"Identical files scored {identical.Confidence}");
            if (identical.RecommendedKeep?.Id != 4) throw new Exception("The copy with reading progress should be recommended");

            var saga = groups.FirstOrDefault(g => g.Copies.Any(c => c.Id == 5)) ?? throw new Exception("Saga #7 cross-format copy not detected");
            if (saga.Confidence != DuplicateConfidence.SameIssue || !Grouped(5, 6)) throw new Exception("Saga #7 should be a same-issue duplicate");
            if (saga.RecommendedKeep?.Id != 6) throw new Exception("Favorite CBZ copy should be recommended over the CBR");
            if (saga.ReclaimableBytes != 9000) throw new Exception($"Reclaimable size wrong: {saga.ReclaimableBytes}");

            var ignored = new DuplicateDetectionService().FindDuplicates(comics, new HashSet<(long, long)> { (5, 6) });
            if (ignored.Any(g => g.Copies.Any(c => c.Id == 5) && g.Copies.Any(c => c.Id == 6))) throw new Exception("Ignored pair was flagged");

            if (DuplicateDetectionService.TryFingerprint(a, payload.Length) != DuplicateDetectionService.TryFingerprint(b, payload.Length))
                throw new Exception("Fingerprints of identical files differ");
            if (DuplicateDetectionService.TryFingerprint(Path.Combine(dir, "missing.cbz"), 10) != null)
                throw new Exception("Missing file must not produce a fingerprint");
            return Task.CompletedTask;
        });

        await runTest("ReadingSessionTracker Counts Active Time with Idle Cap and Distinct Pages", () =>
        {
            var now = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
            var tracker = new ReadingSessionTracker(() => now);
            tracker.Start(42, initialPage: 0);
            if (tracker.PagesRead != 0) throw new Exception("The opening page should not count instantly");

            now = now.AddSeconds(40); tracker.RecordPageView(1);
            now = now.AddSeconds(40); tracker.RecordPageView(2);
            now = now.AddSeconds(30); tracker.RecordPageView(1); // going back does not double count
            if (tracker.PagesRead != 3) throw new Exception($"Expected 3 distinct pages, got {tracker.PagesRead}");
            if (tracker.ActiveSeconds != 110) throw new Exception($"Expected 110 active seconds, got {tracker.ActiveSeconds}");

            now = now.AddHours(8); // left open overnight
            if (tracker.ActiveSeconds != 110 + (int)ReadingSessionTracker.IdleCap.TotalSeconds) throw new Exception($"Idle gap not capped: {tracker.ActiveSeconds}");
            tracker.RecordPageView(3);
            if (tracker.ActiveSeconds != 110 + (int)ReadingSessionTracker.IdleCap.TotalSeconds) throw new Exception("Idle cap applied twice or not at all");

            var dwell = new ReadingSessionTracker(() => now);
            dwell.Start(1, 5);
            now = now.AddSeconds(20);
            if (dwell.PagesRead != 1) throw new Exception("Lingering on the opening page should count it");
            return Task.CompletedTask;
        });

        await runTest("ReadingStatsCalculator Uses Local Days, Streaks, Periods and Real Series Totals", () =>
        {
            // UTC+05:30 so a UTC-date bug would put late-evening sessions on the wrong day.
            var zone = TimeZoneInfo.CreateCustomTimeZone("IST-test", TimeSpan.FromMinutes(330), "IST", "IST");
            var nowUtc = new DateTime(2026, 9, 14, 20, 0, 0, DateTimeKind.Utc); // 01:30 on Sep 15 local

            var comics = new List<ComicEntity>
            {
                new() { Id = 1, Title = "Saga 001", FilePath = @"C:\C\Saga 001.cbz", PageCount = 30, IsCompleted = true, LastReadAt = nowUtc.AddHours(-2) },
                new() { Id = 2, Title = "Saga 002", FilePath = @"C:\C\Saga 002.cbz", PageCount = 30, LastReadPage = 10, LastReadAt = nowUtc.AddMinutes(-5) },
                new() { Id = 3, Title = "Monstress 001", FilePath = @"C:\C\Monstress 001.cbz", PageCount = 60 },
            };

            var sessions = new List<ReadingSessionRecord>
            {
                new(1, new DateTime(2026, 9, 14, 19, 0, 0, DateTimeKind.Utc), 1200, 30), // 00:30 Sep 15 local (today)
                new(2, new DateTime(2026, 9, 14, 19, 50, 0, DateTimeKind.Utc), 600, 11), // 01:20 Sep 15 local (today)
                new(3, new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc), 1800, 20), // 15:30 Sep 14 local
                new(3, new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc), 300, 5),   // Sep 12 (breaks the streak)
                new(3, new DateTime(2026, 9, 12, 11, 0, 0, DateTimeKind.Utc), 0, 0),     // empty rows are ignored
            };

            var stats = ReadingStatsCalculator.Calculate(sessions, comics, null, nowUtc, zone);
            if (stats.TotalSessionsCount != 4) throw new Exception($"Sessions: {stats.TotalSessionsCount}");
            if (stats.TotalPagesRead != 66 || stats.TotalDurationSeconds != 3900) throw new Exception($"Totals: {stats.TotalPagesRead} pages / {stats.TotalDurationSeconds}s");
            if (stats.TodayPages != 41 || stats.TodaySeconds != 1800) throw new Exception($"Today should be local: {stats.TodayPages} pages / {stats.TodaySeconds}s");
            if (stats.CurrentDailyStreak != 2) throw new Exception($"Current streak: {stats.CurrentDailyStreak}");
            if (stats.LongestDailyStreak != 2) throw new Exception($"Longest streak: {stats.LongestDailyStreak}");
            if (stats.ActiveDays != 3) throw new Exception($"Active days: {stats.ActiveDays}");
            if (stats.Last30Days.Count != 30 || !stats.Last30Days[^1].IsToday || stats.Last30Days[^1].PagesRead != 41) throw new Exception("30-day chart wrong");
            if (stats.TopSeries.Count != 2 || stats.TopSeries[0].SeriesName != "Saga" || stats.TopSeries[0].PagesRead != 41 || stats.TopSeries[0].IssuesRead != 2)
                throw new Exception("Top series should combine Saga #1 and #2: " + string.Join(" | ", stats.TopSeries.Select(t => $"{t.SeriesName}:{t.PagesRead}")));
            if (stats.ComicsCompleted != 1 || stats.ComicsInProgress != 1 || stats.ComicsUnread != 1) throw new Exception("Library breakdown wrong");
            if (stats.RecentlyRead.FirstOrDefault()?.ComicId != 2) throw new Exception("Most recently read comic wrong");
            if (Math.Abs(stats.PagesPerHour - 66 / (3900 / 3600.0)) > 0.01) throw new Exception($"Reading speed wrong: {stats.PagesPerHour}");

            var empty = ReadingStatsCalculator.Calculate(new List<ReadingSessionRecord>(), comics, null, nowUtc, zone);
            if (empty.HasReadingData || empty.TotalDurationSeconds != 0 || empty.TotalPagesRead != 0) throw new Exception("No sessions must mean no invented reading time");
            return Task.CompletedTask;
        });

        await runTest("LibraryRepository Upserts Live Reading Sessions and Loads Metadata in Bulk", async () =>
        {
            string dbPath = Path.Combine(tempDir, "live_sessions.db");
            using var repo = new LibraryRepository(dbPath);
            await repo.InitializeAsync();
            long comicId = await repo.InsertComicAsync(new ComicEntity { Title = "Saga 001", FilePath = @"C:\C\Saga 001.cbz", Format = ComicSourceType.ZipArchive, PageCount = 30 });

            var start = DateTime.UtcNow.AddMinutes(-3);
            long sessionId = await repo.SaveReadingSessionAsync(null, comicId, start, DateTime.UtcNow, 45, 3);
            if (sessionId <= 0) throw new Exception("Insert did not return an id");
            long sameId = await repo.SaveReadingSessionAsync(sessionId, comicId, start, DateTime.UtcNow, 150, 9);
            if (sameId != sessionId) throw new Exception("Checkpoint should update the same session row");

            var stats = await repo.GetReadingStatsSummaryAsync();
            if (stats.TotalSessionsCount != 1 || stats.TotalPagesRead != 9 || stats.TotalDurationSeconds != 150)
                throw new Exception($"Upserted session not reflected: {stats.TotalSessionsCount} sessions, {stats.TotalPagesRead} pages, {stats.TotalDurationSeconds}s");
            if (stats.TodayPages != 9) throw new Exception("Live session should count toward today");

            await repo.SaveComicMetadataAsync(new ComicMetadataEntity { ComicId = comicId, SeriesName = "Saga", IssueNumber = "1" });
            var all = await repo.GetAllComicMetadataAsync();
            if (!all.TryGetValue(comicId, out var meta) || meta.SeriesName != "Saga") throw new Exception("Bulk metadata load failed");
        });
        await runTest("SeriesDetectionService Finds Continuations and Creator Collections Across the Whole Library", () =>
        {
            long id = 100;
            ComicEntity C(string folder, string title) => new()
            {
                Id = id++,
                Title = title,
                FilePath = Path.Combine(folder, title + ".cbz"),
                Format = ComicSourceType.ZipArchive,
                PageCount = 20
            };

            string root = @"D:\Library";
            var comics = new List<ComicEntity>
            {
                // Same creator, un-numbered first book + numbered sequel, download ids in front, in different folders.
                C(root, "site-263621 - [Hoshino Ren] Hypno Lesson... [English]"),
                C(Path.Combine(root, "new"), "site-298921 - [Hoshino Ren] Hypno Lesson... 2 [English] [Some Group]"),
                // Circle (artist) credit, story-arc words.
                C(root, "[Moon Circle (Hoshino Ren)] Chairman Zenpen [English]"),
                C(root, "[Moon Circle (Hoshino Ren)] Chairman Kouhen [English]"),
                // Same creator, unrelated standalone work.
                C(root, "[Hoshino Ren] Summer Rain [English]"),
                // Same title by a different creator must not join the continuation.
                C(root, "[Other Artist] Hypno Lesson 3 [English]"),
                C(root, "[Other Artist] Night Bus [English]"),
                // A scanlation group on chapters of one work is not a creator collection.
                C(root, "[Scan Team] Tower Climber c01"),
                C(root, "[Scan Team] Tower Climber c02"),
                // Standalone books directly in the watched root: no folder series.
                C(root, "Lonely Book"),
                C(root, "Another Lonely Book"),
            };

            var service = new SeriesDetectionService();
            var result = service.DetectAll(comics, null, null, new SeriesDetectionOptions { RootFolders = new[] { root + "\\" } });
            string Dump(IEnumerable<ComicSeriesGroup> g) => string.Join(" | ", g.Select(x => $"{x.SeriesName}[{string.Join(",", x.Issues.Select(i => i.Id))}]"));

            var hypno = result.Series.FirstOrDefault(g => g.Issues.Any(i => i.Id == 100)) ?? throw new Exception("Continuation missing: " + Dump(result.Series));
            if (hypno.Issues.Count != 2 || hypno.Issues[0].Id != 100 || hypno.Issues[1].Id != 101)
                throw new Exception("Un-numbered first book + '2' should form an ordered series: " + Dump(result.Series));
            if (hypno.HasMissingIssues) throw new Exception("An un-numbered first book counts as #1: " + hypno.MissingIssuesDisplay);
            if (hypno.Issues.Any(i => i.Id == 105)) throw new Exception("Same title by a different creator joined the series");
            if (!hypno.CreatorsDisplay.Contains("Hoshino Ren")) throw new Exception("Series should credit its creator: " + hypno.CreatorsDisplay);

            var chairman = result.Series.FirstOrDefault(g => g.Issues.Any(i => i.Id == 102)) ?? throw new Exception("Zenpen/Kouhen missing: " + Dump(result.Series));
            if (chairman.Issues.Count != 2 || chairman.Issues[0].Id != 102) throw new Exception("Zenpen should come before Kouhen");

            if (result.Series.Any(g => g.Issues.Any(i => i.Id == 109))) throw new Exception("Books in the watched root must not form a folder series: " + Dump(result.Series));

            var creator = result.Creators.FirstOrDefault(g => g.Issues.Any(i => i.Id == 104)) ?? throw new Exception("Creator collection missing: " + Dump(result.Creators));
            if (creator.Source != SeriesSource.Creator || !creator.SeriesName.Contains("Hoshino Ren") || creator.Issues.Count != 5 || creator.WorkCount != 3)
                throw new Exception($"Creator collection wrong: {creator.SeriesName} {creator.Issues.Count} comics {creator.WorkCount} works");
            if (result.Creators.Any(g => g.SeriesName.Contains("Scan Team"))) throw new Exception("One work by one group is not a creator collection");
            if (!result.Creators.Any(g => g.SeriesName.Contains("Other Artist"))) throw new Exception("Two works by the other artist should form a creator collection: " + Dump(result.Creators));
            if (result.Creators.Count(g => g.Issues.Any(i => i.Id == 102)) != 1) throw new Exception("Circle and artist with the same books should be one card: " + Dump(result.Creators));

            var hidden = service.DetectAll(comics, null, null, new SeriesDetectionOptions { RootFolders = new[] { root }, IgnoredSeriesKeys = new HashSet<string> { creator.SeriesKey } });
            if (hidden.Creators.Any(g => g.SeriesKey == creator.SeriesKey)) throw new Exception("Hidden creator still listed");

            var id1 = ComicIdentityParser.Parse("site-263621 - [Moon Circle (Hoshino Ren)] Hypno Lesson... 2 [English]");
            if (id1.SeriesName != "Hypno Lesson" || id1.Issue != 2 || id1.Creators.Count != 2 || id1.Creators[0] != "Hoshino Ren")
                throw new Exception($"Parser: '{id1.SeriesName}' #{id1.Issue} creators={string.Join(",", id1.Creators)}");
            return Task.CompletedTask;
        });

        await runTest("LibraryRepository Gives Manual Series With Duplicate Names Unique Names", async () =>
        {
            string dbPath = Path.Combine(tempDir, "manual_names.db");
            using var repo = new LibraryRepository(dbPath);
            await repo.InitializeAsync();
            long a = await repo.InsertComicAsync(new ComicEntity { Title = "One", FilePath = @"C:\C\One.cbz", Format = ComicSourceType.ZipArchive, PageCount = 5 });
            long b = await repo.InsertComicAsync(new ComicEntity { Title = "Two", FilePath = @"C:\C\Two.cbz", Format = ComicSourceType.ZipArchive, PageCount = 5 });
            await repo.CreateManualSeriesAsync("Same Name", new[] { a });
            await repo.CreateManualSeriesAsync("Same Name", new[] { b });
            var names = (await repo.GetManualSeriesAsync()).Select(m => m.SeriesName).OrderBy(n => n).ToList();
            if (names.Count != 2 || names[0] != "Same Name" || names[1] != "Same Name (2)") throw new Exception("Names: " + string.Join(", ", names));
        });
        await runTest("Manual Series Support Creator Section, Auto-update, Exclusions and Going Back to Automatic", async () =>
        {
            long id = 500;
            ComicEntity C(string title) => new() { Id = id++, Title = title, FilePath = Path.Combine(@"D:\Lib\sub", title + ".cbz"), Format = ComicSourceType.ZipArchive, PageCount = 10 };
            var comics = new List<ComicEntity>
            {
                C("[Mina Sato] Lantern Road [English]"), C("[Mina Sato] Lantern Road 2 [English]"), C("[Mina Sato] Glass Harbor [English]"),
                C("Orbit 01"), C("Orbit 02"), C("Orbit 03")
            };
            var service = new SeriesDetectionService();
            var auto = service.DetectAll(comics);
            var autoCreator = auto.Creators.Single(g => g.SeriesName.Contains("Mina Sato"));
            var autoOrbit = auto.Series.Single(g => g.Issues.Any(i => i.Title == "Orbit 01"));

            // A manual creator collection replaces the automatic card and does not steal comics from story series.
            var manualCreator = new ComicSeriesGroup { SeriesName = "Mina", IsManual = true, ManualSeriesId = 1, Section = SeriesSection.Creator, SourceKey = autoCreator.SeriesKey };
            manualCreator.Issues.Add(comics[0]);
            var withManual = service.DetectAll(comics, null, new[] { manualCreator });
            if (withManual.Creators.Count(g => g.Issues.Any(i => i.Id == comics[0].Id)) != 1 || !withManual.Creators.Any(g => g.IsManual))
                throw new Exception("Manual creator should replace the automatic creator card");
            if (!withManual.Series.Any(g => !g.IsManual && g.Issues.Any(i => i.Id == comics[0].Id)))
                throw new Exception("A creator collection must not claim comics away from continuation series");
            if (!manualCreator.IsCreatorGroup || manualCreator.WorkCount != 1) throw new Exception($"Manual creator works: {manualCreator.WorkCount}");

            // Auto-update: pulls in the rest of the followed group, never excluded comics.
            manualCreator.IsAutoUpdate = true;
            manualCreator.ExcludedComicIds.Add(comics[2].Id);
            var additions = SeriesDetectionService.FindAutoUpdateAdditions(manualCreator, auto);
            if (additions.Count != 1 || additions[0].Id != comics[1].Id) throw new Exception("Auto-update additions wrong: " + string.Join(",", additions.Select(a => a.Title)));

            var manualOrbit = new ComicSeriesGroup { SeriesName = "My Orbit", IsManual = true, ManualSeriesId = 2, Section = SeriesSection.Story, IsAutoUpdate = true };
            manualOrbit.Issues.Add(comics[4]);
            var orbitAdds = SeriesDetectionService.FindAutoUpdateAdditions(manualOrbit, auto);
            if (orbitAdds.Count != 2) throw new Exception("Story auto-update should follow the overlapping series");
            manualOrbit.IsAutoUpdate = false;
            if (SeriesDetectionService.FindAutoUpdateAdditions(manualOrbit, auto).Count != 0) throw new Exception("Auto-update off must add nothing");

            var ordered = SeriesDetectionService.OrderForReading(new[] { comics[5], comics[3], comics[4] });
            if (string.Join(",", ordered.Select(o => o.Title)) != "Orbit 01,Orbit 02,Orbit 03") throw new Exception("Reading order wrong");

            // Repository round trip
            string dbPath = Path.Combine(tempDir, "manual_modes.db");
            using var repo = new LibraryRepository(dbPath);
            await repo.InitializeAsync();
            long a1 = await repo.InsertComicAsync(new ComicEntity { Title = "A1", FilePath = @"C:\M\A1.cbz", Format = ComicSourceType.ZipArchive, PageCount = 5 });
            long a2 = await repo.InsertComicAsync(new ComicEntity { Title = "A2", FilePath = @"C:\M\A2.cbz", Format = ComicSourceType.ZipArchive, PageCount = 5 });
            long sid = await repo.CreateManualSeriesAsync("Creator X", new[] { a1, a2 }, SeriesSection.Creator, autoUpdate: true, sourceKey: "creator:x");
            var loaded = (await repo.GetManualSeriesAsync()).Single();
            if (loaded.Section != SeriesSection.Creator || !loaded.IsAutoUpdate || loaded.SourceKey != "creator:x") throw new Exception("Manual options not persisted");
            await repo.RemoveComicFromManualSeriesAsync(sid, a2);
            loaded = (await repo.GetManualSeriesAsync()).Single();
            if (!loaded.ExcludedComicIds.Contains(a2) || loaded.Issues.Count != 1) throw new Exception("Removal should be remembered as an exclusion");
            await repo.AddComicsToManualSeriesAsync(sid, new[] { a2 });
            loaded = (await repo.GetManualSeriesAsync()).Single();
            if (loaded.ExcludedComicIds.Contains(a2) || loaded.Issues.Count != 2) throw new Exception("Adding back should clear the exclusion");
            await repo.SetManualSeriesOrderAsync(sid, new[] { a2, a1 });
            loaded = (await repo.GetManualSeriesAsync()).Single();
            if (loaded.Issues[0].Id != a2) throw new Exception("Manual order not saved");
            await repo.UpdateManualSeriesOptionsAsync(sid, false, SeriesSection.Story);
            loaded = (await repo.GetManualSeriesAsync()).Single();
            if (loaded.IsAutoUpdate || loaded.Section != SeriesSection.Story) throw new Exception("Options update failed");
            await repo.DeleteManualSeriesAsync(sid);
            if ((await repo.GetManualSeriesAsync()).Count != 0) throw new Exception("Make automatic (delete manual) failed");
        });

        await runTest("Removed Comics Are Remembered, Skipped by Rescans and Can Be Added Back", async () =>
        {
            string dir = Path.Combine(tempDir, "removed_lib");
            Directory.CreateDirectory(dir);
            string MakeCbz(string name)
            {
                string path = Path.Combine(dir, name);
                if (File.Exists(path)) File.Delete(path);
                using var zip = System.IO.Compression.ZipFile.Open(path, System.IO.Compression.ZipArchiveMode.Create);
                var entry = zip.CreateEntry("001.png");
                using var es = entry.Open();
                es.Write(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAMAASsJTYQAAAAASUVORK5CYII="));
                return path;
            }
            string keep = MakeCbz("Keep 01.cbz");
            string drop = MakeCbz("Drop 01.cbz");

            using var repo = new LibraryRepository(Path.Combine(tempDir, "removed.db"));
            await repo.InitializeAsync();
            var scanner = new LibraryScannerService(repo, thumbnailService: new ThumbnailService(Path.Combine(tempDir, "removed_thumbs")));
            await scanner.ScanFolderAsync(dir);
            if (await repo.GetTotalComicCountAsync() != 2) throw new Exception("Initial scan should index 2 comics");

            var dropped = await repo.GetComicByPathAsync(drop) ?? throw new Exception("comic missing");
            await repo.RemoveComicAsync(dropped.Id);
            if (!(await repo.GetRemovedComicPathsAsync()).Contains(drop)) throw new Exception("Removal not remembered");

            await scanner.RescanAllAsync();
            if (await repo.GetTotalComicCountAsync() != 1) throw new Exception("Rescan must not bring a removed comic back");

            var sources = await scanner.FindComicSourcesAsync(new[] { dir });
            if (sources.Count != 2) throw new Exception($"Discovery should find 2 comics, found {sources.Count}");
            var notInLibrary = sources.Where(s => s.Path != keep).ToList();
            int added = await scanner.IndexComicsAsync(notInLibrary);
            if (added != 1 || await repo.GetTotalComicCountAsync() != 2) throw new Exception("Adding back failed");
            if ((await repo.GetRemovedComicPathsAsync()).Contains(drop)) throw new Exception("Adding back should forget the removal");

            // Library-only removal is remembered too; deleting the file removes it for good.
            var again = await repo.GetComicByPathAsync(drop) ?? throw new Exception("comic missing");
            await repo.DeleteComicAsync(again.Id, deleteFileFromDisk: false);
            if (!(await repo.GetRemovedComicPathsAsync()).Contains(drop) || !File.Exists(drop)) throw new Exception("Library-only delete should keep the file and remember it");
            await scanner.IndexComicsAsync(new[] { (drop, ComicSourceType.ZipArchive) });
            var third = await repo.GetComicByPathAsync(drop) ?? throw new Exception("comic missing");
            await repo.DeleteComicAsync(third.Id, deleteFileFromDisk: true);
            if (File.Exists(drop)) throw new Exception("File was not deleted");
            if (await repo.GetComicByPathAsync(drop) != null) throw new Exception("Deleted comic still in library");
        });

        await runTest("Tags: Usage Counts, Rename, Merge and Delete", async () =>
        {
            using var repo = new LibraryRepository(Path.Combine(tempDir, "tags_v2.db"));
            await repo.InitializeAsync();
            long c1 = await repo.InsertComicAsync(new ComicEntity { Title = "T1", FilePath = @"C:\T\1.cbz", Format = ComicSourceType.ZipArchive, PageCount = 3 });
            long c2 = await repo.InsertComicAsync(new ComicEntity { Title = "T2", FilePath = @"C:\T\2.cbz", Format = ComicSourceType.ZipArchive, PageCount = 3 });
            await repo.AddTagToComicAsync(c1, "Manga");
            await repo.AddTagToComicAsync(c2, "Manga");
            await repo.AddTagToComicAsync(c2, "Shonen");
            await repo.CreateTagAsync("Unused");
            var usage = await repo.GetTagUsageAsync();
            if (usage["manga"] != 2 || usage["Shonen"] != 1 || usage["Unused"] != 0) throw new Exception("Usage counts wrong");

            await repo.RenameTagAsync("Shonen", "Shounen");
            usage = await repo.GetTagUsageAsync();
            if (usage.ContainsKey("Shonen") || usage["Shounen"] != 1) throw new Exception("Rename failed");

            await repo.RenameTagAsync("Shounen", "Manga");
            usage = await repo.GetTagUsageAsync();
            if (usage.ContainsKey("Shounen") || usage["Manga"] != 2) throw new Exception("Merge failed (a comic must not be counted twice)");

            await repo.DeleteTagAsync("Manga");
            if ((await repo.GetTagsForComicAsync(c1)).Count != 0) throw new Exception("Deleted tag still on comic");
        });

        await runTest("Manual Series Rename Keeps Names Unique", async () =>
        {
            using var repo = new LibraryRepository(Path.Combine(tempDir, "rename_series.db"));
            await repo.InitializeAsync();
            long c1 = await repo.InsertComicAsync(new ComicEntity { Title = "R1", FilePath = @"C:\R\1.cbz", Format = ComicSourceType.ZipArchive, PageCount = 3 });
            long a = await repo.CreateManualSeriesAsync("Alpha", new[] { c1 });
            long b = await repo.CreateManualSeriesAsync("Beta", new[] { c1 });
            string renamed = await repo.RenameManualSeriesAsync(b, "alpha");
            if (renamed != "alpha (2)") throw new Exception($"Clash should become 'alpha (2)', got '{renamed}'");
            if (await repo.RenameManualSeriesAsync(a, "Alpha Prime") != "Alpha Prime") throw new Exception("Plain rename failed");
        });

        await runTest("OCR Layout Groups Speech Bubbles in Reading Order and Tiles Tall Pages", () =>
        {
            var page = new OcrPageResult();
            page.Lines.Add(new OcrLineBox { Text = "HELLO THERE,", X = 40, Y = 50, Width = 200, Height = 20 });
            page.Lines.Add(new OcrLineBox { Text = "FRIEND!", X = 60, Y = 74, Width = 120, Height = 20 });
            page.Lines.Add(new OcrLineBox { Text = "WHO ARE YOU?", X = 600, Y = 60, Width = 180, Height = 20 });
            page.Lines.Add(new OcrLineBox { Text = "THE END", X = 300, Y = 700, Width = 150, Height = 22 });

            OcrLayout.BuildBlocks(page, rightToLeft: false, joinWithoutSpaces: false);
            if (page.Blocks.Count != 3) throw new Exception($"Expected 3 bubbles, got {page.Blocks.Count}");
            if (page.Blocks[0].Text != "HELLO THERE, FRIEND!" || page.Blocks[1].Text != "WHO ARE YOU?" || page.Blocks[2].Text != "THE END")
                throw new Exception("Western order wrong: " + string.Join(" | ", page.Blocks.Select(b => b.Text)));

            OcrLayout.BuildBlocks(page, rightToLeft: true, joinWithoutSpaces: false);
            if (page.Blocks[0].Text != "WHO ARE YOU?") throw new Exception("Manga order should start top-right");
            if (!page.ContainsText("hellothere")) throw new Exception("Search should ignore spacing");

            var tiles = OcrLayout.PlanTiles(10000, 3000, 300);
            if (tiles[0] != (0, 3000) || tiles[^1].Top + tiles[^1].Height != 10000) throw new Exception("Tiles must cover the whole strip");
            for (int i = 1; i < tiles.Count; i++)
            {
                if (tiles[i].Top >= tiles[i - 1].Top + tiles[i - 1].Height) throw new Exception("Tiles must overlap");
            }
            if (OcrLayout.PlanTiles(900, 3000, 300).Count != 1) throw new Exception("Short page should be one tile");
            foreach (var noise in new[] { "rrrwruur", "111011'", "~", "xkcdqrtw" })
            {
                if (!OcrLayout.LooksLikeNoise(noise)) throw new Exception($"'{noise}' should be treated as noise");
            }
            foreach (var real in new[] { "WHY ME?", "OK", "THE END... OF ISSUE #01", "NEON SPRAWL, 2089.", "WHOOSH!", "BZZT", "SHHHHH", "こんにちは" })
            {
                if (OcrLayout.LooksLikeNoise(real)) throw new Exception($"'{real}' is real text");
            }
            return Task.CompletedTask;
        });

        await runTest("Top Series Follow Series Grouping With Books Opened and Finished", () =>
        {
            var now = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
            var comics = new List<ComicEntity>
            {
                new() { Id = 1, Title = "Orbit 01", FilePath = "a", PageCount = 20, IsCompleted = true },
                new() { Id = 2, Title = "Orbit 02", FilePath = "b", PageCount = 20, LastReadPage = 5 },
                new() { Id = 3, Title = "Orbit 03", FilePath = "c", PageCount = 20 },
                new() { Id = 4, Title = "Lonely Book", FilePath = "d", PageCount = 30 }
            };
            var sessions = new List<ReadingSessionRecord>
            {
                new(1, now.AddHours(-5), 600, 20),
                new(2, now.AddHours(-2), 300, 6),
                new(4, now.AddHours(-1), 120, 4)
            };
            var membership = new Dictionary<long, SeriesMembership>
            {
                [1] = new("orbit", "Orbit", 3),
                [2] = new("orbit", "Orbit", 3),
                [3] = new("orbit", "Orbit", 3)
            };
            var summary = ReadingStatsCalculator.Calculate(sessions, comics, null, now, TimeZoneInfo.Utc, membership);
            var top = summary.TopSeries;
            if (top.Count != 2 || top[0].SeriesName != "Orbit" || top[0].PagesRead != 26) throw new Exception("Top series grouping wrong");
            if (!top[0].IsSeries || top[0].TotalIssues != 3 || top[0].IssuesRead != 2 || top[0].IssuesCompleted != 1) throw new Exception("Series book counts wrong");
            if (top[0].BooksDisplay != "Opened 2 of 3 · finished 1") throw new Exception("Books display wrong: " + top[0].BooksDisplay);
            if (top[1].IsSeries || top[1].KindLabel != "ONE BOOK") throw new Exception("Standalone book should not be labelled a series");
            if (top[0].LastReadUtc != now.AddHours(-2)) throw new Exception("Last read time wrong");
            // Orbit: 20 (finished) + 6 (page 6 of #02) of 60 pages = 43%; Lonely Book unread so far = 0%.
            if (top[0].SeriesPagesRead != 26 || top[0].SeriesTotalPages != 60 || top[0].SeriesCompletionDisplay != "43% read")
                throw new Exception($"Series percent wrong: {top[0].SeriesPagesDisplay} {top[0].SeriesCompletionDisplay}");
            comics[3].LastReadPage = 4;
            var again = ReadingStatsCalculator.Calculate(sessions, comics, null, now, TimeZoneInfo.Utc, membership);
            if (again.TopSeries[1].SeriesCompletionDisplay != "17% read") throw new Exception("Single book percent wrong: " + again.TopSeries[1].SeriesCompletionDisplay);
            return Task.CompletedTask;
        });
    }
}
