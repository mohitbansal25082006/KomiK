namespace Komik.Models;

/// <summary>
/// Encapsulates page image bytes, distinguishing between encoded formats (JPEG, PNG, WebP)
/// and raw uncompressed BGRA buffers (e.g. from PDFium rendering).
/// </summary>
public sealed class ComicPageData
{
    public byte[] Data { get; }
    public bool IsRawBgra { get; }
    public int Width { get; }
    public int Height { get; }

    public ComicPageData(byte[] data, bool isRawBgra = false, int width = 0, int height = 0)
    {
        Data = data;
        IsRawBgra = isRawBgra;
        Width = width;
        Height = height;
    }
}
