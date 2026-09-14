using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Komik.Helpers;
using Komik.Models;

namespace Komik.Services;

public interface IDuplicateDetectionService
{
    List<DuplicateComicGroup> FindDuplicates(
        IEnumerable<ComicEntity> comics,
        HashSet<(long, long)> ignoredPairs);
}

public class DuplicateDetectionService : IDuplicateDetectionService
{
    private static readonly Regex QualityTagPattern = new(
        @"[\(\[](?:digital|webrip|c2c|scanned|hd|remastered|zone|empire|dcp|novus|minutemen|1080p|720p).*?[\)\]]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CleanPunctuationPattern = new(
        @"[^\w\s#]",
        RegexOptions.Compiled);

    public List<DuplicateComicGroup> FindDuplicates(
        IEnumerable<ComicEntity> comics,
        HashSet<(long, long)> ignoredPairs)
    {
        var comicList = comics.Where(c => !c.IsMissing).ToList();
        var groups = new List<DuplicateComicGroup>();
        var pairedIds = new HashSet<long>();

        // 1. Group by exact normalized file name without extension
        var byBaseName = new Dictionary<string, List<ComicEntity>>(StringComparer.OrdinalIgnoreCase);
        foreach (var comic in comicList)
        {
            string baseName = Path.GetFileNameWithoutExtension(comic.FilePath).Trim();
            baseName = QualityTagPattern.Replace(baseName, string.Empty).Trim();
            baseName = CleanPunctuationPattern.Replace(baseName, " ").Trim();
            // Collapse multiple spaces
            baseName = Regex.Replace(baseName, @"\s+", " ");

            if (baseName.Length < 3) continue;

            if (!byBaseName.TryGetValue(baseName, out var list))
            {
                list = new List<ComicEntity>();
                byBaseName[baseName] = list;
            }
            list.Add(comic);
        }

        foreach (var kvp in byBaseName)
        {
            if (kvp.Value.Count > 1)
            {
                // Filter out pairs that are ignored
                var activeCopies = FilterActiveCopies(kvp.Value, ignoredPairs);
                if (activeCopies.Count > 1)
                {
                    var group = new DuplicateComicGroup
                    {
                        GroupTitle = kvp.Value[0].Title,
                        MatchReason = GetMatchReason(activeCopies)
                    };
                    foreach (var c in activeCopies)
                    {
                        group.Copies.Add(c);
                        pairedIds.Add(c.Id);
                    }
                    groups.Add(group);
                }
            }
        }

        // 2. Group by parsed Series + Issue Number for comics not yet grouped
        var remainingComics = comicList.Where(c => !pairedIds.Contains(c.Id)).ToList();
        var bySeriesIssue = new Dictionary<string, List<ComicEntity>>(StringComparer.OrdinalIgnoreCase);

        foreach (var comic in remainingComics)
        {
            var (series, issue) = SeriesParserHelper.ParseSeriesAndIssue(comic.Title, comic.FilePath);
            if (string.IsNullOrWhiteSpace(series) || series.Length < 3) continue;

            string key = $"{series.ToLowerInvariant()}__#__{issue}";
            if (!bySeriesIssue.TryGetValue(key, out var list))
            {
                list = new List<ComicEntity>();
                bySeriesIssue[key] = list;
            }
            list.Add(comic);
        }

        foreach (var kvp in bySeriesIssue)
        {
            if (kvp.Value.Count > 1)
            {
                var activeCopies = FilterActiveCopies(kvp.Value, ignoredPairs);
                if (activeCopies.Count > 1)
                {
                    var (series, issue) = SeriesParserHelper.ParseSeriesAndIssue(activeCopies[0].Title, activeCopies[0].FilePath);
                    var group = new DuplicateComicGroup
                    {
                        GroupTitle = $"{series} #{issue}",
                        MatchReason = $"Matching Series & Issue #{issue} ({string.Join(", ", activeCopies.Select(c => c.Format.ToString().ToUpperInvariant()).Distinct())})"
                    };
                    foreach (var c in activeCopies)
                    {
                        group.Copies.Add(c);
                        pairedIds.Add(c.Id);
                    }
                    groups.Add(group);
                }
            }
        }

        // 3. Group by 95%+ title similarity or exact file size + page count (cross-format identical books)
        var unassigned = comicList.Where(c => !pairedIds.Contains(c.Id)).ToList();
        for (int i = 0; i < unassigned.Count; i++)
        {
            var primary = unassigned[i];
            if (pairedIds.Contains(primary.Id)) continue;

            var matchingCopies = new List<ComicEntity> { primary };

            for (int j = i + 1; j < unassigned.Count; j++)
            {
                var secondary = unassigned[j];
                if (pairedIds.Contains(secondary.Id)) continue;

                // Check 95% title match
                bool titleMatch95 = SeriesParserHelper.AreDuplicates(primary.Title, secondary.Title, 0.95);

                // Check exact file size and page count
                bool contentMatch = (primary.FileSize > 1000 && primary.FileSize == secondary.FileSize &&
                                     primary.PageCount > 0 && primary.PageCount == secondary.PageCount);

                if (titleMatch95 || contentMatch)
                {
                    matchingCopies.Add(secondary);
                }
            }

            if (matchingCopies.Count > 1)
            {
                var activeCopies = FilterActiveCopies(matchingCopies, ignoredPairs);
                if (activeCopies.Count > 1)
                {
                    string matchReason = GetMatchReason(activeCopies);
                    var group = new DuplicateComicGroup
                    {
                        GroupTitle = activeCopies[0].Title,
                        MatchReason = matchReason
                    };
                    foreach (var c in activeCopies)
                    {
                        group.Copies.Add(c);
                        pairedIds.Add(c.Id);
                    }
                    groups.Add(group);
                }
            }
        }

        return groups;
    }

    private static List<ComicEntity> FilterActiveCopies(List<ComicEntity> copies, HashSet<(long, long)> ignoredPairs)
    {
        var result = new List<ComicEntity>();
        foreach (var copy in copies)
        {
            // If this copy is ignored against ALL other copies in the list, exclude it
            bool hasUnignoredPartner = copies.Any(other =>
                other.Id != copy.Id &&
                !ignoredPairs.Contains((Math.Min(copy.Id, other.Id), Math.Max(copy.Id, other.Id))));

            if (hasUnignoredPartner)
            {
                result.Add(copy);
            }
        }
        return result;
    }

    private static string GetMatchReason(List<ComicEntity> copies)
    {
        var formats = copies.Select(c => c.Format.ToString().ToUpperInvariant()).Distinct().ToList();
        if (formats.Count > 1)
        {
            return $"Multi-format duplicate: {string.Join(" vs ", formats)}";
        }
        return $"Duplicate file copy ({formats[0]}) across directories";
    }
}
