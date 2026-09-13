using System;
using System.Collections.Generic;
using System.IO;
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

    private async Task ProcessComicSourceAsync(string path, ComicSourceType format, CancellationToken ct)
    {
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

            var entity = new ComicEntity
            {
                FilePath = path,
                Title = comic.Title,
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

            await _repository.InsertComicAsync(entity);
        }
        catch
        {
            // Skip invalid/unsupported/corrupted files during background scan
        }
    }

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

        await ProcessComicSourceAsync(path, format.Value, cancellationToken);
        return await _repository.GetComicByPathAsync(path);
    }

    private void ReportProgress(string message, int count, bool isCompleted = false)
    {
        ProgressChanged?.Invoke(this, new ScanProgressEventArgs(message, count, isCompleted));
    }
}
