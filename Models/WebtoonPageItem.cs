using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace Komik.Models;

public partial class WebtoonPageItem : ObservableObject
{
    public int PageIndex { get; set; }

    [ObservableProperty]
    private ImageSource? _image;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private double _displayWidth = 800;

    [ObservableProperty]
    private double _displayHeight = 1200;

    public string PageLabel => $"Page {PageIndex + 1}";
}
