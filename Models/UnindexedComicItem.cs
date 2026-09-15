using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Komik.Models;

/// <summary>A comic found in a watched folder that isn't in the library (never added, or removed earlier).</summary>
public partial class UnindexedComicItem : ObservableObject
{
    public UnindexedComicItem(string path, ComicSourceType format, bool wasRemoved, string? thumbnailPath)
    {
        Path = path;
        Format = format;
        WasRemoved = wasRemoved;
        ThumbnailPath = thumbnailPath;
        try
        {
            if (format == ComicSourceType.Folder)
            {
                var dir = new DirectoryInfo(path);
                FileName = dir.Name;
                FolderName = dir.Parent?.FullName ?? string.Empty;
                Modified = dir.LastWriteTime;
            }
            else
            {
                var file = new FileInfo(path);
                FileName = System.IO.Path.GetFileNameWithoutExtension(path);
                FolderName = file.DirectoryName ?? string.Empty;
                SizeBytes = file.Length;
                Modified = file.LastWriteTime;
            }
        }
        catch
        {
            FileName = System.IO.Path.GetFileName(path);
        }
    }

    public string Path { get; }
    public ComicSourceType Format { get; }
    public bool WasRemoved { get; }
    public bool IsNew => !WasRemoved;
    public string? ThumbnailPath { get; }
    public string FileName { get; } = string.Empty;
    public string FolderName { get; } = string.Empty;
    public long SizeBytes { get; }
    public DateTime Modified { get; }

    [ObservableProperty]
    private bool _isSelected = true;

    public string FormatBadge => Format switch
    {
        ComicSourceType.Folder => "FOLDER",
        ComicSourceType.PdfDocument => "PDF",
        ComicSourceType.RarArchive => System.IO.Path.GetExtension(Path).TrimStart('.').ToUpperInvariant(),
        ComicSourceType.SevenZipArchive => System.IO.Path.GetExtension(Path).TrimStart('.').ToUpperInvariant(),
        _ => System.IO.Path.GetExtension(Path).TrimStart('.').ToUpperInvariant()
    };

    public string StateLabel => WasRemoved ? "REMOVED" : "NEW";
    public string SizeDisplay => SizeBytes <= 0 ? "Image folder" : DuplicateComicGroup.FormatBytes(SizeBytes);
}
