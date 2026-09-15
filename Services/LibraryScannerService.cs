using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Komik.Models;

namespace Komik.Services;

/// <summary>
/// Scans watched folders recursively for comic files and image folders,
/// generating cover thumbnails and persisting metadata into the SQLite database.
/// </summary>
public sealed class LibraryScannerService : ILibraryScannerService
{
    private readonly ILibraryRepository _repository;
    private readonly IComicLoaderService _loaderService;
    private readonly IThumbnailService _thumbnailService;
    private readonly FolderComicLoader _folderLoader = new();
    private readonly ZipComicLoader _zipLoader = new();
    private readonly RarComicLoader _rarLoader = new();
    private readonly SevenZipComicLoader _sevenZipLoader = new();
    private readonly PdfComicLoader _pdfLoader = new();

    public event EventHandler<ScanProgressEventArgs>? ProgressChanged;

    public bool IsScanning { get; private set; }

    // Paths the user removed from the library: automatic scans leave them out.
    private HashSet<string> _removedPaths = new(StringComparer.OrdinalIgnoreCase);

    public LibraryScannerService(
        ILibraryRepository repository,
        IComicLoaderService? loaderService = null,
        IThumbnailService? thumbnailService = null)
    {
        _repository = repository;
        _loaderService = loaderService ?? new ComicLoaderService();
        _thumbnailService = thumbnailService ?? new ThumbnailService();
    }

    public async Task ScanFolderAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath)) return;

        IsScanning = true;
        int discoveredCount = 0;

        try
        {
            // 1. Add/update watched folder record
            var watched = await _repository.AddWatchedFolderAsync(rootPath);
            _removedPaths = await _repository.GetRemovedComicPathsAsync();

            ReportProgress($"Scanning '{Path.GetFileName(rootPath)}'...", discoveredCount);

            // 2. Discover and index comics
            discoveredCount = await ScanDirectoryInternalAsync(rootPath, discoveredCount, cancellationToken);

            // 3. Update last scanned timestamp
            await _repository.UpdateWatchedFolderLastScannedAsync(watched.Id, DateTime.UtcNow);

            ReportProgress($"Scan complete. Found {discoveredCount} comic{(discoveredCount == 1 ? "" : "s")}.", discoveredCount, isCompleted: true);
        }
        finally
        {
            IsScanning = false;
        }
    }

    public async Task RescanAllAsync(CancellationToken cancellationToken = default)
    {
        IsScanning = true;
        int totalDiscovered = 0;

        try
        {
            var watchedFolders = await _repository.GetWatchedFoldersAsync();
            if (watchedFolders.Count == 0)
            {
                ReportProgress("No watched folders in library.", 0, isCompleted: true);
                return;
            }

            _removedPaths = await _repository.GetRemovedComicPathsAsync();

            // 1. Check existing comics for missing files
            var existingComics = await _repository.GetComicsAsync();
            foreach (var comic in existingComics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool exists = comic.Format == ComicSourceType.Folder
                    ? Directory.Exists(comic.FilePath)
                    : File.Exists(comic.FilePath);

                if (comic.IsMissing != !exists)
                {
                    await _repository.SetMissingStatusAsync(comic.Id, !exists);
                }
            }

            // 2. Scan each watched root
            foreach (var folder in watchedFolders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Directory.Exists(folder.Path))
                {
                    ReportProgress($"Folder offline: '{folder.Path}'", totalDiscovered);
                    continue;
                }

                ReportProgress($"Scanning '{folder.Path}'...", totalDiscovered);
                totalDiscovered = await ScanDirectoryInternalAsync(folder.Path, totalDiscovered, cancellationToken);
                await _repository.UpdateWatchedFolderLastScannedAsync(folder.Id, DateTime.UtcNow);
            }

            await ImportEmbeddedMetadataAsync(cancellationToken: cancellationToken);

            ReportProgress($"Rescan complete. {totalDiscovered} total comic{(totalDiscovered == 1 ? "" : "s")} indexed.", totalDiscovered, isCompleted: true);
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task<int> ScanDirectoryInternalAsync(string currentDirectory, int currentCount, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // 1. Check if the directory itself is a comic folder
        if (_folderLoader.CanLoad(currentDirectory))
        {
            await ProcessComicSourceAsync(currentDirectory, ComicSourceType.Folder, ct);
            currentCount++;
            ReportProgress($"Found {currentCount} comic{(currentCount == 1 ? "" : "s")}...", currentCount);
            return currentCount;
        }

        // 2. Check files in the current directory
        string[] files = Array.Empty<string>();
        try
        {
            files = Directory.GetFiles(currentDirectory);
        }
        catch (UnauthorizedAccessException) { /* Ignore restricted directories */ }
        catch (Exception) { /* Ignore I/O read errors */ }

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            if (_zipLoader.CanLoad(file))
            {
                await ProcessComicSourceAsync(file, ComicSourceType.ZipArchive, ct);
                currentCount++;
                ReportProgress($"Found {currentCount} comic{(currentCount == 1 ? "" : "s")}...", currentCount);
            }
            else if (_rarLoader.CanLoad(file))
            {
                await ProcessComicSourceAsync(file, ComicSourceType.RarArchive, ct);
                currentCount++;
                ReportProgress($"Found {currentCount} comic{(currentCount == 1 ? "" : "s")}...", currentCount);
            }
            else if (_sevenZipLoader.CanLoad(file))
            {
                await ProcessComicSourceAsync(file, ComicSourceType.SevenZipArchive, ct);
                currentCount++;
                ReportProgress($"Found {currentCount} comic{(currentCount == 1 ? "" : "s")}...", currentCount);
            }
            else if (_pdfLoader.CanLoad(file))
            {
                await ProcessComicSourceAsync(file, ComicSourceType.PdfDocument, ct);
                currentCount++;
                ReportProgress($"Found {currentCount} comic{(currentCount == 1 ? "" : "s")}...", currentCount);
            }
        }

        // 3. Recurse into subdirectories
        string[] subDirs = Array.Empty<string>();
        try
        {
            subDirs = Directory.GetDirectories(currentDirectory);
        }
        catch (UnauthorizedAccessException) { }
        catch (Exception) { }

        foreach (var dir in subDirs)
        {
            ct.ThrowIfCancellationRequested();
            currentCount = await ScanDirectoryInternalAsync(dir, currentCount, ct);
        }

        return currentCount;
    }

    private async Task ProcessComicSourceAsync(string path, ComicSourceType format, CancellationToken ct, bool includeRemoved = false)
    {
        if (!includeRemoved && _removedPaths.Contains(path)) return;

        // Check if already in repository
        var existing = await _repository.GetComicByPathAsync(path);
        if (existing != null)
        {
            if (existing.IsMissing)
            {
                await _repository.SetMissingStatusAsync(existing.Id, false);
            }
            return;
        }

        // Load metadata and generate thumbnail
        try
        {
            using var comic = await _loaderService.LoadComicAsync(path, ct);
            if (comic.PageCount == 0) return;

            string? thumbPath = null;
            try
            {
                var firstPageData = await comic.Pages[0].GetPageDataAsync(ct);
                thumbPath = await _thumbnailService.SaveThumbnailAsync(path, firstPageData);
            }
            catch
            {
                // Thumbnail generation failure shouldn't prevent comic indexing
            }

            long fileSize = 0;
            DateTime lastModified = DateTime.UtcNow;
            string? parentFolder = null;

            if (format == ComicSourceType.Folder)
            {
                var dirInfo = new DirectoryInfo(path);
                lastModified = dirInfo.LastWriteTimeUtc;
                parentFolder = dirInfo.Parent?.Name;
            }
            else
            {
                var fileInfo = new FileInfo(path);
                fileSize = fileInfo.Length;
                lastModified = fileInfo.LastWriteTimeUtc;
                parentFolder = fileInfo.Directory?.Name;
            }

            // ComicInfo.xml (e.g. from KomiK Downloader): title, series, credits and tags.
            var embedded = format == ComicSourceType.PdfDocument ? null : ComicInfoReader.TryRead(path);
            string title = embedded?.ResolveLibraryTitle(comic.Title) ?? comic.Title;

            var entity = new ComicEntity
            {
                FilePath = path,
                Title = title,
                Format = format,
                PageCount = comic.PageCount,
                ThumbnailPath = thumbPath,
                DateAdded = DateTime.UtcNow,
                LastModified = lastModified,
                ParentFolder = parentFolder,
                FileSize = fileSize,
                IsFavorite = false,
                IsMissing = false
            };

            long comicId = await _repository.InsertComicAsync(entity);
            if (embedded != null)
            {
                await ApplyEmbeddedInfoAsync(comicId, title, embedded);
            }
        }
        catch
        {
            // Skip invalid/unsupported/corrupted files during background scan
        }
    }

    /// <summary>
    /// Saves embedded metadata and tags for a comic. Metadata the user already has (typed in Comic
    /// Details or restored from a backup) is never replaced; tags are only ever added.
    /// </summary>
    private async Task ApplyEmbeddedInfoAsync(long comicId, string libraryTitle, EmbeddedComicInfo info)
    {
        if (info.HasMetadata && await _repository.GetMetadataForComicAsync(comicId) == null)
        {
            await _repository.SaveComicMetadataAsync(info.ToMetadata(comicId, libraryTitle));
        }

        foreach (var tag in info.Tags)
        {
            await _repository.AddTagToComicAsync(comicId, tag);
        }
    }

    /// <summary>
    /// One-time pass for libraries indexed before Komik read ComicInfo.xml: comics without any
    /// metadata get their embedded title, details and tags. Returns how many comics were updated.
    /// </summary>
    public async Task<int> ImportEmbeddedMetadataAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        if (!force && await _repository.GetSettingAsync(EmbeddedMetadataImportedKey) == "1") return 0;

        int updated = 0;
        var metadata = await _repository.GetAllComicMetadataAsync();
        foreach (var comic in await _repository.GetComicsAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (comic.IsMissing || comic.Format == ComicSourceType.PdfDocument || metadata.ContainsKey(comic.Id)) continue;

            var info = await Task.Run(() => ComicInfoReader.TryRead(comic.FilePath), cancellationToken);
            if (info == null) continue;

            string title = info.ResolveLibraryTitle(comic.Title);
            if (title != comic.Title)
            {
                await _repository.UpdateComicTitleAsync(comic.Id, title);
                comic.Title = title;
            }

            await ApplyEmbeddedInfoAsync(comic.Id, title, info);
            updated++;
        }

        await _repository.SetSettingAsync(EmbeddedMetadataImportedKey, "1");
        return updated;
    }

    private const string EmbeddedMetadataImportedKey = "EmbeddedComicInfoImported";

    public async Task<ComicEntity?> IndexSingleComicAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        ComicSourceType? format = null;
        if (_folderLoader.CanLoad(path)) format = ComicSourceType.Folder;
        else if (_zipLoader.CanLoad(path)) format = ComicSourceType.ZipArchive;
        else if (_rarLoader.CanLoad(path)) format = ComicSourceType.RarArchive;
        else if (_sevenZipLoader.CanLoad(path)) format = ComicSourceType.SevenZipArchive;
        else if (_pdfLoader.CanLoad(path)) format = ComicSourceType.PdfDocument;

        if (!format.HasValue) return null;

        // Picking a comic by hand always adds it, even one that was removed before.
        await ProcessComicSourceAsync(path, format.Value, cancellationToken, includeRemoved: true);
        return await _repository.GetComicByPathAsync(path);
    }

    /// <summary>Every comic file or image folder inside the given folders (nothing is indexed).</summary>
    public Task<List<(string Path, ComicSourceType Format)>> FindComicSourcesAsync(IEnumerable<string> roots, CancellationToken cancellationToken = default)
    {
        var rootList = roots.Where(Directory.Exists).ToList();
        return Task.Run(() =>
        {
            var found = new List<(string, ComicSourceType)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in rootList)
            {
                Collect(root, found, seen, cancellationToken);
            }
            return found;
        }, cancellationToken);
    }

    private void Collect(string directory, List<(string, ComicSourceType)> found, HashSet<string> seen, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (_folderLoader.CanLoad(directory))
        {
            if (seen.Add(directory)) found.Add((directory, ComicSourceType.Folder));
            return;
        }

        string[] files = Array.Empty<string>();
        try { files = Directory.GetFiles(directory); } catch { }
        foreach (var file in files)
        {
            ComicSourceType? format =
                _zipLoader.CanLoad(file) ? ComicSourceType.ZipArchive :
                _rarLoader.CanLoad(file) ? ComicSourceType.RarArchive :
                _sevenZipLoader.CanLoad(file) ? ComicSourceType.SevenZipArchive :
                _pdfLoader.CanLoad(file) ? ComicSourceType.PdfDocument : null;
            if (format.HasValue && seen.Add(file)) found.Add((file, format.Value));
        }

        string[] dirs = Array.Empty<string>();
        try { dirs = Directory.GetDirectories(directory); } catch { }
        foreach (var dir in dirs)
        {
            Collect(dir, found, seen, ct);
        }
    }

    /// <summary>Adds the chosen comics (including ones removed earlier) and reports progress.</summary>
    public async Task<int> IndexComicsAsync(IReadOnlyList<(string Path, ComicSourceType Format)> sources, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        int added = 0;
        IsScanning = true;
        try
        {
            for (int i = 0; i < sources.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (path, format) = sources[i];
                await ProcessComicSourceAsync(path, format, cancellationToken, includeRemoved: true);
                if (await _repository.GetComicByPathAsync(path) != null) added++;
                progress?.Report(i + 1);
            }
        }
        finally
        {
            IsScanning = false;
        }
        return added;
    }

    /// <summary>
    /// Quietly indexes new comic files or folders (for example a download that just finished in a watched
    /// folder). Comics already in the library or removed by the user are skipped. No progress is reported.
    /// </summary>
    public async Task<int> IndexNewSourcesAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        var removed = await _repository.GetRemovedComicPathsAsync();
        int added = 0;
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ComicSourceType? format = DetectFormat(path);
            if (!format.HasValue || removed.Contains(path)) continue;
            if (await _repository.GetComicByPathAsync(path) is { IsMissing: false }) continue;

            await ProcessComicSourceAsync(path, format.Value, cancellationToken, includeRemoved: true);
            if (await _repository.GetComicByPathAsync(path) != null) added++;
        }
        return added;
    }

    /// <summary>
    /// Picks up everything that changed in watched folders while Komik was closed: new comics are indexed
    /// (with their embedded metadata) and the one-time ComicInfo.xml import runs. No progress is reported.
    /// </summary>
    public async Task<int> SyncWatchedFoldersQuietlyAsync(CancellationToken cancellationToken = default)
    {
        var folders = await _repository.GetWatchedFoldersAsync();
        int added = 0;
        if (folders.Count > 0)
        {
            var known = new HashSet<string>((await _repository.GetComicsAsync()).Select(c => c.FilePath), StringComparer.OrdinalIgnoreCase);
            var sources = await FindComicSourcesAsync(folders.Select(f => f.Path), cancellationToken);
            added = await IndexNewSourcesAsync(sources.Where(s => !known.Contains(s.Path)).Select(s => s.Path), cancellationToken);
        }

        int imported = await ImportEmbeddedMetadataAsync(cancellationToken: cancellationToken);
        return added + imported;
    }

    private ComicSourceType? DetectFormat(string path)
    {
        if (_folderLoader.CanLoad(path)) return ComicSourceType.Folder;
        if (_zipLoader.CanLoad(path)) return ComicSourceType.ZipArchive;
        if (_rarLoader.CanLoad(path)) return ComicSourceType.RarArchive;
        if (_sevenZipLoader.CanLoad(path)) return ComicSourceType.SevenZipArchive;
        if (_pdfLoader.CanLoad(path)) return ComicSourceType.PdfDocument;
        return null;
    }

    private void ReportProgress(string message, int count, bool isCompleted = false)
    {
        ProgressChanged?.Invoke(this, new ScanProgressEventArgs(message, count, isCompleted));
    }
}
