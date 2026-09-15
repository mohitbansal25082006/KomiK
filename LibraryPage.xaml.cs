using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Komik.Models;
using Komik.Services;
using Komik.ViewModels;

namespace Komik;

/// <summary>
/// Page displaying the comic library grid/list, search, sorting, and tag/collection filters.
/// </summary>
public sealed partial class LibraryPage : Page
{
    public LibraryViewModel ViewModel { get; } = new();

    private static Brush GetThemeBrush(string key, Windows.UI.Color fallback)
    {
        try
        {
            if (Application.Current?.Resources?.TryGetValue(key, out var res) == true && res is Brush b)
            {
                return b;
            }
        }
        catch { }
        return new SolidColorBrush(fallback);
    }

    private static readonly Windows.UI.Color FallbackControlSecondary = Windows.UI.Color.FromArgb(40, 255, 255, 255);
    private static readonly Windows.UI.Color FallbackControlDefault = Windows.UI.Color.FromArgb(15, 255, 255, 255);
    private static readonly Windows.UI.Color FallbackTextSecondary = Windows.UI.Color.FromArgb(180, 255, 255, 255);
    private static readonly Windows.UI.Color FallbackTextTertiary = Windows.UI.Color.FromArgb(110, 255, 255, 255);
    private static readonly Windows.UI.Color FallbackCardSecondary = Windows.UI.Color.FromArgb(20, 255, 255, 255);

    private static readonly Brush ChipIdle = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));

    private static Brush Chip(byte r, byte g, byte b) => new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));

    private static readonly Brush ChipInk = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x0B, 0x0B, 0x12));

    private Brush ChipText(bool active) => active ? ChipInk : GetPageForeground();

    private Brush GetPageForeground() => ActualTheme == ElementTheme.Light
        ? ChipInk
        : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0xF2, 0xF2, 0xF7));

    private bool IsFilterAllActive => !ViewModel.FilterFavoritesOnly && !ViewModel.FilterInProgressOnly && !ViewModel.FilterUnreadOnly && !ViewModel.FilterCompletedOnly && string.IsNullOrEmpty(ViewModel.SelectedTag);

    public Brush FilterAllForeground => ChipText(IsFilterAllActive);
    public Brush FilterFavoritesForeground => ViewModel.FilterFavoritesOnly ? new SolidColorBrush(Microsoft.UI.Colors.White) : ChipText(false);
    public Brush FilterContinueReadingForeground => ChipText(ViewModel.FilterInProgressOnly);
    public Brush FilterCompletedForeground => ChipText(ViewModel.FilterCompletedOnly);
    public Brush FilterUnreadForeground => ChipText(ViewModel.FilterUnreadOnly);

    public Brush FilterAllBackground => (!ViewModel.FilterFavoritesOnly && !ViewModel.FilterInProgressOnly && !ViewModel.FilterUnreadOnly && !ViewModel.FilterCompletedOnly && string.IsNullOrEmpty(ViewModel.SelectedTag))
        ? Chip(0xFF, 0xD7, 0x00) : ChipIdle;

    public Brush FilterFavoritesBackground => ViewModel.FilterFavoritesOnly ? Chip(0xFF, 0x1F, 0x6D) : ChipIdle;

    public Brush FilterContinueReadingBackground => ViewModel.FilterInProgressOnly ? Chip(0x00, 0xC2, 0xFF) : ChipIdle;

    public Brush FilterCompletedBackground => ViewModel.FilterCompletedOnly ? Chip(0x2E, 0xE5, 0x9D) : ChipIdle;

    public Brush FilterUnreadBackground => ViewModel.FilterUnreadOnly ? Chip(0xFF, 0x9F, 0x1C) : ChipIdle;

    public Brush FilterSeriesBackground => ViewModel.IsSeriesView ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0xFF, 0xD7, 0x00)) : ChipIdle;

    public Brush FilterSeriesForeground => ViewModel.IsSeriesView
        ? new SolidColorBrush(Microsoft.UI.Colors.Black)
        : GetThemeBrush("TextFillColorPrimaryBrush", Microsoft.UI.Colors.White);

    public Brush FilterSeriesIconForeground => ViewModel.IsSeriesView
        ? new SolidColorBrush(Microsoft.UI.Colors.Black)
        : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x00, 0xC2, 0xFF));

    public Brush GridModeBackground => ViewModel.IsGridView ? Chip(0xFF, 0xD7, 0x00) : ChipIdle;

    public Brush ListModeBackground => ViewModel.IsListView ? Chip(0xFF, 0xD7, 0x00) : ChipIdle;

    public LibraryPage()
    {
        InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Required;
        ActualThemeChanged += (_, _) => { UpdateViewModeUi(); Bindings.Update(); };

        ViewModel.ComicSelectedForReading += OnComicSelectedForReading;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    /// <summary>Set by the reader's "Library" button: come back to the plain comics grid instead of where you were.</summary>
    public static bool ReturnToLibraryHome { get; set; }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance?.UpdateTitle("Library");
        AttachHeroToTitleBar();

        if (ReturnToLibraryHome)
        {
            ReturnToLibraryHome = false;
            if (ViewModel.IsSeriesDetailOpen) ViewModel.CloseSeriesDetail();
            if (ViewModel.IsSeriesView) ViewModel.CloseSeriesView();
        }

        // Coming back from the reader keeps the series view, section and open series exactly as they were.
        await ViewModel.InitializeAsync(refreshOnly: e.NavigationMode == NavigationMode.Back);
        UpdateFlyoutMenus();
        UpdateViewModeUi();
        Bindings.Update();
        ApplyResponsiveLayout(ActualWidth);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        MainWindow.Instance?.SetTitleBarContent(null);
    }

    private void AttachHeroToTitleBar()
    {
        if (MainWindow.Instance is not { } window) return;
        TopBarStack.Children.Remove(LibraryHero);
        window.SetTitleBarContent(LibraryHero, HeroChipComics, HeroChipReading, HeroChipFinished, HeroChipSeries);
    }

    private void LibraryPage_SizeChanged(object sender, SizeChangedEventArgs e) => ApplyResponsiveLayout(e.NewSize.Width);

    /// <summary>Trims the title-bar banner on narrow windows so it never collides with the caption buttons.</summary>
    private void ApplyResponsiveLayout(double width)
    {
        if (width <= 0) return;
        HeroTagline.Visibility = width >= 1180 ? Visibility.Visible : Visibility.Collapsed;
        var labels = width >= 900 ? Visibility.Visible : Visibility.Collapsed;
        HeroChipComicsLabel.Visibility = labels;
        HeroChipReadingLabel.Visibility = labels;
        HeroChipFinishedLabel.Visibility = labels;
        HeroChipSeriesLabel.Visibility = labels;
        HeroChipFinished.Visibility = width >= 640 ? Visibility.Visible : Visibility.Collapsed;
        HeroChipReading.Visibility = width >= 560 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateViewModeUi()
    {
        bool hasComics = ViewModel.HasComics;

        if (EmptyLibraryContainer != null)
        {
            EmptyLibraryContainer.Visibility = ViewModel.ShowEmptyLibrary ? Visibility.Visible : Visibility.Collapsed;
        }
        if (EmptyFilterContainer != null)
        {
            EmptyFilterContainer.Visibility = ViewModel.ShowEmptyFilter ? Visibility.Visible : Visibility.Collapsed;
        }
        if (SeriesGridView != null)
        {
            SeriesGridView.Visibility = ViewModel.ShowSeriesGrid ? Visibility.Visible : Visibility.Collapsed;
        }
        if (ComicsGridView != null)
        {
            ComicsGridView.Visibility = ViewModel.ShowComicsGrid ? Visibility.Visible : Visibility.Collapsed;
        }
        if (ComicsListView != null)
        {
            ComicsListView.Visibility = ViewModel.ShowComicsList ? Visibility.Visible : Visibility.Collapsed;
        }
        if (GridModeButton != null)
        {
            GridModeButton.Background = GridModeBackground;
        }
        if (ListModeButton != null)
        {
            ListModeButton.Background = ListModeBackground;
        }
    }

    private void ShowAllComics_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = string.Empty;
        ViewModel.FilterAll();
    }

    private void EmptyAddFolder_Click(object sender, RoutedEventArgs e)
    {
        // AddFolder is bound to ViewModel.AddFolderCommand
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.FilterFavoritesOnly) ||
            e.PropertyName == nameof(ViewModel.FilterInProgressOnly) ||
            e.PropertyName == nameof(ViewModel.FilterUnreadOnly) ||
            e.PropertyName == nameof(ViewModel.FilterCompletedOnly) ||
            e.PropertyName == nameof(ViewModel.CompletedCount) ||
            e.PropertyName == nameof(ViewModel.SelectedTag) ||
            e.PropertyName == nameof(ViewModel.IsGridView) ||
            e.PropertyName == nameof(ViewModel.IsListView) ||
            e.PropertyName == nameof(ViewModel.IsSeriesView) ||
            e.PropertyName == nameof(ViewModel.HasComics) ||
            e.PropertyName == nameof(ViewModel.HasNoComics) ||
            e.PropertyName == nameof(ViewModel.ShowEmptyLibrary) ||
            e.PropertyName == nameof(ViewModel.ShowEmptyFilter))
        {
            UpdateActiveFilterUi();
            UpdateViewModeUi();
            Bindings.Update();
        }
        else if (e.PropertyName == nameof(ViewModel.AvailableTags))
        {
            UpdateFlyoutMenus();
            UpdateViewModeUi();
            Bindings.Update();
        }
    }

    private void OnComicSelectedForReading(string filePath)
    {
        MainPage.ReturnLabel = ViewModel.IsSeriesDetailOpen && ViewModel.SelectedSeriesGroup != null
            ? ViewModel.SelectedSeriesGroup.SeriesName
            : ViewModel.IsSeriesView ? (ViewModel.IsCreatorSection ? "Creators" : "Series") : "Library";
        Frame.Navigate(typeof(MainPage), filePath);
    }

    private void SeriesCard_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ComicSeriesGroup group)
        {
            ViewModel.OpenSeriesDetail(group);
        }
    }

    private void PlaySeriesNext_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ComicSeriesGroup group })
        {
            ViewModel.PlaySeriesNext(group);
        }
    }

    /// <summary>Esc closes the top-most Library overlay (full screen is handled window-wide before this).</summary>
    private void LibraryPage_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Escape || e.Handled) return;

        if (ViewModel.IsAddComicsToSeriesDialogOpen) ViewModel.CloseAddComicsToSeriesDialog();
        else if (ViewModel.IsCreateSeriesDialogOpen) ViewModel.CloseCreateSeriesDialog();
        else if (ViewModel.IsStatsDialogOpen) ViewModel.CloseReadingStats();
        else if (ViewModel.IsDuplicateManagerOpen) ViewModel.CloseDuplicateManager();
        else if (ViewModel.IsSeriesDetailOpen) ViewModel.CloseSeriesDetail();
        else return;

        e.Handled = true;
    }

    private void SeriesIssue_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SeriesIssueItem item)
        {
            ViewModel.OpenComic(item.Comic);
        }
        else if (e.ClickedItem is ComicEntity comic)
        {
            ViewModel.OpenComic(comic);
        }
    }

    private void DismissDuplicate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DuplicateComicGroup group })
        {
            _ = ViewModel.DismissDuplicateGroupAsync(group);
        }
    }

    private async void ResolveDuplicateGroup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: DuplicateComicGroup group } || group.RecommendedKeep is not { } keep) return;

        var extras = group.CopyItems.Where(c => !c.IsRecommended).ToList();
        var recycle = new CheckBox { Content = "Move the extra files to the Recycle Bin", IsChecked = true, Margin = new Thickness(0, 12, 0, 0) };
        var body = new StackPanel { Spacing = 6 };
        body.Children.Add(new TextBlock { Text = "Keep this copy:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        body.Children.Add(new TextBlock { Text = $"{keep.Title} ({keep.FormatBadge}, {keep.PageCountFormatted}, {keep.FileSizeFormatted})\n{keep.FilePath}", TextWrapping = TextWrapping.Wrap, Opacity = 0.85 });
        body.Children.Add(new TextBlock { Text = extras.Count == 1 ? "Remove 1 extra copy:" : $"Remove {extras.Count} extra copies:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 0) });
        foreach (var extra in extras)
        {
            body.Children.Add(new TextBlock { Text = $"• {extra.Title} ({extra.FormatBadge}, {extra.FileSizeFormatted})\n   {extra.FilePath}", TextWrapping = TextWrapping.Wrap, Opacity = 0.85 });
        }
        body.Children.Add(new TextBlock { Text = "Reading progress and favorites from the removed copies carry over to the kept copy.", TextWrapping = TextWrapping.Wrap, Opacity = 0.7, Margin = new Thickness(0, 8, 0, 0) });
        body.Children.Add(recycle);

        var dialog = new ContentDialog
        {
            Title = "Keep the best copy?",
            Content = new ScrollViewer { Content = body, MaxHeight = 380 },
            PrimaryButtonText = "Keep Best Copy",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.ResolveDuplicateGroupAsync(group, recycle.IsChecked == true);
        }
    }

    private async void ResolveAllDuplicates_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.HasDuplicates) return;

        int high = ViewModel.DuplicateGroups.Count(g => g.IsHighConfidence);
        int total = ViewModel.DuplicateGroups.Count;
        var onlyHigh = new RadioButton { Content = $"Only identical files and same-issue matches ({high} group{(high == 1 ? "" : "s")})", IsChecked = true, GroupName = "dupscope" };
        var everything = new RadioButton { Content = $"Every group, including possible matches ({total})", GroupName = "dupscope" };
        var recycle = new CheckBox { Content = "Move the extra files to the Recycle Bin", IsChecked = true, Margin = new Thickness(0, 10, 0, 0) };

        var body = new StackPanel { Spacing = 8 };
        body.Children.Add(new TextBlock { Text = "Komik keeps the best copy in each group (reading progress, favorites, page count and format decide) and removes the rest.", TextWrapping = TextWrapping.Wrap });
        body.Children.Add(onlyHigh);
        body.Children.Add(everything);
        body.Children.Add(recycle);

        var dialog = new ContentDialog
        {
            Title = "Clean up duplicates?",
            Content = body,
            PrimaryButtonText = "Clean Up",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.ResolveAllDuplicatesAsync(recycle.IsChecked == true, highConfidenceOnly: onlyHigh.IsChecked == true);
        }
    }

    private async void RemoveDuplicateCopy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: DuplicateCopyItem item }) return;

        var dialog = new ContentDialog
        {
            Title = "Remove this copy?",
            Content = new TextBlock
            {
                Text = $"{item.Title} ({item.FormatBadge}, {item.PageCountFormatted}, {item.FileSizeFormatted})\n{item.FilePath}\n\nIts reading progress and favorite carry over to the best remaining copy.",
                TextWrapping = TextWrapping.Wrap
            },
            PrimaryButtonText = "Move to Recycle Bin",
            SecondaryButtonText = "Remove from Library Only",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.RemoveDuplicateCopyAsync(item.Comic, deleteFile: true);
        }
        else if (result == ContentDialogResult.Secondary)
        {
            await ViewModel.RemoveDuplicateCopyAsync(item.Comic, deleteFile: false);
        }
    }

    private void ReadDuplicateCopy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: DuplicateCopyItem item })
        {
            ViewModel.CloseDuplicateManager();
            ViewModel.OpenComic(item.Comic);
        }
    }

    private void ShowDuplicateCopy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: DuplicateCopyItem item })
        {
            ViewModel.ShowInExplorer(item.Comic);
        }
    }

    private async void RecentlyRead_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: long comicId }) return;
        var comic = await ViewModel.Repository.GetComicByIdAsync(comicId);
        if (comic != null)
        {
            ViewModel.CloseReadingStats();
            ViewModel.OpenComic(comic);
        }
    }

    private void ComicGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ComicEntity comic)
        {
            OnComicSelectedForReading(comic.FilePath);
        }
    }

    private void ComicList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ComicEntity comic)
        {
            OnComicSelectedForReading(comic.FilePath);
        }
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ComicEntity comic })
        {
            _ = ViewModel.ToggleFavoriteAsync(comic);
        }
    }

    private void GridMode_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetViewLayout(true);
        UpdateViewModeUi();
        Bindings.Update();
    }

    private void ListMode_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetViewLayout(false);
        UpdateViewModeUi();
        Bindings.Update();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SettingsPage));
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ComicEntity comic })
        {
            _ = ShowComicDetailsDialogAsync(comic);
        }
    }

    #region Filters

    private void HeroChipComics_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (ViewModel.IsSeriesView) ViewModel.CloseSeriesView();
        ViewModel.FilterAll();
    }

    private void HeroChipReading_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (ViewModel.IsSeriesView) ViewModel.CloseSeriesView();
        ViewModel.FilterContinueReading();
    }

    private void HeroChipFinished_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (ViewModel.IsSeriesView) ViewModel.CloseSeriesView();
        ViewModel.FilterCompleted();
    }

    private void HeroChipSeries_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        ViewModel.IsCreatorSection = false;
        if (!ViewModel.IsSeriesView) ViewModel.OpenSeriesView();
    }

    private void NewKindStory_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NewSeriesIsCreator = false;
        NewKindStoryToggle.IsChecked = true;
        NewKindCreatorToggle.IsChecked = false;
    }

    private void NewKindCreator_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NewSeriesIsCreator = true;
        NewKindStoryToggle.IsChecked = false;
        NewKindCreatorToggle.IsChecked = true;
    }

    private async void DetailAutoUpdate_Click(object sender, RoutedEventArgs e)
    {
        var group = ViewModel.SelectedSeriesGroup;
        if (group == null) return;
        DetailAutoUpdateToggle.IsChecked = group.IsAutoUpdate; // the command flips it and the binding follows
        await ViewModel.ToggleSeriesAutoUpdateAsync(group);
        DetailAutoUpdateToggle.IsChecked = ViewModel.SelectedSeriesGroup?.IsAutoUpdate ?? false;
    }

    private void StorySection_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsCreatorSection = false;
        StorySectionToggle.IsChecked = true;
        CreatorSectionToggle.IsChecked = false;
    }

    private void CreatorSection_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsCreatorSection = true;
        StorySectionToggle.IsChecked = false;
        CreatorSectionToggle.IsChecked = true;
    }

    private void FilterAll_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FilterAll();
    }

    private void FilterFavorites_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FilterFavorites();
    }

    private void FilterContinueReading_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FilterContinueReading();
    }

    private void FilterUnread_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FilterUnread();
    }

    private void FilterCompleted_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FilterCompleted();
    }

    private void ClearTagFilter_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FilterByTag(null);
    }

    private void ClearActiveFilter_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.FilterAll();
    }

    private void CandidateCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ManualSeriesComicItem item })
        {
            ViewModel.ToggleCandidateSelection(item);
        }
    }

    private void RemoveComicFromSeries_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ComicEntity comic })
        {
            _ = ViewModel.RemoveComicFromManualSeriesAsync(comic);
        }
    }

    private void UpdateActiveFilterUi()
    {
        if (ViewModel.FilterCompletedOnly)
        {
            ActiveFilterText.Text = "Filter: Completed";
            ActiveFilterBadge.Visibility = Visibility.Visible;
        }
        else if (!string.IsNullOrEmpty(ViewModel.SelectedTag))
        {
            ActiveFilterText.Text = $"Tag: {ViewModel.SelectedTag}";
            ActiveFilterBadge.Visibility = Visibility.Visible;
        }
        else if (ViewModel.FilterInProgressOnly)
        {
            ActiveFilterText.Text = "Filter: Continue Reading";
            ActiveFilterBadge.Visibility = Visibility.Visible;
        }
        else if (ViewModel.FilterUnreadOnly)
        {
            ActiveFilterText.Text = "Filter: Unread";
            ActiveFilterBadge.Visibility = Visibility.Visible;
        }
        else if (ViewModel.FilterFavoritesOnly)
        {
            ActiveFilterText.Text = "Filter: Favorites";
            ActiveFilterBadge.Visibility = Visibility.Visible;
        }
        else
        {
            ActiveFilterBadge.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateFlyoutMenus()
    {
        // Tags Flyout
        TagsFlyout.Items.Clear();
        var newTagItem = new MenuFlyoutItem { Text = "+ New Tag...", Icon = new FontIcon { Glyph = "\uE710" } };
        newTagItem.Click += CreateNewTag_Click;
        TagsFlyout.Items.Add(newTagItem);

        var manageTagsItem = new MenuFlyoutItem { Text = "Manage Tags...", Icon = new FontIcon { Glyph = "\uE8EC" } };
        manageTagsItem.Click += ManageTags_Click;
        TagsFlyout.Items.Add(manageTagsItem);

        TagsFlyout.Items.Add(new MenuFlyoutSeparator());

        var allTagsItem = new MenuFlyoutItem { Text = "All Tags" };
        allTagsItem.Click += ClearTagFilter_Click;
        TagsFlyout.Items.Add(allTagsItem);

        if (ViewModel.AvailableTags.Count > 0)
        {
            TagsFlyout.Items.Add(new MenuFlyoutSeparator());
            foreach (var tag in ViewModel.AvailableTags)
            {
                var item = new MenuFlyoutItem { Text = tag };
                item.Click += (s, ev) => ViewModel.FilterByTag(tag);
                TagsFlyout.Items.Add(item);
            }
        }
    }

    #endregion

    #region Context Menu Handlers

    private void ComicContextMenu_Opening(object? sender, object e)
    {
        if (sender is MenuFlyout flyout)
        {
            var target = flyout.Target as FrameworkElement;
            var comic = (target?.DataContext as ComicEntity) ?? (target?.Tag as ComicEntity);

            bool isCbz = comic != null &&
                (comic.FilePath.EndsWith(".cbz", StringComparison.OrdinalIgnoreCase) || comic.FormatBadge == "CBZ");

            foreach (var item in flyout.Items)
            {
                if (item is MenuFlyoutItem mfi && (mfi.Name == "ContextConvertToCbzItem" || mfi.Text.StartsWith("Convert to CBZ", StringComparison.OrdinalIgnoreCase)))
                {
                    mfi.IsEnabled = !isCbz;
                    mfi.Visibility = isCbz ? Visibility.Collapsed : Visibility.Visible;
                }
            }
        }
    }

    private ComicEntity? GetComicFromMenuSender(object sender)
    {
        if (sender is MenuFlyoutItem { DataContext: ComicEntity comic }) return comic;
        if (sender is FrameworkElement { Tag: ComicEntity tagComic }) return tagComic;
        return null;
    }

    private void ContextDetails_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null)
        {
            _ = ShowComicDetailsDialogAsync(comic);
        }
    }

    private void ContextRead_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null) OnComicSelectedForReading(comic.FilePath);
    }

    private void ContextFavorite_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null) _ = ViewModel.ToggleFavoriteAsync(comic);
    }

    private void ContextToggleCompleted_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null) _ = ViewModel.ToggleCompletedStatusAsync(comic);
    }

    private async void ContextRestart_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null)
        {
            await ViewModel.RestartComicAsync(comic);
            ViewModel.ShowNotification("Reading Progress Reset", $"Restarted '{comic.Title}' from page 1.", InfoBarSeverity.Informational);
        }
    }

    private async void ContextAddTag_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null)
        {
            await ShowComicTagsDialogAsync(comic);
        }
    }

    private async void CreateNewTag_Click(object sender, RoutedEventArgs e)
    {
        string? name = await PromptTextInputAsync("New Tag", "Enter the name of your new tag:");
        if (!string.IsNullOrWhiteSpace(name))
        {
            await ViewModel.CreateTagAsync(name);
            UpdateFlyoutMenus();
            ViewModel.ShowNotification("Tag Created", $"Created tag '{name}'.", InfoBarSeverity.Success);
        }
    }

    private async void ManageTags_Click(object sender, RoutedEventArgs e)
    {
        await ShowManageTagsDialogAsync();
    }

    private async Task ShowComicTagsDialogAsync(ComicEntity comic)
    {
        var tags = await ViewModel.Repository.GetAllTagsAsync();
        var currentComicTags = new HashSet<string>(await ViewModel.Repository.GetTagsForComicAsync(comic.Id), StringComparer.OrdinalIgnoreCase);

        var stack = new StackPanel { Spacing = 10, Width = 380 };
        stack.Children.Add(new TextBlock
        {
            Text = $"Select tags for '{comic.Title}':",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });

        var checkBoxesPanel = new StackPanel { Spacing = 6 };
        var scrollViewer = new ScrollViewer { Content = checkBoxesPanel, MaxHeight = 220, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        void PopulateCheckBoxes()
        {
            checkBoxesPanel.Children.Clear();
            if (tags.Count == 0)
            {
                checkBoxesPanel.Children.Add(new TextBlock
                {
                    Text = "No tags created yet. Create one below!",
                    FontSize = 12,
                    Opacity = 0.75,
                    Margin = new Thickness(0, 4, 0, 4)
                });
            }
            else
            {
                foreach (var t in tags)
                {
                    var cb = new CheckBox
                    {
                        Content = t,
                        IsChecked = currentComicTags.Contains(t),
                        Margin = new Thickness(0, 2, 0, 2)
                    };
                    string capturedTag = t;
                    cb.Checked += async (s, e) =>
                    {
                        currentComicTags.Add(capturedTag);
                        await ViewModel.AddTagToComicAsync(comic, capturedTag);
                    };
                    cb.Unchecked += async (s, e) =>
                    {
                        currentComicTags.Remove(capturedTag);
                        await ViewModel.RemoveTagFromComicAsync(comic, capturedTag);
                    };
                    checkBoxesPanel.Children.Add(cb);
                }
            }
        }

        PopulateCheckBoxes();
        stack.Children.Add(scrollViewer);

        // Inline create new tag
        var newBox = new TextBox { PlaceholderText = "New tag name (e.g. Manga, Superhero)...", HorizontalAlignment = HorizontalAlignment.Stretch };
        var addBtn = new Button { Content = "Add", Height = 32, Padding = new Thickness(14, 0, 14, 0) };
        addBtn.Click += async (s, e) =>
        {
            string newName = newBox.Text.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                await ViewModel.CreateTagAsync(newName);
                await ViewModel.AddTagToComicAsync(comic, newName);
                currentComicTags.Add(newName);
                tags = await ViewModel.Repository.GetAllTagsAsync();
                PopulateCheckBoxes();
                newBox.Text = string.Empty;
                UpdateFlyoutMenus();
            }
        };

        var addGrid = new Grid { ColumnSpacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(newBox, 0);
        Grid.SetColumn(addBtn, 1);
        addGrid.Children.Add(newBox);
        addGrid.Children.Add(addBtn);
        stack.Children.Add(addGrid);

        var dialog = new ContentDialog
        {
            Title = "Manage Tags",
            Content = stack,
            CloseButtonText = "Done",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
        UpdateFlyoutMenus();
    }

    private async Task ShowManageTagsDialogAsync()
    {
        var mainStack = new StackPanel { Spacing = 14, Width = 420 };

        var tagHeader = new TextBlock { Text = "Tags", FontWeight = Microsoft.UI.Text.FontWeights.Bold, FontSize = 16 };
        var tagDescription = new TextBlock
        {
            Text = "Categorize comics by genre, publisher, format, or theme.",
            FontSize = 12,
            Opacity = 0.75,
            Margin = new Thickness(0, -6, 0, 4)
        };

        var tagListPanel = new StackPanel { Spacing = 4 };
        var tagScroll = new ScrollViewer { Content = tagListPanel, MaxHeight = 220, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        var newTagBox = new TextBox { PlaceholderText = "New tag name (e.g. Manga, Superhero)...", HorizontalAlignment = HorizontalAlignment.Stretch };
        var addTagBtn = new Button { Content = "Add Tag", Height = 32, Padding = new Thickness(16, 0, 16, 0) };

        async Task RefreshTagsUiAsync()
        {
            tagListPanel.Children.Clear();
            var tags = await ViewModel.Repository.GetAllTagsAsync();
            if (tags.Count == 0)
            {
                tagListPanel.Children.Add(new TextBlock
                {
                    Text = "No tags created yet. Add your first tag below.",
                    FontSize = 12,
                    Opacity = 0.75,
                    Margin = new Thickness(4)
                });
            }
            else
            {
                foreach (var t in tags)
                {
                    int count = await ViewModel.Repository.GetComicCountForTagAsync(t);
                    var itemGrid = new Grid { Padding = new Thickness(6, 4, 6, 4) };
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var label = new TextBlock
                    {
                        Text = $"{t}  ({count} {(count == 1 ? "comic" : "comics")})",
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 12
                    };

                    var delBtn = new Button
                    {
                        Content = new FontIcon { Glyph = "\uE74D", FontSize = 11 },
                        Height = 26,
                        Width = 26,
                        Padding = new Thickness(0)
                    };
                    ToolTipService.SetToolTip(delBtn, $"Delete tag '{t}'");

                    string capturedTag = t;
                    delBtn.Click += async (s, e) =>
                    {
                        await ViewModel.DeleteTagAsync(capturedTag);
                        await RefreshTagsUiAsync();
                        UpdateFlyoutMenus();
                    };

                    Grid.SetColumn(label, 0);
                    Grid.SetColumn(delBtn, 1);
                    itemGrid.Children.Add(label);
                    itemGrid.Children.Add(delBtn);
                    tagListPanel.Children.Add(itemGrid);
                }
            }
        }

        addTagBtn.Click += async (s, e) =>
        {
            string name = newTagBox.Text.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                await ViewModel.CreateTagAsync(name);
                newTagBox.Text = string.Empty;
                await RefreshTagsUiAsync();
                UpdateFlyoutMenus();
            }
        };

        var addTagGrid = new Grid { ColumnSpacing = 8 };
        addTagGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        addTagGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(newTagBox, 0);
        Grid.SetColumn(addTagBtn, 1);
        addTagGrid.Children.Add(newTagBox);
        addTagGrid.Children.Add(addTagBtn);

        var tagBorder = new Border
        {
            Background = GetThemeBrush("CardBackgroundFillColorSecondaryBrush", FallbackCardSecondary),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Child = new StackPanel { Spacing = 8, Children = { tagHeader, tagDescription, tagScroll, addTagGrid } }
        };

        await RefreshTagsUiAsync();
        mainStack.Children.Add(tagBorder);

        var dialog = new ContentDialog
        {
            Title = "Manage Tags",
            Content = mainStack,
            CloseButtonText = "Done",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
        UpdateFlyoutMenus();
    }

    private async void ContextConvertToCbz_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic == null) return;

        if (comic.FilePath.EndsWith(".cbz", StringComparison.OrdinalIgnoreCase) || comic.FormatBadge == "CBZ")
        {
            ViewModel.ShowNotification("Already CBZ", $"'{comic.Title}' is already in standard CBZ format.", InfoBarSeverity.Informational);
            return;
        }

        bool isDir = Directory.Exists(comic.FilePath);
        string defaultDest = FormatConversionService.GenerateDefaultDestinationPath(comic.FilePath, isDir);

        var destBox = new TextBox
        {
            Text = defaultDest,
            Width = 420,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 4, 0, 12)
        };

        var addCheck = new CheckBox
        {
            Content = "After conversion, add new CBZ to library",
            IsChecked = true,
            Margin = new Thickness(0, 4, 0, 4)
        };

        var removeOldCheck = new CheckBox
        {
            Content = "Remove old entry from library",
            IsChecked = false,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var removeHint = new TextBlock
        {
            Text = "Removes original entry from library index. Original file/folder on disk will NOT be deleted.",
            FontSize = 11,
            Opacity = 0.75,
            Margin = new Thickness(28, 0, 0, 8),
            TextWrapping = TextWrapping.Wrap
        };

        var contentStack = new StackPanel
        {
            Spacing = 6,
            Width = 440,
            Children =
            {
                new TextBlock
                {
                    Text = $"Convert '{comic.Title}' ({comic.FormatBadge}) to a standard Comic Book Zip archive (.cbz):",
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                new TextBlock { Text = "Target Destination:", FontSize = 12, Foreground = GetThemeBrush("TextFillColorSecondaryBrush", FallbackTextSecondary) },
                destBox,
                addCheck,
                removeOldCheck,
                removeHint
            }
        };

        var dialog = new ContentDialog
        {
            Title = "Convert to CBZ",
            Content = contentStack,
            PrimaryButtonText = "Convert",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            string chosenDest = string.IsNullOrWhiteSpace(destBox.Text) ? defaultDest : destBox.Text.Trim();
            bool addToLib = addCheck.IsChecked == true;
            bool removeOld = removeOldCheck.IsChecked == true;

            await ViewModel.ConvertComicAsync(comic.FilePath, chosenDest, addToLib, removeOld, comic.Id);
        }
    }

    private async void ConvertStandaloneFile_Click(object sender, RoutedEventArgs e)
    {
        var pickerService = new FilePickerService();
        string? pickedFile = await pickerService.PickComicFileAsync();
        if (string.IsNullOrWhiteSpace(pickedFile)) return;

        if (pickedFile.EndsWith(".cbz", StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.ShowNotification("Already CBZ", $"'{Path.GetFileName(pickedFile)}' is already in standard CBZ format.", InfoBarSeverity.Informational);
            return;
        }

        await ShowStandaloneConversionDialogAsync(pickedFile, isDirectory: false);
    }

    private async void ConvertStandaloneFolder_Click(object sender, RoutedEventArgs e)
    {
        var pickerService = new FilePickerService();
        string? pickedFolder = await pickerService.PickComicFolderAsync();
        if (string.IsNullOrWhiteSpace(pickedFolder)) return;

        await ShowStandaloneConversionDialogAsync(pickedFolder, isDirectory: true);
    }

    private async Task ShowStandaloneConversionDialogAsync(string sourcePath, bool isDirectory)
    {
        string defaultDest = FormatConversionService.GenerateDefaultDestinationPath(sourcePath, isDirectory);
        string displayName = Path.GetFileName(sourcePath);
        if (string.IsNullOrEmpty(displayName)) displayName = sourcePath;

        var destBox = new TextBox
        {
            Text = defaultDest,
            Width = 420,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 4, 0, 12)
        };

        var addCheck = new CheckBox
        {
            Content = "Add new CBZ to library after conversion",
            IsChecked = true,
            Margin = new Thickness(0, 4, 0, 8)
        };

        var contentStack = new StackPanel
        {
            Spacing = 6,
            Width = 440,
            Children =
            {
                new TextBlock
                {
                    Text = $"Convert '{(isDirectory ? "Folder: " : "")}{displayName}' to a standard Comic Book Zip archive (.cbz):",
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                new TextBlock { Text = "Target Destination:", FontSize = 12, Foreground = GetThemeBrush("TextFillColorSecondaryBrush", FallbackTextSecondary) },
                destBox,
                addCheck
            }
        };

        var dialog = new ContentDialog
        {
            Title = "Convert to CBZ",
            Content = contentStack,
            PrimaryButtonText = "Convert",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            string chosenDest = string.IsNullOrWhiteSpace(destBox.Text) ? defaultDest : destBox.Text.Trim();
            bool addToLib = addCheck.IsChecked == true;

            await ViewModel.ConvertComicAsync(sourcePath, chosenDest, addToLib);
        }
    }

    private void ContextExplorer_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null) ViewModel.ShowInExplorer(comic);
    }

    private void ContextRemove_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null) _ = ViewModel.RemoveFromLibraryAsync(comic);
    }

    private async Task<string?> PromptTextInputAsync(string title, string instruction, string defaultText = "")
    {
        var textBox = new TextBox { Text = defaultText, Width = 300, Margin = new Thickness(0, 10, 0, 0) };
        if (!string.IsNullOrEmpty(defaultText))
        {
            textBox.SelectAll();
        }
        var stack = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = instruction, TextWrapping = TextWrapping.Wrap },
                textBox
            }
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = stack,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? textBox.Text.Trim() : null;
    }

    private async Task ShowComicDetailsDialogAsync(ComicEntity comic)
    {
        var metadata = await ViewModel.GetComicMetadataAsync(comic.Id);

        // --- Left Column: Cover & File Stats ---
        var coverGrid = new Grid
        {
            CornerRadius = new CornerRadius(8),
            Height = 220,
            Width = 150,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var placeholderBorder = new Border
        {
            Background = GetThemeBrush("CardBackgroundFillColorSecondaryBrush", FallbackCardSecondary),
            CornerRadius = new CornerRadius(8),
            Child = new FontIcon
            {
                Glyph = "\uE82D",
                FontSize = 36,
                Opacity = 0.75,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        coverGrid.Children.Add(placeholderBorder);

        if (!string.IsNullOrWhiteSpace(comic.ThumbnailPath) && File.Exists(comic.ThumbnailPath))
        {
            var coverImage = new Image
            {
                Source = new BitmapImage(new Uri(comic.ThumbnailPath)),
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            coverGrid.Children.Add(coverImage);
        }

        var statsPanel = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // Format & Page count
        statsPanel.Children.Add(new TextBlock
        {
            Text = $"{comic.FormatBadge} • {comic.PageCountFormatted}",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // File size
        statsPanel.Children.Add(new TextBlock
        {
            Text = $"Size: {comic.FileSizeFormatted}",
            FontSize = 11,
            Opacity = 0.75,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // Reading progress info
        statsPanel.Children.Add(new TextBlock
        {
            Text = comic.IsCompleted ? "Status: Completed" : (comic.IsInProgress ? $"Status: Page {comic.LastReadPage + 1}/{comic.PageCount}" : "Status: Unread"),
            FontSize = 11,
            Foreground = comic.IsCompleted ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 124, 65)) : GetThemeBrush("TextFillColorSecondaryBrush", FallbackTextSecondary),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        // Date Added
        statsPanel.Children.Add(new TextBlock
        {
            Text = $"Added: {comic.DateAdded:d}",
            FontSize = 10,
            Foreground = GetThemeBrush("TextFillColorTertiaryBrush", FallbackTextTertiary),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        if (comic.IsMissing)
        {
            statsPanel.Children.Add(new TextBlock
            {
                Text = "⚠ File Missing / Offline",
                FontSize = 11,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 209, 52, 56)),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        var leftPanel = new StackPanel
        {
            Width = 150,
            Children = { coverGrid, statsPanel }
        };

        // --- Right Column: Editable Metadata Fields ---
        var rightPanel = new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var titleBox = new TextBox
        {
            Header = "Title",
            Text = metadata?.Title ?? comic.Title,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        rightPanel.Children.Add(titleBox);

        var seriesRow = new Grid { ColumnSpacing = 8, HorizontalAlignment = HorizontalAlignment.Stretch };
        seriesRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        seriesRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var seriesBox = new TextBox
        {
            Header = "Series",
            Text = metadata?.SeriesName ?? string.Empty,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetColumn(seriesBox, 0);
        seriesRow.Children.Add(seriesBox);

        var issueBox = new TextBox
        {
            Header = "Issue #",
            Text = metadata?.IssueNumber ?? string.Empty,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetColumn(issueBox, 1);
        seriesRow.Children.Add(issueBox);
        rightPanel.Children.Add(seriesRow);

        var creditsRow = new Grid { ColumnSpacing = 8, HorizontalAlignment = HorizontalAlignment.Stretch };
        creditsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        creditsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var writerBox = new TextBox
        {
            Header = "Writer(s)",
            Text = metadata?.Writers ?? string.Empty,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetColumn(writerBox, 0);
        creditsRow.Children.Add(writerBox);

        var artistBox = new TextBox
        {
            Header = "Artist(s)",
            Text = metadata?.Artists ?? string.Empty,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetColumn(artistBox, 1);
        creditsRow.Children.Add(artistBox);
        rightPanel.Children.Add(creditsRow);

        var pubRow = new Grid { ColumnSpacing = 8, HorizontalAlignment = HorizontalAlignment.Stretch };
        pubRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
        pubRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pubBox = new TextBox
        {
            Header = "Publisher",
            Text = metadata?.Publisher ?? string.Empty,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetColumn(pubBox, 0);
        pubRow.Children.Add(pubBox);

        var dateBox = new TextBox
        {
            Header = "Year / Date",
            Text = metadata?.ReleaseDate ?? string.Empty,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Grid.SetColumn(dateBox, 1);
        pubRow.Children.Add(dateBox);
        rightPanel.Children.Add(pubRow);

        var summaryBox = new TextBox
        {
            Header = "Summary / Synopsis",
            Text = metadata?.Summary ?? string.Empty,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 72,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        rightPanel.Children.Add(summaryBox);

        var pathBox = new TextBox
        {
            Header = "File Location",
            Text = comic.FilePath,
            IsReadOnly = true,
            FontSize = 11,
            Opacity = 0.75,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        rightPanel.Children.Add(pathBox);

        // --- Dialog Layout Grid ---
        var mainGrid = new Grid
        {
            ColumnSpacing = 16,
            Width = 540
        };
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(leftPanel, 0);
        mainGrid.Children.Add(leftPanel);

        Grid.SetColumn(rightPanel, 1);
        mainGrid.Children.Add(rightPanel);

        var scrollViewer = new ScrollViewer
        {
            Content = mainGrid,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 520
        };

        var dialog = new ContentDialog
        {
            Title = "Comic Details",
            Content = scrollViewer,
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Read Comic",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary || result == ContentDialogResult.Secondary)
        {
            var updatedMeta = metadata ?? new ComicMetadataEntity { ComicId = comic.Id };
            updatedMeta.Title = string.IsNullOrWhiteSpace(titleBox.Text) ? comic.Title : titleBox.Text.Trim();
            updatedMeta.SeriesName = string.IsNullOrWhiteSpace(seriesBox.Text) ? null : seriesBox.Text.Trim();
            updatedMeta.IssueNumber = string.IsNullOrWhiteSpace(issueBox.Text) ? null : issueBox.Text.Trim();
            updatedMeta.Writers = string.IsNullOrWhiteSpace(writerBox.Text) ? null : writerBox.Text.Trim();
            updatedMeta.Artists = string.IsNullOrWhiteSpace(artistBox.Text) ? null : artistBox.Text.Trim();
            updatedMeta.Publisher = string.IsNullOrWhiteSpace(pubBox.Text) ? null : pubBox.Text.Trim();
            updatedMeta.ReleaseDate = string.IsNullOrWhiteSpace(dateBox.Text) ? null : dateBox.Text.Trim();
            updatedMeta.Summary = string.IsNullOrWhiteSpace(summaryBox.Text) ? null : summaryBox.Text.Trim();
            updatedMeta.LastUpdated = DateTime.UtcNow;

            await ViewModel.SaveComicMetadataAsync(updatedMeta, comic);

            if (result == ContentDialogResult.Secondary)
            {
                OnComicSelectedForReading(comic.FilePath);
            }
            else
            {
                ViewModel.ShowNotification("Metadata Saved", $"Updated details for '{comic.Title}'.", InfoBarSeverity.Success);
            }
        }
    }

    private void HorizontalScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (sender is ScrollViewer sv && sv.ScrollableWidth > 0)
        {
            var delta = e.GetCurrentPoint(sv).Properties.MouseWheelDelta;
            if (delta != 0)
            {
                sv.ChangeView(sv.HorizontalOffset - delta, null, null);
                e.Handled = true;
            }
        }
    }

    #endregion
}
