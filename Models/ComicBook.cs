using System;
using System.Collections.Generic;

namespace Komik.Models;

/// <summary>
/// Represents an opened comic book loaded from a folder, ZIP/CBZ archive, or PDF document.
/// </summary>
public sealed class ComicBook : IDisposable
{
    private bool _disposed;

    public string Title { get; }
    public string FilePath { get; }
    public ComicSourceType SourceType { get; }
    public IReadOnlyList<IComicPage> Pages { get; }
    public int PageCount => Pages.Count;

    public ComicBook(string title, string filePath, ComicSourceType sourceType, IReadOnlyList<IComicPage> pages)
    {
        Title = title;
        FilePath = filePath;
        SourceType = sourceType;
        Pages = pages ?? Array.Empty<IComicPage>();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var page in Pages)
        {
            page.Dispose();
        }
    }
}
