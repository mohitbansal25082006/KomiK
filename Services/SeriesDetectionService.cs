using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Komik.Helpers;
using Komik.Models;

namespace Komik.Services;

public sealed class SeriesDetectionOptions
{
    /// <summary>Auto-detected series need at least this many distinct books (duplicate copies count once).</summary>
    public int MinIssues { get; set; } = 2;

    /// <summary>Series keys the user marked as "not a series".</summary>
    public ISet<string> IgnoredSeriesKeys { get; set; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Similarity (0..1) at which two long series keys are treated as the same series (typos, punctuation).</summary>
    public double FuzzyMergeThreshold { get; set; } = 0.9;

    /// <summary>Watched library folders. Books sitting directly in one of these never form a folder series.</summary>
    public IEnumerable<string> RootFolders { get; set; } = Array.Empty<string>();

    /// <summary>Folders holding more books than this are collections, not series.</summary>
    public int MaxFolderSeriesSize { get; set; } = 80;
}

/// <summary>Both views of a library: story continuations and everything grouped by creator.</summary>
public sealed class SeriesDetectionResult
{
    public List<ComicSeriesGroup> Series { get; } = new();
    public List<ComicSeriesGroup> Creators { get; } = new();
}

public interface ISeriesDetectionService
{
    List<ComicSeriesGroup> DetectSeries(
        IEnumerable<ComicEntity> comics,
        IReadOnlyDictionary<long, ComicMetadataEntity>? metadata = null,
        IEnumerable<ComicSeriesGroup>? manualSeries = null,
        SeriesDetectionOptions? options = null);

    SeriesDetectionResult DetectAll(
        IEnumerable<ComicEntity> comics,
        IReadOnlyDictionary<long, ComicMetadataEntity>? metadata = null,
        IEnumerable<ComicSeriesGroup>? manualSeries = null,
        SeriesDetectionOptions? options = null);
}

/// <summary>
/// Groups the whole library two ways.
/// Story series (continuations), strongest signal first: manual series → metadata series → parsed titles
/// (series + volume + issue/chapter/part, un-numbered first books joining their numbered sequels) with typo-tolerant
/// merging → titles by the same creator that clearly continue each other → folders of un-numbered books.
/// Creator collections: every creator credited on at least two different works.
/// </summary>
public sealed class SeriesDetectionService : ISeriesDetectionService
{
    public List<ComicSeriesGroup> DetectSeries(
        IEnumerable<ComicEntity> comics,
        IReadOnlyDictionary<long, ComicMetadataEntity>? metadata = null,
        IEnumerable<ComicSeriesGroup>? manualSeries = null,
        SeriesDetectionOptions? options = null) =>
        DetectAll(comics, metadata, manualSeries, options).Series;

    public SeriesDetectionResult DetectAll(
        IEnumerable<ComicEntity> comics,
        IReadOnlyDictionary<long, ComicMetadataEntity>? metadata = null,
        IEnumerable<ComicSeriesGroup>? manualSeries = null,
        SeriesDetectionOptions? options = null)
    {
        options ??= new SeriesDetectionOptions();
        var output = new SeriesDetectionResult();
        var result = output.Series;
        var present = comics.Where(c => !c.IsMissing).ToList();

        // 1. Manual series always win. Story manuals claim their comics; creator manuals replace the matching creator card.
        var claimed = new HashSet<long>();
        var manualCreators = new List<ComicSeriesGroup>();
        foreach (var manual in manualSeries ?? Enumerable.Empty<ComicSeriesGroup>())
        {
            if (manual.Issues.Count == 0) continue;
            manual.IsManual = true;
            manual.Source = SeriesSource.Manual;
            manual.SeriesKey = "manual:" + manual.ManualSeriesId.ToString(CultureInfo.InvariantCulture);
            manual.Metadata = metadata;
            manual.RefreshProperties();
            if (manual.Section == SeriesSection.Creator)
            {
                manualCreators.Add(manual);
                continue;
            }
            foreach (var issue in manual.Issues) claimed.Add(issue.Id);
            result.Add(manual);
        }

        // 2. Parse every present comic once.
        var allEntries = present
            .Select(c =>
            {
                ComicMetadataEntity? meta = null;
                metadata?.TryGetValue(c.Id, out meta);
                return new Entry(c, ComicIdentityParser.Parse(c, meta));
            })
            .ToList();
        var entries = allEntries.Where(e => !claimed.Contains(e.Comic.Id)).ToList();

        // 3. Buckets of identical series keys (numbered or not: "Title" + "Title 2" is a continuation).
        var loose = new List<Entry>();
        var keyed = new Dictionary<string, List<Entry>>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            string key = entry.Identity.SeriesKey;
            int minKey = entry.Identity.HasNumber || entry.Identity.FromMetadata ? 2 : 3;
            if (key.Length < minKey)
            {
                loose.Add(entry);
                continue;
            }

            if (!keyed.TryGetValue(key, out var list))
            {
                list = new List<Entry>();
                keyed[key] = list;
            }
            list.Add(entry);
        }

        // Same title by different credited creators = different works.
        var buckets = new List<Bucket>();
        foreach (var kvp in keyed.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var parts = SplitByCreators(kvp.Value);
            foreach (var part in parts)
            {
                buckets.Add(new Bucket(kvp.Key, part, parts.Count > 1));
            }
        }

        var uf = new UnionFind(buckets.Count);

        // 4. Typo merge between numbered/metadata buckets ("invincible" vs "invincibel"; "batman2099" vs "batman1989" stays apart).
        var numbered = Enumerable.Range(0, buckets.Count).Where(i => buckets[i].NumberedOrMetadata).ToList();
        foreach (var group in numbered.GroupBy(i => buckets[i].Key.Length >= 2 ? buckets[i].Key[..2] : buckets[i].Key))
        {
            var idx = group.ToList();
            for (int x = 0; x < idx.Count; x++)
            {
                for (int y = x + 1; y < idx.Count; y++)
                {
                    Bucket a = buckets[idx[x]], b = buckets[idx[y]];
                    if (!CreatorsCompatible(a, b)) continue;
                    if (IsTypoOf(a.Key, b.Key, options.FuzzyMergeThreshold)) uf.Union(idx[x], idx[y]);
                }
            }
        }

        // 5. Same creator + titles that continue each other ("Title" / "Title - Second Arc" / "Title Kouhen").
        var byCreator = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (int i = 0; i < buckets.Count; i++)
        {
            foreach (var creator in buckets[i].CreatorKeys)
            {
                if (!byCreator.TryGetValue(creator, out var list))
                {
                    list = new List<int>();
                    byCreator[creator] = list;
                }
                list.Add(i);
            }
        }

        foreach (var list in byCreator.Values)
        {
            if (list.Count < 2 || list.Count > 400) continue;
            for (int x = 0; x < list.Count; x++)
            {
                for (int y = x + 1; y < list.Count; y++)
                {
                    if (uf.Find(list[x]) == uf.Find(list[y])) continue;
                    if (TitlesContinue(buckets[list[x]], buckets[list[y]])) uf.Union(list[x], list[y]);
                }
            }
        }

        // 6. Components with at least two distinct books are series; the rest are loose.
        var entryWork = new Dictionary<long, string>();
        foreach (var component in Enumerable.Range(0, buckets.Count).GroupBy(uf.Find))
        {
            var members = component.SelectMany(i => buckets[i].Entries).ToList();
            int distinctBooks = members.Select(e => PositionKey(e.Identity)).Distinct(StringComparer.Ordinal).Count();
            if (distinctBooks < options.MinIssues)
            {
                loose.AddRange(members);
                continue;
            }

            bool splitByCreator = component.Any(i => buckets[i].SplitByCreator);
            var source = members.Any(e => e.Identity.FromMetadata) ? SeriesSource.Metadata : SeriesSource.FileName;
            var seriesGroup = BuildGroup(members, metadata, source);
            if (splitByCreator)
            {
                string? creator = PrimaryCreatorKey(members);
                if (creator != null) seriesGroup.SeriesKey += "@" + creator;
            }

            foreach (var m in members) entryWork[m.Comic.Id] = seriesGroup.SeriesKey;
            if (!options.IgnoredSeriesKeys.Contains(seriesGroup.SeriesKey)) result.Add(seriesGroup);
        }

        // 7. Folder series: several un-grouped books in a meaningful sub-folder (never a watched root or a big collection).
        var roots = new HashSet<string>(
            (options.RootFolders ?? Array.Empty<string>()).Select(NormalizeDirectory).Where(r => r.Length > 0),
            StringComparer.OrdinalIgnoreCase);

        foreach (var folderGroup in loose.GroupBy(e => NormalizeDirectory(SafeDirectory(e.Comic.FilePath)), StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(folderGroup.Key) || roots.Contains(folderGroup.Key)) continue;
            var members = folderGroup.ToList();
            if (members.Count < options.MinIssues || members.Count > options.MaxFolderSeriesSize) continue;

            // Books by several different credited creators are a mixed collection, not a series.
            int creatorsInFolder = members
                .Select(m => m.Identity.TitleCreators.Count > 0 ? ComicIdentityParser.MakeKey(m.Identity.TitleCreators[0]) : null)
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct(StringComparer.Ordinal)
                .Count();
            if (creatorsInFolder > 1) continue;

            string? folderName = ComicIdentityParser.GetMeaningfulFolderName(members[0].Comic.FilePath);
            if (string.IsNullOrWhiteSpace(folderName)) continue;

            var group = BuildGroup(members, metadata, SeriesSource.Folder, folderName);
            foreach (var m in members) entryWork[m.Comic.Id] = group.SeriesKey;
            if (!options.IgnoredSeriesKeys.Contains(group.SeriesKey)) result.Add(group);
        }

        // 8. Creator collections across the whole library (manual-claimed comics included).
        var autoCreators = BuildCreatorGroups(allEntries, entryWork, metadata, options);
        output.Creators.AddRange(manualCreators);
        output.Creators.AddRange(autoCreators.Where(auto => !manualCreators.Any(m => IsSameGroup(m, auto))));
        return output;
    }

    /// <summary>A manual group stands in for an automatic one when it was saved from it or holds most of its comics.</summary>
    public static bool IsSameGroup(ComicSeriesGroup manual, ComicSeriesGroup auto)
    {
        if (!string.IsNullOrEmpty(manual.SourceKey) && string.Equals(manual.SourceKey, auto.SeriesKey, StringComparison.Ordinal)) return true;
        if (auto.Issues.Count == 0) return false;
        var mine = new HashSet<long>(manual.Issues.Select(i => i.Id));
        int shared = auto.Issues.Count(i => mine.Contains(i.Id));
        return shared > 0 && shared * 2 >= auto.Issues.Count;
    }

    /// <summary>
    /// Comics an auto-updating manual group should gain: members of the automatic groups (same section) it was saved from
    /// or already overlaps, minus comics it holds and comics the user removed from it.
    /// </summary>
    public static List<ComicEntity> FindAutoUpdateAdditions(ComicSeriesGroup manual, SeriesDetectionResult automatic)
    {
        var additions = new List<ComicEntity>();
        if (!manual.IsAutoUpdate) return additions;

        var pool = manual.Section == SeriesSection.Creator ? automatic.Creators : automatic.Series;
        var mine = new HashSet<long>(manual.Issues.Select(i => i.Id));
        foreach (var auto in pool.Where(g => !g.IsManual))
        {
            bool related = (!string.IsNullOrEmpty(manual.SourceKey) && string.Equals(manual.SourceKey, auto.SeriesKey, StringComparison.Ordinal))
                           || auto.Issues.Any(i => mine.Contains(i.Id));
            if (!related) continue;

            foreach (var comic in auto.Issues)
            {
                if (comic.IsMissing || mine.Contains(comic.Id) || manual.ExcludedComicIds.Contains(comic.Id)) continue;
                if (additions.Any(a => a.Id == comic.Id)) continue;
                additions.Add(comic);
            }
        }
        return additions;
    }

    /// <summary>Reading order for a series: specials last, then volume, year and number (un-numbered first books first).</summary>
    public static List<ComicEntity> OrderForReading(IEnumerable<ComicEntity> comics, IReadOnlyDictionary<long, ComicMetadataEntity>? metadata = null, bool groupByTitle = false)
    {
        var entries = comics.Select(c =>
        {
            ComicMetadataEntity? meta = null;
            metadata?.TryGetValue(c.Id, out meta);
            return new Entry(c, ComicIdentityParser.Parse(c, meta));
        }).ToList();
        bool allHaveYears = entries.All(e => e.Identity.Year.HasValue);
        return entries
            .OrderBy(e => groupByTitle ? e.Identity.SeriesKey : string.Empty, StringComparer.Ordinal)
            .ThenBy(e => e.Identity.Kind is ComicNumberKind.Annual or ComicNumberKind.Special ? 1 : 0)
            .ThenBy(e => e.Identity.Volume ?? 0)
            .ThenBy(e => allHaveYears ? e.Identity.Year ?? 0 : 0)
            .ThenBy(e => e.Identity.HasNumber ? e.Identity.Issue ?? double.MaxValue : 1)
            .ThenBy(e => e.Comic.Title, new NaturalSortComparer())
            .Select(e => e.Comic)
            .ToList();
    }

    private static List<ComicSeriesGroup> BuildCreatorGroups(
        List<Entry> entries,
        Dictionary<long, string> entryWork,
        IReadOnlyDictionary<long, ComicMetadataEntity>? metadata,
        SeriesDetectionOptions options)
    {
        var byCreator = new Dictionary<string, List<(Entry Entry, int Rank, string Spelling)>>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            for (int i = 0; i < entry.Identity.Creators.Count; i++)
            {
                string spelling = entry.Identity.Creators[i];
                string key = ComicIdentityParser.MakeKey(spelling);
                if (key.Length < 2) continue;
                if (!byCreator.TryGetValue(key, out var list))
                {
                    list = new();
                    byCreator[key] = list;
                }
                if (!list.Any(x => x.Entry.Comic.Id == entry.Comic.Id)) list.Add((entry, i, spelling));
            }
        }

        // Near-identical long creator keys are the same person ("nasipasuta" / "nasipasuya" style romanization slips).
        var keys = byCreator.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        var uf = new UnionFind(keys.Count);
        for (int x = 0; x < keys.Count; x++)
        {
            for (int y = x + 1; y < keys.Count && keys[y].StartsWith(keys[x][..Math.Min(3, keys[x].Length)], StringComparison.Ordinal); y++)
            {
                string a = keys[x], b = keys[y];
                if (Math.Min(a.Length, b.Length) < 8 || DigitsDiffer(a, b)) continue;
                if (SeriesParserHelper.TranspositionDistance(a, b) <= 1) uf.Union(x, y);
            }
        }

        var candidates = new List<(string Key, List<(Entry Entry, int Rank, string Spelling)> Items)>();
        foreach (var component in Enumerable.Range(0, keys.Count).GroupBy(uf.Find))
        {
            var items = component.SelectMany(i => byCreator[keys[i]])
                .GroupBy(x => x.Entry.Comic.Id)
                .Select(g => g.OrderBy(x => x.Rank).First())
                .ToList();
            if (items.Count < 2) continue;

            int works = items
                .Select(x => entryWork.TryGetValue(x.Entry.Comic.Id, out var w) ? w : (x.Entry.Identity.SeriesKey.Length > 0 ? x.Entry.Identity.SeriesKey : "id" + x.Entry.Comic.Id))
                .Distinct(StringComparer.Ordinal)
                .Count();
            if (works < 2) continue;

            string key = component.Select(i => keys[i]).OrderByDescending(k => byCreator[k].Count).First();
            candidates.Add((key, items));
        }

        // A circle and its only artist credit the same books: show one card named after both.
        var groups = new List<ComicSeriesGroup>();
        foreach (var same in candidates.GroupBy(c => string.Join(",", c.Items.Select(i => i.Entry.Comic.Id).OrderBy(i => i))))
        {
            var ordered = same.OrderBy(c => c.Items.Average(i => i.Rank)).ToList();
            var names = ordered
                .Select(c => c.Items.GroupBy(i => i.Spelling).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key.Any(char.IsLower)).First().Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var members = ordered[0].Items.Select(i => i.Entry).ToList();
            string seriesKey = "creator:" + ordered[0].Key;
            if (options.IgnoredSeriesKeys.Contains(seriesKey)) continue;

            var workOf = members.ToDictionary(
                m => m.Comic.Id,
                m => entryWork.TryGetValue(m.Comic.Id, out var w) ? w : m.Identity.SeriesKey);

            var sorted = members
                .OrderBy(m => workOf[m.Comic.Id], StringComparer.Ordinal)
                .ThenBy(m => m.Identity.Volume ?? 0)
                .ThenBy(m => m.Identity.Position)
                .ThenBy(m => m.Comic.Title, new NaturalSortComparer())
                .ToList();

            var group = new ComicSeriesGroup
            {
                SeriesName = string.Join(" · ", names),
                SeriesKey = seriesKey,
                Source = SeriesSource.Creator,
                Section = SeriesSection.Creator,
                Metadata = metadata,
                WorkCount = workOf.Values.Distinct(StringComparer.Ordinal).Count()
            };
            foreach (var m in sorted) group.Issues.Add(m.Comic);
            group.CoverThumbnailPath = sorted
                .OrderByDescending(m => m.Comic.LastReadAt ?? DateTime.MinValue)
                .Select(m => m.Comic.ThumbnailPath)
                .FirstOrDefault(p => !string.IsNullOrEmpty(p));
            group.RefreshProperties();
            groups.Add(group);
        }

        return groups
            .OrderByDescending(g => g.WorkCount)
            .ThenByDescending(g => g.IssueCount)
            .ThenBy(g => g.SeriesName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ComicSeriesGroup BuildGroup(
        List<Entry> members,
        IReadOnlyDictionary<long, ComicMetadataEntity>? metadata,
        SeriesSource source,
        string? forcedName = null)
    {
        bool allHaveYears = members.All(e => e.Identity.Year.HasValue);
        var ordered = members
            .OrderBy(e => e.Identity.Kind is ComicNumberKind.Annual or ComicNumberKind.Special ? 1 : 0)
            .ThenBy(e => e.Identity.Volume ?? 0)
            .ThenBy(e => allHaveYears ? e.Identity.Year ?? 0 : 0)
            .ThenBy(e => e.Identity.HasNumber ? e.Identity.Issue ?? double.MaxValue : 1)
            .ThenBy(e => e.Comic.Title, new NaturalSortComparer())
            .ToList();

        string name = forcedName ?? PickSeriesName(members);
        var group = new ComicSeriesGroup
        {
            SeriesName = name,
            SeriesKey = source == SeriesSource.Folder ? "folder:" + ComicIdentityParser.MakeKey(SafeDirectory(members[0].Comic.FilePath)) : ComicIdentityParser.MakeKey(name),
            Source = source,
            Metadata = metadata
        };

        foreach (var entry in ordered)
        {
            group.Issues.Add(entry.Comic);
        }

        group.Creators = members
            .SelectMany(e => e.Identity.TitleCreators.Count > 0 ? e.Identity.TitleCreators : e.Identity.Creators)
            .GroupBy(ComicIdentityParser.MakeKey)
            .OrderByDescending(g => g.Count())
            .Select(g => g.First())
            .Take(3)
            .ToList();
        group.CoverThumbnailPath = ordered.Select(e => e.Comic.ThumbnailPath).FirstOrDefault(p => !string.IsNullOrEmpty(p));
        group.RefreshProperties();
        return group;
    }

    /// <summary>
    /// Metadata names win; one spelling shared by everyone wins; otherwise the name every other title extends
    /// ("Title" for "Title" + "Title - Second Arc"), then shared opening words, then the most common spelling.
    /// </summary>
    private static string PickSeriesName(List<Entry> members)
    {
        var fromMeta = members.Where(e => e.Identity.FromMetadata).GroupBy(e => e.Identity.SeriesName, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count()).FirstOrDefault();
        if (fromMeta != null) return fromMeta.First().Identity.SeriesName;

        var spellings = members
            .GroupBy(e => e.Identity.SeriesName, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key.Length)
            .ToList();
        string mostCommon = spellings.FirstOrDefault()?.First().Identity.SeriesName ?? "Series";

        var distinctKeys = members.Select(e => e.Identity.SeriesKey).Distinct(StringComparer.Ordinal).ToList();
        if (distinctKeys.Count <= 1) return mostCommon;

        var prefix = spellings
            .Select(g => g.First().Identity)
            .OrderBy(i => i.SeriesKey.Length)
            .FirstOrDefault(i => i.SeriesKey.Length >= 3 && distinctKeys.All(k => k.StartsWith(i.SeriesKey, StringComparison.Ordinal)));
        if (prefix != null) return prefix.SeriesName;

        var wordLists = spellings.Select(g => g.First().Identity.SeriesName.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToList();
        int shared = 0;
        while (wordLists.All(w => w.Length > shared) &&
               wordLists.All(w => string.Equals(ComicIdentityParser.MakeKey(w[shared]), ComicIdentityParser.MakeKey(wordLists[0][shared]), StringComparison.Ordinal)))
        {
            shared++;
        }
        if (shared >= 2)
        {
            string common = string.Join(" ", wordLists[0].Take(shared)).Trim(' ', '-', '–', ':', ',');
            if (common.Length >= 6) return common;
        }

        return mostCommon;
    }

    /// <summary>Splits one title key into works by different credited creators; uncredited copies join the largest work.</summary>
    private static List<List<Entry>> SplitByCreators(List<Entry> entries)
    {
        if (entries.Count < 2 || entries.Any(e => e.Identity.FromMetadata)) return new List<List<Entry>> { entries };

        var credited = entries.Where(e => e.Identity.TitleCreators.Count > 0).ToList();
        if (credited.Count < 2) return new List<List<Entry>> { entries };

        var uf = new UnionFind(credited.Count);
        var firstOwner = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < credited.Count; i++)
        {
            foreach (var key in credited[i].Identity.TitleCreators.Select(ComicIdentityParser.MakeKey))
            {
                if (firstOwner.TryGetValue(key, out int owner)) uf.Union(owner, i);
                else firstOwner[key] = i;
            }
        }

        var parts = Enumerable.Range(0, credited.Count)
            .GroupBy(uf.Find)
            .Select(g => g.Select(i => credited[i]).ToList())
            .OrderByDescending(p => p.Count)
            .ToList();
        parts[0].AddRange(entries.Where(e => e.Identity.TitleCreators.Count == 0));
        return parts;
    }

    private static bool CreatorsCompatible(Bucket a, Bucket b) =>
        a.CreatorKeys.Count == 0 || b.CreatorKeys.Count == 0 || a.CreatorKeys.Overlaps(b.CreatorKeys);

    private static bool IsTypoOf(string a, string b, double threshold)
    {
        if (Math.Min(a.Length, b.Length) < 6) return false;
        if (Math.Abs(a.Length - b.Length) > Math.Max(2, Math.Max(a.Length, b.Length) / 8)) return false;
        if (DigitsDiffer(a, b)) return false;
        bool oneTypo = Math.Min(a.Length, b.Length) >= 7 && SeriesParserHelper.TranspositionDistance(a, b) <= 1;
        return oneTypo || SeriesParserHelper.CalculateTypoSimilarity(a, b) >= threshold;
    }

    /// <summary>Two title buckets by the same creator that read as one continuing story.</summary>
    private static bool TitlesContinue(Bucket a, Bucket b)
    {
        string ka = a.Key, kb = b.Key;
        if (ka == kb) return true;
        if (DigitsDiffer(ka, kb)) return false;

        string shorter = ka.Length <= kb.Length ? ka : kb;
        string longer = ka.Length <= kb.Length ? kb : ka;
        if (shorter.Length >= 10 && longer.StartsWith(shorter, StringComparison.Ordinal)) return true;

        if (shorter.Length >= 8 && SeriesParserHelper.CalculateTypoSimilarity(ka, kb) >= 0.85) return true;

        // "Original - Translated": one side's alternate title names the other.
        foreach (var alt in a.AltKeys)
        {
            if (alt.Length >= 10 && (b.AltKeys.Contains(alt) || SeriesParserHelper.CalculateTypoSimilarity(alt, kb) >= 0.9)) return true;
        }
        foreach (var alt in b.AltKeys)
        {
            if (alt.Length >= 10 && SeriesParserHelper.CalculateTypoSimilarity(alt, ka) >= 0.9) return true;
        }

        // Shared opening words: "Long Story Name Chapter Title" / "Long Story Name Another Title".
        int shared = 0;
        while (shared < a.Words.Length && shared < b.Words.Length && a.Words[shared] == b.Words[shared]) shared++;
        return shared >= 3 && a.Words.Take(shared).Sum(w => w.Length) >= 16;
    }

    private static string PositionKey(ComicIdentity id)
    {
        string kind = id.Kind switch
        {
            ComicNumberKind.Annual => "a",
            ComicNumberKind.Special => "s",
            _ => string.Empty
        };
        string vol = id.Volume.HasValue ? id.Volume.Value.ToString("0.###", CultureInfo.InvariantCulture) : "-";
        string pos = id.HasNumber ? (id.Issue.HasValue ? id.Issue.Value.ToString("0.###", CultureInfo.InvariantCulture) : "-") : "1";
        if (!id.HasNumber) vol = "-";
        return $"{vol}|{kind}{pos}";
    }

    private static string? PrimaryCreatorKey(List<Entry> members) => members
        .SelectMany(e => e.Identity.TitleCreators.Select(ComicIdentityParser.MakeKey))
        .Where(k => k.Length > 0)
        .GroupBy(k => k)
        .OrderByDescending(g => g.Count())
        .Select(g => g.Key)
        .FirstOrDefault();

    /// <summary>"batman2099" and "batman1989" differ only by digits: different series even though they look similar.</summary>
    private static bool DigitsDiffer(string a, string b)
    {
        string da = new(a.Where(char.IsDigit).ToArray());
        string db = new(b.Where(char.IsDigit).ToArray());
        return da != db;
    }

    private static string SafeDirectory(string path)
    {
        try
        {
            return Path.GetDirectoryName(path) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string NormalizeDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        try
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path.TrimEnd('\\', '/');
        }
    }

    private sealed record Entry(ComicEntity Comic, ComicIdentity Identity);

    private sealed class Bucket
    {
        public Bucket(string key, List<Entry> entries, bool splitByCreator)
        {
            Key = key;
            Entries = entries;
            SplitByCreator = splitByCreator;
            NumberedOrMetadata = entries.Any(e => e.Identity.HasNumber || e.Identity.FromMetadata);
            CreatorKeys = new HashSet<string>(
                entries.SelectMany(e => e.Identity.TitleCreators.Count > 0 ? e.Identity.TitleCreators : e.Identity.Creators)
                    .Select(ComicIdentityParser.MakeKey)
                    .Where(k => k.Length >= 2),
                StringComparer.Ordinal);
            AltKeys = new HashSet<string>(entries.Select(e => e.Identity.AltTitleKey).Where(k => k.Length > 0), StringComparer.Ordinal);
            Words = (entries[0].Identity.SeriesName ?? string.Empty)
                .Split(new[] { ' ', '-', '–', ':', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ComicIdentityParser.MakeKey)
                .Where(w => w.Length > 0)
                .ToArray();
        }

        public string Key { get; }
        public List<Entry> Entries { get; }
        public bool SplitByCreator { get; }
        public bool NumberedOrMetadata { get; }
        public HashSet<string> CreatorKeys { get; }
        public HashSet<string> AltKeys { get; }
        public string[] Words { get; }
    }

    private sealed class UnionFind
    {
        private readonly int[] _parent;

        public UnionFind(int size)
        {
            _parent = Enumerable.Range(0, size).ToArray();
        }

        public int Find(int x)
        {
            while (_parent[x] != x)
            {
                _parent[x] = _parent[_parent[x]];
                x = _parent[x];
            }
            return x;
        }

        public void Union(int a, int b)
        {
            int ra = Find(a), rb = Find(b);
            if (ra != rb) _parent[Math.Max(ra, rb)] = Math.Min(ra, rb);
        }
    }
}
