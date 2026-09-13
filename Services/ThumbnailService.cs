using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Komik.Models;

namespace Komik.Services;

/// <summary>
/// Service abstraction for caching and retrieving comic cover thumbnails.
/// </summary>
public interface IThumbnailService
{
    string ThumbnailDirectory { get; }
    string GetThumbnailPathForComic(string comicPath, string extension = ".png");
    bool ThumbnailExists(string comicPath);
    Task<string?> SaveThumbnailAsync(string comicPath, ComicPageData pageData);
    long GetCacheSizeBytes();
    void ClearCache();
}

/// <summary>
/// Default implementation of IThumbnailService, caching thumbnails in LocalAppData/Komik/Thumbnails.
/// </summary>
public sealed class ThumbnailService : IThumbnailService
{
    private readonly string _thumbnailDirectory;
    private static readonly string[] PossibleExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".webp" };

    public static string ResolveThumbnailDirectory()
    {
        string durableThumbDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".komik", "Thumbnails");
        string localThumbDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Komik", "Thumbnails");

        if (Directory.Exists(durableThumbDir))
        {
            return durableThumbDir;
        }

        if (Directory.Exists(localThumbDir))
        {
            try
            {
                Directory.CreateDirectory(durableThumbDir);
                foreach (var file in Directory.GetFiles(localThumbDir))
                {
                    try { File.Copy(file, Path.Combine(durableThumbDir, Path.GetFileName(file)), overwrite: false); } catch { }
                }
                return durableThumbDir;
            }
            catch
            {
                return localThumbDir;
            }
        }

        try { Directory.CreateDirectory(durableThumbDir); } catch { }
        return durableThumbDir;
    }

    public static string DefaultThumbnailDirectory => ResolveThumbnailDirectory();

    public ThumbnailService(string? thumbnailDirectory = null)
    {
        _thumbnailDirectory = thumbnailDirectory ?? DefaultThumbnailDirectory;
        if (!Directory.Exists(_thumbnailDirectory))
        {
            Directory.CreateDirectory(_thumbnailDirectory);
        }
    }

    public string GetThumbnailPathForComic(string comicPath, string extension = ".png")
    {
        string hash = ComputeHash(comicPath);
        return Path.Combine(_thumbnailDirectory, $"thumb_{hash}{extension}");
    }

    public string ThumbnailDirectory => _thumbnailDirectory;

    public bool ThumbnailExists(string comicPath)
    {
        string hash = ComputeHash(comicPath);
        foreach (var ext in PossibleExtensions)
        {
            string p = Path.Combine(_thumbnailDirectory, $"thumb_{hash}{ext}");
            if (File.Exists(p) && new FileInfo(p).Length > 0)
            {
                return true;
            }
        }
        return false;
    }

    public long GetCacheSizeBytes()
    {
        if (!Directory.Exists(_thumbnailDirectory)) return 0;
        long total = 0;
        try
        {
            foreach (var file in Directory.GetFiles(_thumbnailDirectory))
            {
                try { total += new FileInfo(file).Length; } catch { }
            }
        }
        catch { }
        return total;
    }

    public void ClearCache()
    {
        if (!Directory.Exists(_thumbnailDirectory)) return;
        try
        {
            foreach (var file in Directory.GetFiles(_thumbnailDirectory))
            {
                try { File.Delete(file); } catch { }
            }
        }
        catch { }
    }

    public async Task<string?> SaveThumbnailAsync(string comicPath, ComicPageData pageData)
    {
        try
        {
            if (pageData.IsRawBgra)
            {
                // Top-down 32-bit BGRA BMP format
                string targetPath = GetThumbnailPathForComic(comicPath, ".bmp");
                byte[] bmpBytes = CreateBgraBmp(pageData.Data, pageData.Width, pageData.Height);
                await File.WriteAllBytesAsync(targetPath, bmpBytes);
                return targetPath;
            }
            else
            {
                string ext = ".png";
                if (pageData.Data.Length >= 3 && pageData.Data[0] == 0xFF && pageData.Data[1] == 0xD8)
                {
                    ext = ".jpg";
                }
                else if (pageData.Data.Length >= 4 && pageData.Data[0] == 'R' && pageData.Data[1] == 'I' && pageData.Data[2] == 'F' && pageData.Data[3] == 'F')
                {
                    ext = ".webp";
                }

                string targetPath = GetThumbnailPathForComic(comicPath, ext);
                await File.WriteAllBytesAsync(targetPath, pageData.Data);
                return targetPath;
            }
        }
        catch
        {
            return null;
        }
    }

    private static byte[] CreateBgraBmp(byte[] bgraData, int width, int height)
    {
        int headerSize = 54;
        int totalSize = headerSize + bgraData.Length;
        byte[] bmp = new byte[totalSize];

        // BITMAPFILEHEADER (14 bytes)
        bmp[0] = 0x42; // 'B'
        bmp[1] = 0x4D; // 'M'
        BitConverter.GetBytes(totalSize).CopyTo(bmp, 2);
        BitConverter.GetBytes(headerSize).CopyTo(bmp, 10);

        // BITMAPINFOHEADER (40 bytes)
        BitConverter.GetBytes(40).CopyTo(bmp, 14); // biSize
        BitConverter.GetBytes(width).CopyTo(bmp, 18); // biWidth
        BitConverter.GetBytes(-height).CopyTo(bmp, 22); // biHeight (negative for top-down)
        BitConverter.GetBytes((short)1).CopyTo(bmp, 26); // biPlanes
        BitConverter.GetBytes((short)32).CopyTo(bmp, 28); // biBitCount
        BitConverter.GetBytes(0).CopyTo(bmp, 30); // biCompression (BI_RGB)
        BitConverter.GetBytes(bgraData.Length).CopyTo(bmp, 34); // biSizeImage

        Buffer.BlockCopy(bgraData, 0, bmp, headerSize, bgraData.Length);
        return bmp;
    }

    private static string ComputeHash(string input)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input.ToLowerInvariant()));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
