using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Komik.Helpers;
using Komik.Models;
using Komik.ViewModels;

namespace Komik;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new();
    private bool _isInitializing = true;

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance?.UpdateTitle("Settings");
        _isInitializing = true;
        ViewModel.IsNotificationOpen = false;
        await ViewModel.InitializeAsync();
        ViewModel.IsNotificationOpen = false;
        SyncThemeTiles();
        SyncReadingTiles(animate: false);
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            _isInitializing = false;
            ViewModel.IsNotificationOpen = false;
        });
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
        else
        {
            Frame.Navigate(typeof(LibraryPage));
        }
    }

    private void ThemeTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string tag } && int.TryParse(tag, out int index))
        {
            ViewModel.ThemeIndex = index;
            SyncThemeTiles();
            if (!_isInitializing) _ = ViewModel.SaveSettingsAsync();
        }
    }

    private void ReaderModeTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string tag } && int.TryParse(tag, out int index))
        {
            ViewModel.ReaderViewModeIndex = index;
            SyncReadingTiles(animate: true);
            if (!_isInitializing) _ = ViewModel.SaveSettingsAsync();
        }
    }

    private void DirectionTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string tag } && int.TryParse(tag, out int index))
        {
            ViewModel.ReadingDirectionIndex = index;
            SyncReadingTiles(animate: true);
            if (!_isInitializing) _ = ViewModel.SaveSettingsAsync();
        }
    }

    /// <summary>Lights up the chosen layout and direction and spells out how comics will open.</summary>
    private void SyncReadingTiles(bool animate)
    {
        ReaderSingleTile.IsChecked = ViewModel.ReaderViewModeIndex == 0;
        ReaderSpreadTile.IsChecked = ViewModel.ReaderViewModeIndex == 1;
        ReaderWebtoonTile.IsChecked = ViewModel.ReaderViewModeIndex == 2;
        DirectionWesternTile.IsChecked = ViewModel.ReadingDirectionIndex != 1;
        DirectionMangaTile.IsChecked = ViewModel.ReadingDirectionIndex == 1;

        string layout = ViewModel.ReaderViewModeIndex switch { 1 => "as a two-page spread", 2 => "as a webtoon strip", _ => "page by page" };
        string direction = ViewModel.ReadingDirectionIndex == 1 ? "manga style, right to left" : "left to right";
        ReadingModeSummaryText.Text = ViewModel.ReaderViewModeIndex == 2
            ? $"Comics open {layout}, scrolling down{(ViewModel.ReadingDirectionIndex == 1 ? "; speech bubbles are read right to left" : string.Empty)}."
            : $"Comics open {layout}, {direction}.";
        if (animate) Helpers.MotionHelper.PlayReveal(ReadingModeSummary);
    }

    private void SyncThemeTiles()
    {
        ThemeSystemTile.IsChecked = ViewModel.ThemeIndex == 0;
        ThemeLightTile.IsChecked = ViewModel.ThemeIndex == 1;
        ThemeDarkTile.IsChecked = ViewModel.ThemeIndex == 2;
    }

    private void JumpTo(FrameworkElement section)
    {
        var point = section.TransformToVisual((UIElement)SettingsScroller.Content).TransformPoint(new Windows.Foundation.Point(0, 0));
        SettingsScroller.ChangeView(null, point.Y, null, false);
        Helpers.MotionHelper.PlayReveal(section);
    }

    private void NavAppearance_Click(object sender, RoutedEventArgs e) => JumpTo(AppearanceSection);
    private void NavReading_Click(object sender, RoutedEventArgs e) => JumpTo(ReadingSection);
    private void NavFolders_Click(object sender, RoutedEventArgs e) => JumpTo(FoldersSection);
    private void NavCovers_Click(object sender, RoutedEventArgs e) => JumpTo(CoversSection);
    private void NavAddBack_Click(object sender, RoutedEventArgs e) => JumpTo(AddBackSection);
    private void NavBackup_Click(object sender, RoutedEventArgs e) => JumpTo(BackupSection);
    private void NavAbout_Click(object sender, RoutedEventArgs e) => JumpTo(AboutSection);

    private void SettingChanged(object sender, object e)
    {
        if (_isInitializing) return;
        _ = ViewModel.SaveSettingsAsync();
    }

    private async void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WatchedFolder folder })
        {
            string name = System.IO.Path.GetFileName(folder.Path.TrimEnd('\\', '/'));
            var dialog = ComicDialogXaml.Create(XamlRoot,
                ComicDialogXaml.Header("REMOVE", "#FF1F6D", "#FFFFFF", string.IsNullOrEmpty(name) ? "Remove this folder?" : name, "Stop watching this folder"),
                ComicDialogXaml.Load($@"
<StackPanel {{NS}} Spacing=""12"">
  {PathPanel(folder.Path)}
  <TextBlock Text=""Comics inside it leave your library, along with their place in the list."" TextWrapping=""Wrap"" FontSize=""13"" />
  {KeepSafeNote("Nothing is deleted from your disk. The comic files stay exactly where they are.")}
</StackPanel>"),
                480);
            dialog.PrimaryButtonText = "Remove Folder";
            dialog.CloseButtonText = "Keep It";
            dialog.DefaultButton = ContentDialogButton.Close;

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.RemoveFolderAsync(folder);
            }
        }
    }

    private async void CacheAllCoversButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = ComicDialogXaml.Create(XamlRoot,
            ComicDialogXaml.Header("COVERS", "#00C2FF", "#0B0B12", "Cache every cover?", "One pass over your whole library"),
            ComicDialogXaml.Load($@"
<StackPanel {{NS}} Spacing=""12"">
  <TextBlock Text=""Komik opens each comic once and saves its cover, so the library grid and series screens scroll without a flicker."" TextWrapping=""Wrap"" FontSize=""13"" />
  {KeepSafeNote("Big libraries take a few minutes. You can keep reading while it works.")}
</StackPanel>"),
            480);
        dialog.PrimaryButtonText = "Cache All Covers";
        dialog.CloseButtonText = "Not Now";
        dialog.DefaultButton = ContentDialogButton.Primary;

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.CacheAllCoversAsync();
        }
    }

    private async void ClearCacheButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = ComicDialogXaml.Create(XamlRoot,
            ComicDialogXaml.Header("CLEAR", "#FF9F1C", "#0B0B12", "Clear the cover cache?", ViewModel.ThumbnailCacheSizeText),
            ComicDialogXaml.Load($@"
<StackPanel {{NS}} Spacing=""12"">
  <TextBlock Text=""Every saved cover thumbnail is deleted to free up space. Covers come back on their own as you browse, so the library looks plain for a moment."" TextWrapping=""Wrap"" FontSize=""13"" />
  {KeepSafeNote("Your comics, reading progress, tags and bookmarks are not touched.")}
</StackPanel>"),
            480);
        dialog.PrimaryButtonText = "Clear Cache";
        dialog.CloseButtonText = "Keep Covers";
        dialog.DefaultButton = ContentDialogButton.Close;

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.ClearThumbnailCacheAsync();
        }
    }

    /// <summary>A sunken panel showing a folder path, like the file details in the comic dialogs.</summary>
    private static string PathPanel(string path) => $@"
<Grid ColumnSpacing=""10"" Padding=""10"" CornerRadius=""10"" Background=""{{ThemeResource KomikPanelSunkenBrush}}"" BorderBrush=""{{ThemeResource KomikInkStrokeBrush}}"" BorderThickness=""1.5,1.5,3,3"">
  <Grid.ColumnDefinitions><ColumnDefinition Width=""Auto"" /><ColumnDefinition Width=""*"" /></Grid.ColumnDefinitions>
  <FontIcon Glyph=""&#xE8B7;"" FontSize=""18"" VerticalAlignment=""Center"" Foreground=""{{ThemeResource KomikCyanBrush}}"" />
  <TextBlock Grid.Column=""1"" Text=""{ComicDialogXaml.E(path)}"" FontSize=""12"" FontFamily=""Consolas"" TextWrapping=""Wrap"" VerticalAlignment=""Center"" />
</Grid>";

    /// <summary>The reassuring line each of these dialogs ends with.</summary>
    private static string KeepSafeNote(string text) => $@"
<Border Background=""{{ThemeResource KomikPanelRaisedBrush}}"" BorderBrush=""{{ThemeResource KomikInkStrokeBrush}}"" BorderThickness=""1.5"" CornerRadius=""10"" Padding=""10,8"">
  <StackPanel Orientation=""Horizontal"" Spacing=""8"">
    <FontIcon Glyph=""&#xE946;"" FontSize=""15"" Foreground=""{{ThemeResource KomikSubtleTextBrush}}"" VerticalAlignment=""Top"" />
    <TextBlock Text=""{ComicDialogXaml.E(text)}"" FontSize=""12"" TextWrapping=""Wrap"" Foreground=""{{ThemeResource KomikSubtleTextBrush}}"" />
  </StackPanel>
</Border>";
}
