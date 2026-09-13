using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Komik.Helpers;
using Komik.Models;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Komik.Services;

/// <summary>
/// Loads comic pages from 7-Zip and CB7 archives (.7z, .cb7) using SharpCompress.
/// </summary>
public sealed class SevenZipComicLoader : IComicLoader
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cb7", ".7z"
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

        FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        IArchive archive;
        try
        {
            var options = new ReaderOptions { LeaveStreamOpen = false };
            archive = SevenZipArchive.OpenArchive(fileStream, options);
        }
        catch (CryptographicException)
        {
            fileStream.Dispose();
            throw new InvalidOperationException($"'{Path.GetFileName(path)}' is password-protected. Password-protected 7Z/CB7 archives are not supported.");
        }
        catch (Exception ex)
        {
            fileStream.Dispose();
            throw new InvalidOperationException($"Failed to open '{Path.GetFileName(path)}' as a valid 7Z/CB7 archive: {ex.Message}", ex);
        }

        List<IArchiveEntry> validEntries;
        try
        {
            if (archive.Entries.Any(e => e.IsEncrypted))
            {
                archive.Dispose();
                throw new InvalidOperationException($"'{Path.GetFileName(path)}' contains password-protected entries. Password-protected 7Z/CB7 archives are not supported.");
            }

            validEntries = archive.Entries
                .Where(e => !e.IsDirectory &&
                            !string.IsNullOrEmpty(e.Key) &&
                            !e.Key.StartsWith("__MACOSX", StringComparison.OrdinalIgnoreCase) &&
                            !Path.GetFileName(e.Key).StartsWith(".", StringComparison.OrdinalIgnoreCase) &&
                            !Path.GetFileName(e.Key).Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase) &&
                            SupportedImageExtensions.Contains(Path.GetExtension(e.Key)))
                .OrderBy(e => e.Key ?? string.Empty, NaturalSortComparer.Instance)
                .ToList();
        }
        catch (CryptographicException)
        {
            archive.Dispose();
            throw new InvalidOperationException($"'{Path.GetFileName(path)}' is password-protected. Password-protected 7Z/CB7 archives are not supported.");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            archive.Dispose();
            throw new InvalidOperationException($"Failed to read '{Path.GetFileName(path)}' as a valid 7Z/CB7 archive: {ex.Message}", ex);
        }

        if (validEntries.Count == 0)
        {
            archive.Dispose();
            throw new InvalidOperationException($"No supported comic images found inside '{Path.GetFileName(path)}'.");
        }

        string title = Path.GetFileNameWithoutExtension(path);
        var archiveContext = new SevenZipArchiveContext(archive, validEntries);

        var pages = new List<IComicPage>(validEntries.Count);
        for (int i = 0; i < validEntries.Count; i++)
        {
            string fileName = Path.GetFileName(validEntries[i].Key ?? string.Empty);
            pages.Add(new SevenZipComicPage(i, validEntries[i].Key!, fileName, archiveContext));
        }

        var comicBook = new ComicBook(title, path, ComicSourceType.SevenZipArchive, pages);
        return Task.FromResult(comicBook);
    }

    private sealed class SevenZipArchiveContext : IDisposable
    {
        private readonly IArchive _archive;
        private readonly Dictionary<string, IArchiveEntry> _entriesByKey;
        private readonly object _syncLock = new();
        private bool _disposed;

        public SevenZipArchiveContext(IArchive archive, IEnumerable<IArchiveEntry> entries)
        {
            _archive = archive;
            _entriesByKey = entries.ToDictionary(e => e.Key!, e => e, StringComparer.Ordinal);
        }

        public byte[] ReadEntryBytes(string entryKey)
        {
            lock (_syncLock)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(SevenZipArchiveContext));

                if (!_entriesByKey.TryGetValue(entryKey, out var entry))
                {
                    throw new FileNotFoundException($"Entry '{entryKey}' was not found in the 7Z archive.");
                }

                try
                {
                    using var stream = entry.OpenEntryStream();
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
                catch (CryptographicException)
                {
                    throw new InvalidOperationException($"Entry '{entryKey}' is password-protected and cannot be read.");
                }
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

    private sealed class SevenZipComicPage : IComicPage
    {
        private readonly string _entryKey;
        private readonly SevenZipArchiveContext _context;

        public int Index { get; }
        public string DisplayName { get; }

        public SevenZipComicPage(int index, string entryKey, string displayName, SevenZipArchiveContext context)
        {
            Index = index;
            _entryKey = entryKey;
            DisplayName = displayName;
            _context = context;
        }

        public Task<ComicPageData> GetPageDataAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] bytes = _context.ReadEntryBytes(_entryKey);
            return Task.FromResult(new ComicPageData(bytes, isRawBgra: false));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
