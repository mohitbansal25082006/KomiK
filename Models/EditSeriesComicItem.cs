using CommunityToolkit.Mvvm.ComponentModel;

namespace Komik.Models;

/// <summary>One comic in the Edit Series screen: its place in the reading order and whether it is the cover.</summary>
public partial class EditSeriesComicItem : ObservableObject
{
    public EditSeriesComicItem(ComicEntity comic) => Comic = comic;

    public ComicEntity Comic { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PositionDisplay))]
    private int _position;

    [ObservableProperty]
    private bool _isCover;

    public string PositionDisplay => $"#{Position}";
}

/// <summary>A tag shown as a chip in the tag screens, with how many comics use it.</summary>
public partial class TagChipItem : ObservableObject
{
    public TagChipItem(string name, int count, bool isSelected = false)
    {
        _name = name;
        _count = count;
        _isSelected = isSelected;
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountDisplay))]
    [NotifyPropertyChangedFor(nameof(IsUnused))]
    private int _count;

    [ObservableProperty]
    private bool _isSelected;

    public bool IsUnused => Count == 0;
    public string CountDisplay => Count == 1 ? "1 comic" : $"{Count} comics";
}
