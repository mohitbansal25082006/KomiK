namespace Komik.Models;

/// <summary>
/// Specifies the origin source of a comic book.
/// </summary>
public enum ComicSourceType
{
    Folder = 0,
    ZipArchive = 1,
    PdfDocument = 2,
    RarArchive = 3,
    SevenZipArchive = 4
}
