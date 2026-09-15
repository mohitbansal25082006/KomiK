using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Komik.Helpers;
using Komik.Models;

namespace Komik.Services;

/// <summary>
/// Event arguments providing progress details during comic format conversion.
/// </summary>
public sealed class ConversionProgressEventArgs : EventArgs
{
    public string StatusMessage { get; }
    public int CurrentPage { get; }
    public int TotalPages { get; }
    public double Percentage => TotalPages > 0 ? ((double)CurrentPage / TotalPages) * 100.0 : 0.0;
    public bool IsCompleted { get; }

    public ConversionProgressEventArgs(string statusMessage, int currentPage, int totalPages, bool isCompleted = false)
    {
        StatusMessage = statusMessage;
        CurrentPage = currentPage;
        TotalPages = totalPages;
        IsCompleted = isCompleted;
    }
}

/// <summary>
/// Contract for converting folders, RAR/CBR, 7Z/CB7, and PDF comics into standard CBZ archives.
/// </summary>
public interface IFormatConversionService
{
    event EventHandler<ConversionProgressEventArgs>? ProgressChanged;
    bool IsConverting { get; }
    Task<string> ConvertToCbzAsync(string sourcePath, string? destinationPath = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implements format conversion to standard CBZ (ZIP archive with .cbz extension).
/// Preserves natural page order, copies image streams directly without quality loss,
/// and provides cancellation and progress reporting.
/// </summary>
public sealed class FormatConversionService : IFormatConversionService
{
    private readonly IComicLoaderService _loaderService;

    public event EventHandler<ConversionProgressEventArgs>? ProgressChanged;

    public bool IsConverting { get; private set; }

    public FormatConversionService(IComicLoaderService? loaderService = null)
    {
        _loaderService = loaderService ?? new ComicLoaderService();
    }

    public async Task<string> ConvertToCbzAsync(
        string sourcePath,
        string? destinationPath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path cannot be null or empty.", nameof(sourcePath));
        }

        bool isDirectory = Directory.Exists(sourcePath);
        bool isFile = File.Exists(sourcePath);

        if (!isDirectory && !isFile)
        {
            throw new FileNotFoundException($"Source comic path '{sourcePath}' was not found.");
        }

        // Determine destination path if not provided
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            destinationPath = GenerateDefaultDestinationPath(sourcePath, isDirectory);
        }

        string destDir = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException($"Cannot determine destination directory for '{destinationPath}'.");

        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        IsConverting = true;
        string? createdFile = null;

        try
        {
            ReportProgress("Loading comic pages...", 0, 1);

            // Load source comic using the established IComicLoaderService
            using var comic = await _loaderService.LoadComicAsync(sourcePath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (comic.PageCount == 0)
            {
                throw new InvalidOperationException($"No supported comic pages found to convert in '{Path.GetFileName(sourcePath)}'.");
            }

            int totalPages = comic.PageCount;
            ReportProgress($"Preparing CBZ archive for {totalPages} pages...", 0, totalPages);

            // Create target CBZ stream and ZIP archive
            var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            createdFile = destinationPath;

            using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: false))
            {
                for (int i = 0; i < totalPages; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var page = comic.Pages[i];
                    ReportProgress($"Converting page {i + 1} of {totalPages} ({page.DisplayName})...", i, totalPages);

                    var pageData = await page.GetPageDataAsync(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    // Determine image extension & bytes
                    string ext = GetImageExtension(pageData, page.DisplayName);
                    byte[] imageBytes = pageData.IsRawBgra
                        ? CreateBgraBmp(pageData.Data, pageData.Width, pageData.Height)
                        : pageData.Data;

                    // Pad page index for deterministic natural alphabetical ordering
                    string sanitizedName = SanitizeEntryName(page.DisplayName);
                    string entryName = $"{i + 1:D4}_{sanitizedName}";
                    if (!entryName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        entryName = Path.ChangeExtension(entryName, ext);
                    }

                    var entry = zipArchive.CreateEntry(entryName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(imageBytes, 0, imageBytes.Length, cancellationToken);
                }

                // Keep the comic's embedded details and tags in the converted CBZ.
                byte[]? comicInfo = ComicInfoReader.TryReadRaw(sourcePath);
                if (comicInfo != null)
                {
                    var infoEntry = zipArchive.CreateEntry(ComicInfoReader.FileName, CompressionLevel.Optimal);
                    using var infoStream = infoEntry.Open();
                    await infoStream.WriteAsync(comicInfo, 0, comicInfo.Length, cancellationToken);
                }
            }

            ReportProgress($"Successfully converted to '{Path.GetFileName(destinationPath)}'.", totalPages, totalPages, isCompleted: true);
            return destinationPath;
        }
        catch
        {
            // If conversion fails or is cancelled, clean up partial file
            if (createdFile != null && File.Exists(createdFile))
            {
                try { File.Delete(createdFile); } catch { }
            }
            throw;
        }
        finally
        {
            IsConverting = false;
        }
    }

    private void ReportProgress(string message, int current, int total, bool isCompleted = false)
    {
        ProgressChanged?.Invoke(this, new ConversionProgressEventArgs(message, current, total, isCompleted));
    }

    public static string GenerateDefaultDestinationPath(string sourcePath, bool isDirectory = false)
    {
        string dir;
        string baseName;

        if (isDirectory || Directory.Exists(sourcePath))
        {
            var dirInfo = new DirectoryInfo(sourcePath);
            dir = dirInfo.Parent?.FullName ?? dirInfo.FullName;
            baseName = dirInfo.Name;
        }
        else
        {
            dir = Path.GetDirectoryName(sourcePath) ?? string.Empty;
            baseName = Path.GetFileNameWithoutExtension(sourcePath);
        }

        string target = Path.Combine(dir, $"{baseName}.cbz");
        if (!File.Exists(target) && !string.Equals(sourcePath, target, StringComparison.OrdinalIgnoreCase))
        {
            return target;
        }

        int counter = 1;
        while (true)
        {
            target = Path.Combine(dir, $"{baseName} ({counter}).cbz");
            if (!File.Exists(target))
            {
                return target;
            }
            counter++;
        }
    }

    private static string GetImageExtension(ComicPageData data, string originalName)
    {
        if (data.IsRawBgra) return ".bmp";

        // Check magic bytes
        if (data.Data.Length >= 3 && data.Data[0] == 0xFF && data.Data[1] == 0xD8) return ".jpg";
        if (data.Data.Length >= 8 && data.Data[0] == 0x89 && data.Data[1] == 0x50 && data.Data[2] == 0x4E && data.Data[3] == 0x47) return ".png";
        if (data.Data.Length >= 4 && data.Data[0] == 'R' && data.Data[1] == 'I' && data.Data[2] == 'F' && data.Data[3] == 'F') return ".webp";
        if (data.Data.Length >= 2 && data.Data[0] == 'B' && data.Data[1] == 'M') return ".bmp";

        string ext = Path.GetExtension(originalName);
        return string.IsNullOrEmpty(ext) ? ".png" : ext;
    }

    private static string SanitizeEntryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "page.png";
        string clean = Path.GetFileName(name);
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            clean = clean.Replace(c, '_');
        }
        return clean;
    }

    private static byte[] CreateBgraBmp(byte[] bgraData, int width, int height)
    {
        int headerSize = 54;
        int totalSize = headerSize + bgraData.Length;
        byte[] bmp = new byte[totalSize];

        // BMP Header: 'BM'
        bmp[0] = 0x42;
        bmp[1] = 0x4D;

        BitConverter.GetBytes(totalSize).CopyTo(bmp, 2);
        BitConverter.GetBytes(headerSize).CopyTo(bmp, 10);

        // DIB Header (BITMAPINFOHEADER - 40 bytes)
        BitConverter.GetBytes(40).CopyTo(bmp, 14);
        BitConverter.GetBytes(width).CopyTo(bmp, 18);
        BitConverter.GetBytes(-height).CopyTo(bmp, 22); // Top-down
        BitConverter.GetBytes((short)1).CopyTo(bmp, 26);
        BitConverter.GetBytes((short)32).CopyTo(bmp, 28);
        BitConverter.GetBytes(0).CopyTo(bmp, 30); // BI_RGB
        BitConverter.GetBytes(bgraData.Length).CopyTo(bmp, 34);

        Buffer.BlockCopy(bgraData, 0, bmp, headerSize, bgraData.Length);
        return bmp;
    }
}
