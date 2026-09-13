using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Komik.Helpers;
using Komik.Models;

namespace Komik.Services;

/// <summary>
/// Loads comic pages from ZIP and CBZ archives using built-in System.IO.Compression.
/// </summary>
public sealed class ZipComicLoader : IComicLoader
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cbz", ".zip"
    };

    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".jfif", ".tif", ".tiff"
    };

    public bool CanLoad(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        string ext = Path.GetExtension(path);
        return SupportedExtensions.Contains(ext);
    }

    public Task<ComicBook> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The archive file '{path}' does not exist.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Open archive for reading
        FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        ZipArchive archive;
        try
        {
            archive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: false);
        }
        catch (Exception ex)
        {
            fileStream.Dispose();
            throw new InvalidOperationException($"Failed to open '{Path.GetFileName(path)}' as a valid ZIP/CBZ archive: {ex.Message}", ex);
        }

        var validEntries = archive.Entries
            .Where(e => !string.IsNullOrEmpty(e.Name) &&
                        !e.FullName.StartsWith("__MACOSX", StringComparison.OrdinalIgnoreCase) &&
                        !e.Name.StartsWith(".", StringComparison.OrdinalIgnoreCase) &&
                        SupportedImageExtensions.Contains(Path.GetExtension(e.Name)))
            .OrderBy(e => e.FullName, NaturalSortComparer.Instance)
            .ToList();

        if (validEntries.Count == 0)
        {
            archive.Dispose();
            throw new InvalidOperationException($"No supported comic images found inside '{Path.GetFileName(path)}'.");
        }

        string title = Path.GetFileNameWithoutExtension(path);
        var archiveContext = new ZipArchiveContext(archive);

        var pages = new List<IComicPage>(validEntries.Count);
        for (int i = 0; i < validEntries.Count; i++)
        {
            pages.Add(new ZipComicPage(i, validEntries[i].FullName, validEntries[i].Name, archiveContext));
        }

        var comicBook = new ComicBook(title, path, ComicSourceType.ZipArchive, pages);
        return Task.FromResult(comicBook);
    }

    private sealed class ZipArchiveContext : IDisposable
    {
        private readonly ZipArchive _archive;
        private readonly object _syncLock = new();
        private bool _disposed;

        public ZipArchiveContext(ZipArchive archive)
        {
            _archive = archive;
        }

        public byte[] ReadEntryBytes(string entryFullName)
        {
            lock (_syncLock)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(ZipArchiveContext));

                var entry = _archive.GetEntry(entryFullName)
                    ?? throw new FileNotFoundException($"Entry '{entryFullName}' was not found in the archive.");

                using var stream = entry.Open();
                using var ms = new MemoryStream((int)entry.Length);
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public void Dispose()
        {
            lock (_syncLock)
            {
                if (_disposed) return;
                _disposed = true;
                _archive.Dispose();
            }
        }
    }

    private sealed class ZipComicPage : IComicPage
    {
        private readonly string _entryFullName;
        private readonly ZipArchiveContext _context;

        public int Index { get; }
        public string DisplayName { get; }

        public ZipComicPage(int index, string entryFullName, string displayName, ZipArchiveContext context)
        {
            Index = index;
            _entryFullName = entryFullName;
            DisplayName = displayName;
            _context = context;
        }

        public Task<ComicPageData> GetPageDataAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] bytes = _context.ReadEntryBytes(_entryFullName);
            return Task.FromResult(new ComicPageData(bytes, isRawBgra: false));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
