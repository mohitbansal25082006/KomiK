using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Komik.Helpers;
using Komik.Models;

namespace Komik.Services;

public interface IDuplicateDetectionService
{
    List<DuplicateComicGroup> FindDuplicates(
        IEnumerable<ComicEntity> comics,
        HashSet<(long, long)> ignoredPairs);

    List<DuplicateComicGroup> FindDuplicates(
        IEnumerable<ComicEntity> comics,
        HashSet<(long, long)> ignoredPairs,
        IReadOnlyDictionary<long, ComicMetadataEntity>? metadata,
        bool compareFileContents);
}

/// <summary>
/// Finds duplicate copies of the same book. Pairs are scored by independent signals and joined into groups:
/// <list type="bullet">
/// <item>Identical bytes (same size + content fingerprint) → Identical</item>
/// <item>Same series, volume and issue/chapter number (from metadata or name), compatible page counts → SameIssue</item>
/// <item>Same cleaned title (release tags removed) with no conflicting numbers → Likely</item>
/// <item>Near-identical title, identical numbers and page count → Possible</item>
/// </list>
/// Different issue numbers never match (so "#101" is not a copy of "#102"), and each group gets a recommended copy to keep.
/// </summary>
public class DuplicateDetectionService : IDuplicateDetectionService
{
    private const int FingerprintChunk = 64 * 1024;

    public List<DuplicateComicGroup> FindDuplicates(IEnumerable<ComicEntity> comics, HashSet<(long, long)> ignoredPairs) =>
        FindDuplicates(comics, ignoredPairs, metadata: null, compareFileContents: true);

    public List<DuplicateComicGroup> FindDuplicates(
        IEnumerable<ComicEntity> comics,
        HashSet<(long, long)> ignoredPairs,
        IReadOnlyDictionary<long, ComicMetadataEntity>? metadata,
        bool compareFileContents)
    {
        var list = comics.Where(c => !c.IsMissing).GroupBy(c => c.Id).Select(g => g.First()).ToList();
        if (list.Count < 2) return new List<DuplicateComicGroup>();

        var info = list.ToDictionary(c => c.Id, c =>
        {
            ComicMetadataEntity? meta = null;
            metadata?.TryGetValue(c.Id, out meta);
            var identity = ComicIdentityParser.Parse(c, meta);
            string titleKey = ComicIdentityParser.MakeKey(identity.CleanTitle);
            return new CopyInfo(c, identity, titleKey, ComicIdentityParser.NumberSignature(identity.CleanTitle));
        });

        var pairs = new Dictionary<(long, long), (DuplicateConfidence Confidence, string Reason)>();

        void AddPair(long a, long b, DuplicateConfidence confidence, string reason)
        {
            if (a == b) return;
            var key = (Math.Min(a, b), Math.Max(a, b));
            if (ignoredPairs.Contains(key)) return;
            if (!pairs.TryGetValue(key, out var existing) || existing.Confidence < confidence)
            {
                pairs[key] = (confidence, reason);
            }
        }

        // 1. Identical files: same size, then same content fingerprint (skipped when files can't be read).
        if (compareFileContents)
        {
            foreach (var sizeGroup in list.Where(c => c.FileSize > 0).GroupBy(c => c.FileSize).Where(g => g.Count() > 1))
            {
                var fingerprints = sizeGroup
                    .Select(c => (Comic: c, Hash: TryFingerprint(c.FilePath, c.FileSize)))
                    .Where(x => x.Hash != null)
                    .GroupBy(x => x.Hash!, StringComparer.Ordinal);

                foreach (var same in fingerprints.Where(g => g.Count() > 1))
                {
                    var items = same.ToList();
                    for (int i = 0; i < items.Count; i++)
                        for (int j = i + 1; j < items.Count; j++)
                            AddPair(items[i].Comic.Id, items[j].Comic.Id, DuplicateConfidence.Identical, "Byte-for-byte identical files");
                }
            }
        }

        // 2. Same book identity (series + volume + kind + number).
        foreach (var bookGroup in info.Values.Where(x => x.Identity.BookKey != null).GroupBy(x => x.Identity.BookKey!, StringComparer.Ordinal))
        {
            var items = bookGroup.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                for (int j = i + 1; j < items.Count; j++)
                {
                    var a = items[i];
                    var b = items[j];
                    if (a.Identity.Year.HasValue && b.Identity.Year.HasValue && a.Identity.Year != b.Identity.Year) continue;

                    string label = a.Identity.NumberLabel;
                    string name = string.IsNullOrWhiteSpace(label) ? a.Identity.SeriesName : $"{a.Identity.SeriesName} {label}";
                    if (PageCountsCompatible(a.Comic, b.Comic, 0.15))
                    {
                        AddPair(a.Comic.Id, b.Comic.Id, DuplicateConfidence.SameIssue, $"Same issue: {name}");
                    }
                    else if (PageCountsCompatible(a.Comic, b.Comic, 0.4))
                    {
                        AddPair(a.Comic.Id, b.Comic.Id, DuplicateConfidence.Possible, $"Same issue number, different page count ({a.Comic.PageCount} vs {b.Comic.PageCount}), maybe a variant edition");
                    }
                }
            }
        }

        // 3. Same cleaned title (release/scanner tags removed), numbers agree.
        foreach (var titleGroup in info.Values.Where(x => x.TitleKey.Length >= 3).GroupBy(x => x.TitleKey, StringComparer.Ordinal))
        {
            var items = titleGroup.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                for (int j = i + 1; j < items.Count; j++)
                {
                    if (!NumbersAgree(items[i], items[j])) continue;
                    if (!PageCountsCompatible(items[i].Comic, items[j].Comic, 0.25)) continue;
                    AddPair(items[i].Comic.Id, items[j].Comic.Id, DuplicateConfidence.Likely, "Same title, different release tags or format");
                }
            }
        }

        // 4. Near-identical titles with identical numbers and page counts (typos, punctuation). Bucketed to stay fast.
        foreach (var bucket in info.Values
                     .Where(x => x.TitleKey.Length >= 8)
                     .GroupBy(x => (x.Numbers, Prefix: x.TitleKey[..3])))
        {
            var items = bucket.ToList();
            if (items.Count < 2 || items.Count > 400) continue;
            for (int i = 0; i < items.Count; i++)
            {
                for (int j = i + 1; j < items.Count; j++)
                {
                    var a = items[i];
                    var b = items[j];
                    if (a.TitleKey == b.TitleKey) continue; // handled above
                    if (!NumbersAgree(a, b)) continue;
                    if (a.Comic.PageCount <= 0 || a.Comic.PageCount != b.Comic.PageCount) continue;
                    if (Math.Abs(a.TitleKey.Length - b.TitleKey.Length) > 3) continue;
                    if (SeriesParserHelper.CalculateTypoSimilarity(a.TitleKey, b.TitleKey) < 0.92) continue;
                    AddPair(a.Comic.Id, b.Comic.Id, DuplicateConfidence.Possible, "Nearly identical titles with the same page count");
                }
            }
        }

        return BuildGroups(list, info, pairs);
    }

    private static List<DuplicateComicGroup> BuildGroups(
        List<ComicEntity> comics,
        Dictionary<long, CopyInfo> info,
        Dictionary<(long, long), (DuplicateConfidence Confidence, string Reason)> pairs)
    {
        var parent = comics.ToDictionary(c => c.Id, c => c.Id);
        long Find(long x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }

        foreach (var (a, b) in pairs.Keys)
        {
            long ra = Find(a), rb = Find(b);
            if (ra != rb) parent[rb] = ra;
        }

        var groups = new List<DuplicateComicGroup>();
        foreach (var component in comics.GroupBy(c => Find(c.Id)).Where(g => g.Count() > 1))
        {
            var members = component.ToList();
            var ids = members.Select(m => m.Id).ToHashSet();
            var strongest = pairs
                .Where(p => ids.Contains(p.Key.Item1) && ids.Contains(p.Key.Item2))
                .Select(p => p.Value)
                .OrderByDescending(p => p.Confidence)
                .First();

            var ranked = members
                .Select(m => (Comic: m, Score: KeepScore(m)))
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Comic.DateAdded)
                .ToList();

            var first = info[ranked[0].Comic.Id].Identity;
            string title = first.HasNumber && !string.IsNullOrWhiteSpace(first.SeriesName)
                ? $"{first.SeriesName} {first.NumberLabel}".Trim()
                : ranked[0].Comic.Title;

            var group = new DuplicateComicGroup
            {
                GroupTitle = title,
                MatchReason = strongest.Reason,
                Confidence = strongest.Confidence
            };

            var best = ranked[0].Comic;
            foreach (var (comic, _) in ranked)
            {
                group.Copies.Add(comic);
                group.CopyItems.Add(new DuplicateCopyItem(comic)
                {
                    IsRecommended = ReferenceEquals(comic, best),
                    Highlights = DescribeCopy(comic, members)
                });
            }

            group.Refresh();
            groups.Add(group);
        }

        return groups
            .OrderByDescending(g => g.Confidence)
            .ThenByDescending(g => g.ReclaimableBytes)
            .ThenBy(g => g.GroupTitle, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Higher is better: reading progress and favorites matter most, then completeness and format.</summary>
    public static double KeepScore(ComicEntity c)
    {
        double score = 0;
        if (c.IsCompleted) score += 60;
        else if (c.LastReadPage > 0) score += 40 + Math.Min(15, c.LastReadPage / 10.0);
        if (c.IsFavorite) score += 30;
        score += Math.Min(25, c.PageCount / 8.0);
        score += c.FormatBadge switch
        {
            "CBZ" or "ZIP" => 15,
            "CB7" or "7Z" => 10,
            "CBR" or "RAR" => 8,
            "PDF" => 5,
            _ => 3
        };
        if (!string.IsNullOrEmpty(c.ThumbnailPath)) score += 2;
        score += Math.Min(8, c.FileSize / (64.0 * 1024 * 1024)); // bigger files are usually higher-resolution scans
        return score;
    }

    private static string DescribeCopy(ComicEntity comic, List<ComicEntity> members)
    {
        var notes = new List<string>();
        if (comic.IsCompleted) notes.Add("finished reading");
        else if (comic.LastReadPage > 0) notes.Add($"read to page {comic.LastReadPage + 1}");
        if (comic.IsFavorite) notes.Add("favorite");

        int maxPages = members.Max(m => m.PageCount);
        if (members.Any(m => m.PageCount != maxPages) && comic.PageCount == maxPages) notes.Add("most pages");

        long maxSize = members.Max(m => m.FileSize);
        if (members.Any(m => m.FileSize != maxSize) && comic.FileSize == maxSize) notes.Add("largest file");

        if (comic.FormatBadge is "CBZ" or "ZIP" && members.Any(m => m.FormatBadge is not ("CBZ" or "ZIP"))) notes.Add("open CBZ format");
        return string.Join(" · ", notes);
    }

    private static bool NumbersAgree(CopyInfo a, CopyInfo b)
    {
        // "X-Men 1 (1991)" and "X-Men 1 (2019)" are different books.
        if (a.Identity.Year.HasValue && b.Identity.Year.HasValue && a.Identity.Year != b.Identity.Year) return false;
        if (a.Identity.BookKey != null && b.Identity.BookKey != null) return a.Identity.BookKey == b.Identity.BookKey;
        return a.Numbers == b.Numbers;
    }

    private static bool PageCountsCompatible(ComicEntity a, ComicEntity b, double tolerance)
    {
        if (a.PageCount <= 0 || b.PageCount <= 0) return true;
        int max = Math.Max(a.PageCount, b.PageCount);
        return Math.Abs(a.PageCount - b.PageCount) <= Math.Max(2, max * tolerance);
    }

    /// <summary>SHA-256 over the file length plus its first and last 64 KB. Null when the file can't be read.</summary>
    public static string? TryFingerprint(string path, long expectedSize)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, FingerprintChunk);
            long length = stream.Length;
            if (expectedSize > 0 && length != expectedSize) return null;

            using var sha = SHA256.Create();
            byte[] header = BitConverter.GetBytes(length);
            sha.TransformBlock(header, 0, header.Length, null, 0);

            byte[] buffer = new byte[FingerprintChunk];
            int read = stream.Read(buffer, 0, buffer.Length);
            sha.TransformBlock(buffer, 0, read, null, 0);

            if (length > FingerprintChunk)
            {
                stream.Seek(Math.Max(FingerprintChunk, length - FingerprintChunk), SeekOrigin.Begin);
                read = stream.Read(buffer, 0, buffer.Length);
                sha.TransformBlock(buffer, 0, read, null, 0);
            }

            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return Convert.ToHexString(sha.Hash!);
        }
        catch
        {
            return null;
        }
    }

    private sealed record CopyInfo(ComicEntity Comic, ComicIdentity Identity, string TitleKey, string Numbers);
}
