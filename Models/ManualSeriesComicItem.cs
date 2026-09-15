using CommunityToolkit.Mvvm.ComponentModel;
using Komik.Helpers;

namespace Komik.Models;

public partial class ManualSeriesComicItem : ObservableObject
{
    public ComicEntity Comic { get; }

    /// <summary>Parsed series, number and creators, used by smart picking and the card captions.</summary>
    public ComicIdentity Identity { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isVisible = true;

    public ManualSeriesComicItem(ComicEntity comic, bool isSelected = false)
        : this(comic, ComicIdentityParser.Parse(comic), isSelected)
    {
    }

    public ManualSeriesComicItem(ComicEntity comic, ComicIdentity identity, bool isSelected = false)
    {
        Comic = comic;
        Identity = identity;
        _isSelected = isSelected;
        _isVisible = true;
    }

    public string Caption
    {
        get
        {
            string creators = Identity.TitleCreators.Count > 0 ? string.Join(", ", Identity.TitleCreators) : string.Empty;
            string number = Identity.NumberLabel;
            if (creators.Length > 0 && number.Length > 0) return $"{number} · {creators}";
            return creators.Length > 0 ? creators : number.Length > 0 ? number : Comic.PageCountFormatted;
        }
    }
}
