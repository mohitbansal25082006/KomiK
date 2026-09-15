using System;
using System.Collections.Generic;

namespace Komik.Models;

public sealed class BackupComicRecord
{
    public string FilePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int LastReadPage { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsFavorite { get; set; }
    public string? LastReadAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> Collections { get; set; } = new();
    public ComicMetadataEntity? Metadata { get; set; }
}

public sealed class BackupBookmarkRecord
{
    public string ComicFilePath { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public string? UserNote { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public sealed class LibraryBackupData
{
    public string AppVersion { get; set; } = "1.2.0";
    public string ExportDateUtc { get; set; } = DateTime.UtcNow.ToString("o");
    public List<string> WatchedFolders { get; set; } = new();
    public List<BackupComicRecord> Comics { get; set; } = new();
    public List<BackupBookmarkRecord> Bookmarks { get; set; } = new();
    public Dictionary<string, string> Settings { get; set; } = new();
}
