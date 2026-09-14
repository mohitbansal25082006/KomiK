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

public sealed class OcrPageResult
{
    public int PageIndex { get; set; }
    public string FullText { get; set; } = string.Empty;
    public List<OcrWordBox> Words { get; set; } = new();
    public double ImageWidth { get; set; }
    public double ImageHeight { get; set; }

    public bool ContainsText(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(FullText))
            return false;
        return FullText.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

public sealed class OcrSearchResultItem
{
    public int PageIndex { get; set; }
    public string Snippet { get; set; } = string.Empty;
    public string PageDisplay => $"Page {PageIndex + 1}";
}
