using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Komik.Models;

/// <summary>
/// Represents a user bookmark within a specific comic issue.
/// </summary>
public sealed class BookmarkEntity : ObservableObject
{
    private long _id;
    private long _comicId;
    private int _pageIndex; // 0-based page index
    private string? _userNote;
    private DateTime _createdAt = DateTime.UtcNow;

    public long Id { get => _id; set => SetProperty(ref _id, value); }
    public long ComicId { get => _comicId; set => SetProperty(ref _comicId, value); }
    public int PageIndex { get => _pageIndex; set => SetProperty(ref _pageIndex, value); }
    public int PageNumber => PageIndex + 1; // 1-based page number for display
    public string? UserNote { get => _userNote; set => SetProperty(ref _userNote, value); }
    public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }

    public string DisplayTitle => $"Page {PageNumber}";
    public string DisplaySubtitle => string.IsNullOrWhiteSpace(UserNote) ? CreatedAtFormatted : UserNote;
    public string CreatedAtFormatted => CreatedAt.ToLocalTime().ToString("MMM dd, yyyy h:mm tt");
}
