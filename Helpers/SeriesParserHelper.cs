using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Komik.Helpers;

public static class SeriesParserHelper
{
    // Specific issue pattern: #12, issue 05, ch. 102, chapter 4
    private static readonly Regex SpecificIssuePattern = new(
        @"(?:[\s_.-]+(?:#|issue|ch|chapter)[\s_.-]*(\d+(?:\.\d+)?))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Volume pattern: v2, vol 03, volume 1
    private static readonly Regex VolumePattern = new(
        @"(?:[\s_.-]+(?:v|vol|volume)[\s_.-]*(\d+(?:\.\d+)?))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Trailing issue number pattern: "Batman 001", "Invincible 042 (2020)"
    private static readonly Regex TrailingNumberPattern = new(
        @"(?:[\s_.-]+(\d{1,4})(?:\s*[\(\[].*?[\)\]])?\.?[a-zA-Z0-9]*$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PunctuationCleanPattern = new(
        @"^[\s_.-]+|[\s_.-]+$",
        RegexOptions.Compiled);

    public static (string SeriesName, double IssueNumber) ParseSeriesAndIssue(string title, string? filePath = null)
    {
        string cleanTitle = Path.GetFileNameWithoutExtension(title);
        if (string.IsNullOrWhiteSpace(cleanTitle) && !string.IsNullOrWhiteSpace(filePath))
        {
            cleanTitle = Path.GetFileNameWithoutExtension(filePath);
        }

        // 1. Try explicit issue marker (#, issue, ch, chapter)
        var match = SpecificIssuePattern.Match(cleanTitle);
        if (!match.Success)
        {
            // 2. Try explicit volume marker (v, vol, volume)
            match = VolumePattern.Match(cleanTitle);
        }

        if (!match.Success)
        {
            // 3. Try trailing number
            match = TrailingNumberPattern.Match(cleanTitle);
        }

        if (match.Success)
        {
            string issueStr = match.Groups[1].Value;
            if (double.TryParse(issueStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double issueNum))
            {
                string seriesPart = cleanTitle.Substring(0, match.Index).Trim();
                seriesPart = PunctuationCleanPattern.Replace(seriesPart, string.Empty).Trim();

                if (!string.IsNullOrWhiteSpace(seriesPart))
                {
                    return (seriesPart, issueNum);
                }
            }
        }

        // Fallback: If title has parent folder name that looks like a series
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            try
            {
                string? parentDir = Path.GetFileName(Path.GetDirectoryName(filePath));
                if (!string.IsNullOrWhiteSpace(parentDir) && !parentDir.Equals("Comics", StringComparison.OrdinalIgnoreCase) && !parentDir.Equals("Downloads", StringComparison.OrdinalIgnoreCase))
                {
                    return (parentDir, 1.0);
                }
            }
            catch { }
        }

        return (cleanTitle, 1.0);
    }

    private static readonly Regex MetadataTagsPattern = new(
        @"[\(\[](?:digital|webrip|c2c|scanned|hd|remastered|zone|empire|dcp|novus|minutemen|1080p|720p|\d{4}).*?[\)\]]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        string s = Path.GetFileNameWithoutExtension(title);
        s = MetadataTagsPattern.Replace(s, string.Empty);
        s = Regex.Replace(s, @"\s+", " ").Trim().ToLowerInvariant();
        return s;
    }

    public static string NormalizeStructureForSeries(string title)
    {
        string norm = NormalizeTitle(title);
        // Replace numbers with '#' placeholder so issue #1 and #2 match structurally
        norm = Regex.Replace(norm, @"\b\d+(?:\.\d+)?\b", "#");
        return norm;
    }

    public static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[] dCurrent = new int[m + 1];
        int[] dPrevious = new int[m + 1];

        for (int j = 0; j <= m; j++) dPrevious[j] = j;

        for (int i = 1; i <= n; i++)
        {
            dCurrent[0] = i;
            char s_i = s[i - 1];

            for (int j = 1; j <= m; j++)
            {
                int cost = (s_i == t[j - 1]) ? 0 : 1;
                dCurrent[j] = Math.Min(
                    Math.Min(dCurrent[j - 1] + 1, dPrevious[j] + 1),
                    dPrevious[j - 1] + cost);
            }

            int[] temp = dPrevious;
            dPrevious = dCurrent;
            dCurrent = temp;
        }

        return dPrevious[m];
    }

    public static double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 1.0;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;
        if (string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase)) return 1.0;

        int maxLen = Math.Max(s1.Length, s2.Length);
        if (maxLen == 0) return 1.0;

        int dist = LevenshteinDistance(s1, s2);
        return 1.0 - ((double)dist / maxLen);
    }

    public static bool AreInSameSeries(string title1, string title2, double threshold = 0.90)
    {
        if (string.IsNullOrWhiteSpace(title1) || string.IsNullOrWhiteSpace(title2)) return false;

        string norm1 = NormalizeTitle(title1);
        string norm2 = NormalizeTitle(title2);

        if (string.Equals(norm1, norm2, StringComparison.OrdinalIgnoreCase))
        {
            // Same normalized title: identical issues or duplicates, but if different issue parsed, same series
            return true;
        }

        // 1. Raw similarity of full normalized title
        double rawSim = CalculateSimilarity(norm1, norm2);
        if (rawSim >= threshold) return true;

        // 2. Structural similarity (with issue numbers mapped to # placeholder)
        string struct1 = NormalizeStructureForSeries(title1);
        string struct2 = NormalizeStructureForSeries(title2);

        double structSim = CalculateSimilarity(struct1, struct2);
        if (structSim >= threshold) return true;

        return false;
    }

    public static bool AreDuplicates(string title1, string title2, double threshold = 0.95)
    {
        if (string.IsNullOrWhiteSpace(title1) || string.IsNullOrWhiteSpace(title2)) return false;

        string norm1 = NormalizeTitle(title1);
        string norm2 = NormalizeTitle(title2);

        if (string.Equals(norm1, norm2, StringComparison.OrdinalIgnoreCase)) return true;

        double sim = CalculateSimilarity(norm1, norm2);
        return sim >= threshold;
    }
}

