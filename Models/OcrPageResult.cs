using System;
using System.Collections.Generic;

namespace Komik.Models;

public sealed class OcrWordBox
{
    public string Text { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

/// <summary>One recognized line, in page pixel coordinates.</summary>
public sealed class OcrLineBox
{
    public string Text { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public double CenterY => Y + Height / 2;
}

/// <summary>A speech bubble or caption: nearby lines grouped together, in reading order.</summary>
public sealed class OcrTextBlock
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string NumberDisplay => Number.ToString();
}

public sealed class OcrPageResult
{
    public int PageIndex { get; set; }
    public string FullText { get; set; } = string.Empty;
    public List<OcrWordBox> Words { get; set; } = new();
    public List<OcrLineBox> Lines { get; set; } = new();
    public List<OcrTextBlock> Blocks { get; set; } = new();
    public double ImageWidth { get; set; }
    public double ImageHeight { get; set; }
    public string LanguageName { get; set; } = string.Empty;

    public bool HasText => !string.IsNullOrWhiteSpace(FullText);
    public string PageDisplay => $"Page {PageIndex + 1}";
    public string SummaryDisplay => Blocks.Count == 0
        ? "No text found"
        : $"{Blocks.Count} text block{(Blocks.Count == 1 ? "" : "s")} · {Words.Count} word{(Words.Count == 1 ? "" : "s")}" + (string.IsNullOrEmpty(LanguageName) ? string.Empty : $" · {LanguageName}");

    public bool ContainsText(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(FullText))
            return false;
        if (FullText.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return true;

        // OCR often breaks a phrase over lines or drops spaces: compare with whitespace squeezed out.
        string squeezedQuery = Squeeze(query);
        return squeezedQuery.Length > 0 && Squeeze(FullText).IndexOf(squeezedQuery, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string Squeeze(string text)
    {
        var chars = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (!char.IsWhiteSpace(c) && c != '-') chars.Append(c);
        }
        return chars.ToString();
    }
}

public sealed class OcrSearchResultItem
{
    public int PageIndex { get; set; }
    public string Snippet { get; set; } = string.Empty;
    public string PageDisplay => $"Page {PageIndex + 1}";
}
