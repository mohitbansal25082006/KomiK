using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Komik.Helpers;

public static class SeriesParserHelper
{
    public static (string SeriesName, double IssueNumber) ParseSeriesAndIssue(string title, string? filePath = null)
    {
        var identity = ComicIdentityParser.Parse(title, filePath);
        if (identity.HasNumber && !string.IsNullOrWhiteSpace(identity.SeriesName))
        {
            return (identity.SeriesName, identity.Issue ?? identity.Volume ?? 1.0);
        }

        // No number in the name: a meaningful parent folder usually names the series.
        string? folder = ComicIdentityParser.GetMeaningfulFolderName(filePath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            return (folder, 1.0);
        }

        return (string.IsNullOrWhiteSpace(identity.SeriesName) ? Path.GetFileNameWithoutExtension(title) : identity.SeriesName, 1.0);
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

    /// <summary>
    /// Optimal string alignment distance: like Levenshtein, but swapping two neighbouring letters costs 1 (common typo).
    /// </summary>
    public static int TranspositionDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        var d = new int[s.Length + 1, t.Length + 1];
        for (int i = 0; i <= s.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= t.Length; j++) d[0, j] = j;

        for (int i = 1; i <= s.Length; i++)
        {
            for (int j = 1; j <= t.Length; j++)
            {
                int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && s[i - 1] == t[j - 2] && s[i - 2] == t[j - 1])
                {
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
                }
            }
        }

        return d[s.Length, t.Length];
    }

    /// <summary>1.0 for equal strings, tolerant of letter swaps.</summary>
    public static double CalculateTypoSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 1.0;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;
        int maxLen = Math.Max(s1.Length, s2.Length);
        return 1.0 - (double)TranspositionDistance(s1, s2) / maxLen;
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

        var a = ComicIdentityParser.Parse(title1);
        var b = ComicIdentityParser.Parse(title2);
        if (a.SeriesKey.Length == 0 || b.SeriesKey.Length == 0) return false;
        if (a.SeriesKey == b.SeriesKey) return true;

        // Tolerate small spelling/punctuation differences in longer series names only.
        if (Math.Min(a.SeriesKey.Length, b.SeriesKey.Length) < 6) return false;
        return CalculateTypoSimilarity(a.SeriesKey, b.SeriesKey) >= threshold;
    }

    public static bool AreDuplicates(string title1, string title2, double threshold = 0.95)
    {
        if (string.IsNullOrWhiteSpace(title1) || string.IsNullOrWhiteSpace(title2)) return false;

        var a = ComicIdentityParser.Parse(title1);
        var b = ComicIdentityParser.Parse(title2);

        // Numbered books: only the exact same series + volume + issue can be duplicates (#1 is never a copy of #2).
        if (a.BookKey != null || b.BookKey != null)
        {
            return a.BookKey != null && a.BookKey == b.BookKey;
        }

        string keyA = ComicIdentityParser.MakeKey(a.CleanTitle);
        string keyB = ComicIdentityParser.MakeKey(b.CleanTitle);
        if (keyA.Length == 0 || keyB.Length == 0) return false;
        if (keyA == keyB) return true;
        return CalculateSimilarity(keyA, keyB) >= threshold;
    }
}
