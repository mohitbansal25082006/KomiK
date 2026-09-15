using System;
using System.Collections.Generic;
using System.Linq;
using Komik.Models;

namespace Komik.Services;

/// <summary>
/// Turns recognized lines into comic reading units: lines that sit together become one speech bubble or caption,
/// and bubbles are ordered like a reader's eye moves (left-to-right for comics, right-to-left for manga).
/// </summary>
public static class OcrLayout
{
    public static void BuildBlocks(OcrPageResult result, bool rightToLeft, bool joinWithoutSpaces)
    {
        var lines = result.Lines.Where(l => !string.IsNullOrWhiteSpace(l.Text) && l.Width > 0 && l.Height > 0).ToList();
        result.Blocks.Clear();
        if (lines.Count == 0)
        {
            result.FullText = string.Empty;
            return;
        }

        double typical = Median(lines.Select(l => l.Height));

        // Union lines that touch vertically and overlap (or nearly overlap) horizontally.
        var parent = Enumerable.Range(0, lines.Count).ToArray();
        int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }

        for (int i = 0; i < lines.Count; i++)
        {
            for (int j = i + 1; j < lines.Count; j++)
            {
                var a = lines[i];
                var b = lines[j];
                double lineHeight = Math.Max(Math.Min(a.Height, b.Height), typical * 0.6);
                double verticalGap = Math.Max(a.Y, b.Y) - Math.Min(a.Bottom, b.Bottom);
                double horizontalGap = Math.Max(a.X, b.X) - Math.Min(a.Right, b.Right);
                bool sameColumn = horizontalGap < lineHeight * 0.8;
                bool stacked = verticalGap < lineHeight * 0.95;
                // Side-by-side fragments of the same line (OCR sometimes splits them).
                bool sameRow = Math.Abs(a.CenterY - b.CenterY) < lineHeight * 0.45 && horizontalGap < lineHeight * 1.2;
                if ((sameColumn && stacked) || sameRow)
                {
                    parent[Find(i)] = Find(j);
                }
            }
        }

        var groups = lines.Select((line, index) => (line, root: Find(index)))
            .GroupBy(x => x.root)
            .Select(g => g.Select(x => x.line).ToList())
            .ToList();

        var blocks = new List<OcrTextBlock>();
        foreach (var group in groups)
        {
            double left = group.Min(l => l.X), top = group.Min(l => l.Y);
            double right = group.Max(l => l.Right), bottom = group.Max(l => l.Bottom);
            var ordered = group
                .OrderBy(l => Math.Round(l.CenterY / Math.Max(1, typical * 0.6)))
                .ThenBy(l => rightToLeft ? -l.X : l.X);
            string separator = joinWithoutSpaces ? string.Empty : " ";
            string text = string.Join(separator, ordered.Select(l => l.Text.Trim()));
            text = FixHyphenation(text);
            if (LooksLikeNoise(text)) continue;
            blocks.Add(new OcrTextBlock { Text = text, X = left, Y = top, Width = right - left, Height = bottom - top });
        }

        // Reading order: rows of bubbles from top to bottom, each row left-to-right (or right-to-left for manga).
        double band = Math.Max(typical * 3, 1);
        var sorted = blocks.OrderBy(b => b.Y).ToList();
        var rows = new List<List<OcrTextBlock>>();
        foreach (var block in sorted)
        {
            var row = rows.LastOrDefault();
            if (row != null && block.Y < row.Min(r => r.Y) + band && block.Y < row.Max(r => r.Y + r.Height))
            {
                row.Add(block);
            }
            else
            {
                rows.Add(new List<OcrTextBlock> { block });
            }
        }

        int number = 1;
        foreach (var row in rows)
        {
            foreach (var block in rightToLeft ? row.OrderByDescending(b => b.X + b.Width) : row.OrderBy(b => b.X))
            {
                block.Number = number++;
                result.Blocks.Add(block);
            }
        }

        result.FullText = string.Join(Environment.NewLine + Environment.NewLine, result.Blocks.Select(b => b.Text));
    }

    /// <summary>
    /// Artwork (buildings, rain, texture) sometimes reads as gibberish like "rrrwruur" or "111011'". Real lettering has
    /// vowels, no long consonant pile-ups and isn't a lone string of digits.
    /// </summary>
    public static bool LooksLikeNoise(string text)
    {
        string t = text.Trim();
        int letters = t.Count(char.IsLetter);
        int digits = t.Count(char.IsDigit);
        if (letters + digits < 2) return true;

        bool latinOnly = t.All(c => c < 0x250);
        if (!latinOnly) return false; // CJK, Cyrillic, etc.: leave to the engine

        if (letters == 0) return digits > 4 && !t.Contains(' ');

        var words = t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 2) return false;

        const string vowels = "aeiouyAEIOUY";
        // Sound effects like "BZZT" or "HMM" have no vowels but few different letters; texture noise has many.
        if (letters >= 6 && !t.Any(c => vowels.IndexOf(c) >= 0) && t.Where(char.IsLetter).Select(char.ToLowerInvariant).Distinct().Count() >= 4) return true;

        int run = 0, longest = 0;
        foreach (char c in t)
        {
            run = char.IsLetter(c) && vowels.IndexOf(c) < 0 ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }
        return longest >= 5 && t.Where(char.IsLetter).Select(char.ToLowerInvariant).Distinct().Count() >= 3;
    }

    /// <summary>Joins words split across lines with a trailing hyphen ("amaz- ing" becomes "amazing").</summary>
    private static string FixHyphenation(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, @"(\p{L})- (\p{Ll})", "$1$2");

    private static double Median(IEnumerable<double> values)
    {
        var list = values.OrderBy(v => v).ToList();
        if (list.Count == 0) return 1;
        return list[list.Count / 2];
    }

    /// <summary>
    /// Splits a tall page (webtoon strips) into overlapping bands the OCR engine can read, as (top, height) pairs.
    /// </summary>
    public static List<(int Top, int Height)> PlanTiles(int imageHeight, int tileHeight, int overlap)
    {
        var tiles = new List<(int, int)>();
        if (imageHeight <= tileHeight)
        {
            tiles.Add((0, imageHeight));
            return tiles;
        }

        int step = Math.Max(1, tileHeight - overlap);
        for (int top = 0; ; top += step)
        {
            int height = Math.Min(tileHeight, imageHeight - top);
            tiles.Add((top, height));
            if (top + height >= imageHeight) break;
        }
        return tiles;
    }
}
