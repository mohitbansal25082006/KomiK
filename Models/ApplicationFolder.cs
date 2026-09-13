using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Komik.Models;

/// <summary>
/// Represents a virtual in-app folder/collection for organizing comic books.
/// This exists strictly inside the application database and does not modify disk folders.
/// </summary>
public sealed class ApplicationFolder : ObservableObject
{
    public long Id { get; set; }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private int _comicCount;
    public int ComicCount
    {
        get => _comicCount;
        set
        {
            if (SetProperty(ref _comicCount, value))
            {
                OnPropertyChanged(nameof(FormattedCount));
            }
        }
    }

    public string FormattedCount => $"{ComicCount} comic{(ComicCount == 1 ? "" : "s")}";

    private string? _coverThumbnailPath;
    public string? CoverThumbnailPath
    {
        get => _coverThumbnailPath;
        set => SetProperty(ref _coverThumbnailPath, value);
    }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
