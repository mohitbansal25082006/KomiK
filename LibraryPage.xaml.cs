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
        if (EmptyFilterContainer != null) UpdateEmptyBurstAnimation();
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
        if (ViewModel.IsSeriesView) ViewModel.CloseSeriesView();
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
            e.PropertyName == nameof(ViewModel.ShowEmptyFilter) ||
            e.PropertyName == nameof(ViewModel.SearchText) ||
            e.PropertyName == nameof(ViewModel.TotalComicCount))
        {
            UpdateActiveFilterUi();
            UpdateViewModeUi();
            Bindings.Update();
        }
        else if (e.PropertyName == nameof(ViewModel.AvailableTags))
        {
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

        if (ViewModel.IsComicTagsOpen) ViewModel.CloseComicTags();
        else if (ViewModel.IsManageTagsOpen) ViewModel.CloseManageTags();
        else if (ViewModel.IsEditSeriesDialogOpen) ViewModel.CloseEditSeries();
        else if (ViewModel.IsAddComicsToSeriesDialogOpen) ViewModel.CloseAddComicsToSeriesDialog();
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
        TagFilterFlyout.Hide();
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

    /// <summary>Colored sticker next to the filter chips: which filter or tag is on, how many comics match, and a clear button.</summary>
    private void UpdateActiveFilterUi()
    {
        (string Kind, string Text, string Glyph, byte R, byte G, byte B)? state =
            !string.IsNullOrWhiteSpace(ViewModel.SearchText) ? ("SEARCH", $"\u201C{ViewModel.SearchText.Trim()}\u201D", "\uE721", 0x00, 0xC2, 0xFF)
            : !string.IsNullOrEmpty(ViewModel.SelectedTag) ? ("TAG", ViewModel.SelectedTag!, "\uE8EC", 0xFF, 0xD7, 0x00)
            : ViewModel.FilterCompletedOnly ? ("FILTER", "Completed", "\uE73E", 0x2E, 0xE5, 0x9D)
            : ViewModel.FilterInProgressOnly ? ("FILTER", "Continue Reading", "\uE768", 0x00, 0xC2, 0xFF)
            : ViewModel.FilterUnreadOnly ? ("FILTER", "Unread", "\uE8A5", 0xFF, 0x9F, 0x1C)
            : ViewModel.FilterFavoritesOnly ? ("FILTER", "Favorites", "\uEB52", 0xFF, 0x4D, 0x8D)
            : null;

        bool tagOn = !string.IsNullOrEmpty(ViewModel.SelectedTag);
        if (tagOn)
        {
            TagsDropDown.Background = Chip(0xFF, 0xD7, 0x00);
            TagsDropDown.Foreground = ChipInk;
            TagsDropDown.BorderBrush = ChipInk;
        }
        else
        {
            TagsDropDown.ClearValue(Control.BackgroundProperty);
            TagsDropDown.ClearValue(Control.ForegroundProperty);
            TagsDropDown.ClearValue(Control.BorderBrushProperty);
        }

        if (state is not { } s || ViewModel.IsSeriesView)
        {
            ActiveFilterBadge.Visibility = Visibility.Collapsed;
            return;
        }

        ActiveFilterKind.Text = s.Kind;
        ActiveFilterText.Text = s.Text;
        ActiveFilterGlyph.Glyph = s.Glyph;
        ActiveFilterBadge.Background = Chip(s.R, s.G, s.B);
        int count = ViewModel.Comics.Count;
        ActiveFilterCount.Text = count == 1 ? "1 comic" : $"{count} comics";
        ToolTipService.SetToolTip(ActiveFilterBadge, $"{(s.Kind == "FILTER" ? "Showing" : s.Kind == "TAG" ? "Comics tagged" : "Search for")} {s.Text}: {ActiveFilterCount.Text}");
        ActiveFilterBadge.Visibility = Visibility.Visible;
    }

    private async void EmptyFilterAction_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsSeriesView || !string.IsNullOrWhiteSpace(ViewModel.SearchText))
        {
            SearchBox.Text = string.Empty;
        }
        await ViewModel.RunEmptyFilterActionAsync();
    }

    /// <summary>The empty-screen burst rocks gently while it's on screen.</summary>
    private Microsoft.UI.Xaml.Media.Animation.Storyboard? _emptyBurstWiggle;

    private void UpdateEmptyBurstAnimation()
    {
        bool visible = EmptyFilterContainer.Visibility == Visibility.Visible;
        if (visible && _emptyBurstWiggle == null)
        {
            var rock = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = -7,
                To = 7,
                Duration = new Duration(TimeSpan.FromSeconds(1.8)),
                AutoReverse = true,
                RepeatBehavior = Microsoft.UI.Xaml.Media.Animation.RepeatBehavior.Forever,
                EasingFunction = new Microsoft.UI.Xaml.Media.Animation.SineEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut }
            };
            _emptyBurstWiggle = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
            _emptyBurstWiggle.Children.Add(rock);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(rock, EmptyBurstRotate);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(rock, "Angle");
            _emptyBurstWiggle.Begin();
        }
        else if (!visible && _emptyBurstWiggle != null)
        {
            _emptyBurstWiggle.Stop();
            _emptyBurstWiggle = null;
        }
    }

    #endregion

    #region Context Menu Handlers

    private void ComicContextMenu_Opening(object? sender, object e)
    {
        if (sender is MenuFlyout flyout)
        {
            var target = flyout.Target as FrameworkElement;
            var comic = (target?.DataContext as ComicEntity) ?? (target?.DataContext as SeriesIssueItem)?.Comic ?? (target?.Tag as ComicEntity);

            bool isCbz = comic != null &&
                (comic.FilePath.EndsWith(".cbz", StringComparison.OrdinalIgnoreCase) || comic.FormatBadge == "CBZ");

            // Inside an open series the menu also offers series actions.
            bool inSeries = target?.DataContext is SeriesIssueItem && ViewModel.SelectedSeriesGroup != null;
            ContextSetCoverItem.Visibility = inSeries ? Visibility.Visible : Visibility.Collapsed;
            ContextRemoveFromSeriesItem.Visibility = inSeries && ViewModel.SelectedSeriesGroup!.IsManual ? Visibility.Visible : Visibility.Collapsed;

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
        if (sender is MenuFlyoutItem { DataContext: SeriesIssueItem issue }) return issue.Comic;
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
        if (comic != null) _ = RunThenRefreshSeriesAsync(ViewModel.ToggleFavoriteAsync(comic));
    }

    private void ContextToggleCompleted_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null) _ = RunThenRefreshSeriesAsync(ViewModel.ToggleCompletedStatusAsync(comic));
    }

    private async void ContextRestart_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null)
        {
            await ViewModel.RestartComicAsync(comic);
            RefreshSeriesIfShown();
            ViewModel.ShowNotification("Reading Progress Reset", $"Restarted '{comic.Title}' from page 1.", InfoBarSeverity.Informational);
        }
    }

    private async void ContextAddTag_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null)
        {
            await ViewModel.OpenComicTagsAsync(comic);
        }
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
