using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Komik.Models;

/// <summary>
/// Represents a comic entry stored in the local SQLite library database.
/// </summary>
public sealed class ComicEntity : ObservableObject
{
    private long _id;
    private string _filePath = string.Empty;
    private string _title = string.Empty;
    private ComicSourceType _format;
    private int _pageCount;
    private string? _thumbnailPath;
    private DateTime _dateAdded = DateTime.UtcNow;
    private DateTime _lastModified = DateTime.UtcNow;
    private string? _parentFolder;
    private long _fileSize;
    private bool _isFavorite;
    private bool _isMissing;
    private int _lastReadPage;
    private DateTime? _lastReadAt;
    private bool _isCompleted;
    private List<string> _tags = new();
    private List<string> _collections = new();

    public long Id { get => _id; set => SetProperty(ref _id, value); }
    public string FilePath { get => _filePath; set => SetProperty(ref _filePath, value); }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public ComicSourceType Format { get => _format; set => SetProperty(ref _format, value); }
    public int PageCount { get => _pageCount; set => SetProperty(ref _pageCount, value); }

    public string? ThumbnailPath
    {
        get => _thumbnailPath;
        set => SetProperty(ref _thumbnailPath, value);
    }

    public DateTime DateAdded { get => _dateAdded; set => SetProperty(ref _dateAdded, value); }
    public DateTime LastModified { get => _lastModified; set => SetProperty(ref _lastModified, value); }
    public string? ParentFolder { get => _parentFolder; set => SetProperty(ref _parentFolder, value); }
    public long FileSize { get => _fileSize; set => SetProperty(ref _fileSize, value); }

    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (SetProperty(ref _isFavorite, value))
            {
                OnPropertyChanged(nameof(FavoriteGlyph));
                OnPropertyChanged(nameof(FavoriteToolTip));
            }
        }
    }

    public bool IsMissing
    {
        get => _isMissing;
        set => SetProperty(ref _isMissing, value);
    }

    public int LastReadPage
    {
        get => _lastReadPage;
        set
        {
            if (SetProperty(ref _lastReadPage, value))
            {
                NotifyProgressChanged();
            }
        }
    }

    public DateTime? LastReadAt { get => _lastReadAt; set => SetProperty(ref _lastReadAt, value); }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (SetProperty(ref _isCompleted, value))
            {
                NotifyProgressChanged();
            }
        }
    }

    private void NotifyProgressChanged()
    {
        OnPropertyChanged(nameof(ReadingProgressPercentage));
        OnPropertyChanged(nameof(ProgressBarValue));
        OnPropertyChanged(nameof(IsInProgress));
        OnPropertyChanged(nameof(IsUnread));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ProgressPillText));
    }

    public List<string> Tags { get => _tags; set => SetProperty(ref _tags, value); }
    public List<string> Collections { get => _collections; set => SetProperty(ref _collections, value); }

    #region UI Helper Properties

    public string FormatBadge
    {
        get
        {
            string ext = System.IO.Path.GetExtension(FilePath).ToLowerInvariant();
            return ext switch
            {
                ".cbz" => "CBZ",
                ".zip" => "ZIP",
                ".cbr" => "CBR",
                ".rar" => "RAR",
                ".cb7" => "CB7",
                ".7z" => "7Z",
                ".pdf" => "PDF",
                _ => Format switch
                {
                    ComicSourceType.ZipArchive => "CBZ",
                    ComicSourceType.PdfDocument => "PDF",
                    ComicSourceType.Folder => "FOLDER",
                    ComicSourceType.RarArchive => "CBR",
                    ComicSourceType.SevenZipArchive => "CB7",
                    _ => "COMIC"
                }
            };
        }
    }

    public string PageCountFormatted => PageCount == 1 ? "1 page" : $"{PageCount} pages";

    public double ReadingProgressPercentage
    {
        get
        {
            if (IsCompleted) return 100.0;
            if (PageCount <= 0 || LastReadPage <= 0) return 0.0;
            return Math.Clamp(Math.Round(((double)(LastReadPage + 1) / PageCount) * 100), 0, 100);
        }
    }

    public double ProgressBarValue => ReadingProgressPercentage;

    public bool IsInProgress => !IsCompleted && LastReadPage > 0;

    public bool IsUnread => !IsCompleted && LastReadPage <= 0;

    public string ProgressPillText => IsCompleted ? "Completed" : $"{ReadingProgressPercentage:F0}%";

    public string ProgressText => IsCompleted
        ? "Completed"
        : (IsInProgress ? $"p. {LastReadPage + 1}/{PageCount} ({ReadingProgressPercentage:F0}%)" : "Unread");

    public string FileSizeFormatted
    {
        get
        {
            if (FileSize <= 0) return string.Empty;
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024.0 * 1024.0):F1} MB";
            return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }

    public string DateAddedFormatted => DateAdded.ToLocalTime().ToString("MMM dd, yyyy");

    public string FavoriteGlyph => IsFavorite ? "\uEB52" : "\uEB51"; // Heart filled vs outline

    public string FavoriteToolTip => IsFavorite ? "Remove from Favorites" : "Add to Favorites";

    #endregion
}
