using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Komik.Models;

public partial class DuplicateComicGroup : ObservableObject
{
    [ObservableProperty]
    private string _groupTitle = string.Empty;

    [ObservableProperty]
    private string _matchReason = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ComicEntity> _copies = new();

    public int CopyCount => Copies.Count;
}
