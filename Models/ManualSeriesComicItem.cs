using CommunityToolkit.Mvvm.ComponentModel;

namespace Komik.Models;

public partial class ManualSeriesComicItem : ObservableObject
{
    public ComicEntity Comic { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isVisible = true;

    public ManualSeriesComicItem(ComicEntity comic, bool isSelected = false)
    {
        Comic = comic;
        _isSelected = isSelected;
        _isVisible = true;
    }
}
