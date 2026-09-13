using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Komik.Helpers;
using Komik.Models;

namespace Komik.Services;

/// <summary>
/// Loads comic pages from a folder containing image files.
/// </summary>
public sealed class FolderComicLoader : IComicLoader
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".jfif", ".tif", ".tiff"
    };

    public bool CanLoad(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return false;
        try
        {
            return Directory.EnumerateFiles(path)
                .Any(f => SupportedExtensions.Contains(Path.GetExtension(f)));
        }
        catch
        {
            return false;
        }
    }

    public Task<ComicBook> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"The directory '{path}' does not exist.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var imageFiles = Directory.EnumerateFiles(path)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f)))
            .OrderBy(f => Path.GetFileName(f), NaturalSortComparer.Instance)
            .ToList();

        if (imageFiles.Count == 0)
        {
            throw new InvalidOperationException($"No supported image files found in '{Path.GetFileName(path)}'.");
        }

        string title = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(title))
        {
            title = path;
        }

        var pages = new List<IComicPage>(imageFiles.Count);
        for (int i = 0; i < imageFiles.Count; i++)
        {
            pages.Add(new FolderComicPage(i, imageFiles[i]));
        }

        var comicBook = new ComicBook(title, path, ComicSourceType.Folder, pages);
        return Task.FromResult(comicBook);
    }

    private sealed class FolderComicPage : IComicPage
    {
        private readonly string _filePath;

        public int Index { get; }
        public string DisplayName => Path.GetFileName(_filePath);

        public FolderComicPage(int index, string filePath)
        {
            Index = index;
            _filePath = filePath;
        }

        public async Task<ComicPageData> GetPageDataAsync(CancellationToken cancellationToken = default)
        {
            byte[] bytes = await File.ReadAllBytesAsync(_filePath, cancellationToken);
            return new ComicPageData(bytes, isRawBgra: false);
        }

        public void Dispose()
        {
            // No persistent resources to release
        }
    }
}
