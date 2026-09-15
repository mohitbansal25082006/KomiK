using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Komik.Helpers;
using Komik.Models;

namespace Komik;

/// <summary>Series, tag and comic dialogs of the library page.</summary>
public sealed partial class LibraryPage
{
    private async Task RunThenRefreshSeriesAsync(Task action)
    {
        await action;
        RefreshSeriesIfShown();
    }

    /// <summary>Reading state changed from a menu: keep series progress, "up next" and badges current.</summary>
    private void RefreshSeriesIfShown()
    {
        if (ViewModel.IsSeriesView || ViewModel.IsSeriesDetailOpen) _ = ViewModel.UpdateSeriesGroupsAsync();
    }

    #region Series: cover, context menu, edit, picker

    private async void ContextSetCover_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null) await ViewModel.SetSeriesCoverAsync(ViewModel.SelectedSeriesGroup, comic);
    }

    private async void ContextRemoveFromSeries_Click(object sender, RoutedEventArgs e)
    {
        var comic = GetComicFromMenuSender(sender);
        if (comic != null) await ViewModel.RemoveComicFromManualSeriesAsync(comic);
    }

    private void CoverPickerFlyout_Opened(object sender, object e)
    {
        if (ViewModel.SelectedSeriesGroup is { } group && ViewModel.GetCoverComicId(group) is long id)
        {
            var current = group.IssueItems.FirstOrDefault(i => i.Comic.Id == id);
            if (current != null) CoverPickerGrid.ScrollIntoView(current);
        }
    }

    private async void CoverPickerItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not SeriesIssueItem item) return;
        CoverPickerFlyout.Hide();
        await ViewModel.SetSeriesCoverAsync(ViewModel.SelectedSeriesGroup, item.Comic);
    }

    private void CoverReset_Click(object sender, RoutedEventArgs e) => CoverPickerFlyout.Hide();

    private void CandidateGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ManualSeriesComicItem item) ViewModel.ToggleCandidateSelection(item);
    }

    private void EditKindStory_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.EditSeriesIsCreator = false;
        EditKindStoryToggle.IsChecked = true;
        EditKindCreatorToggle.IsChecked = false;
    }

    private void EditKindCreator_Click(object sender, RoutedEventArgs e)
    {
        bool allowed = ViewModel.EditSeriesCanBeCreator;
        ViewModel.EditSeriesIsCreator = allowed;
        EditKindStoryToggle.IsChecked = !allowed;
        EditKindCreatorToggle.IsChecked = allowed;
    }

    private void EditSeriesList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args) => ViewModel.NotifyEditOrderChanged();

    private void EditItemCover_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: EditSeriesComicItem item }) ViewModel.SetEditCover(item);
    }

    private void EditItemUp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: EditSeriesComicItem item }) ViewModel.MoveEditItem(item, -1);
    }

    private void EditItemDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: EditSeriesComicItem item }) ViewModel.MoveEditItem(item, 1);
    }

    private void EditItemRemove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: EditSeriesComicItem item }) ViewModel.RemoveEditItem(item);
    }

    #endregion

    #region Tags

    private void TagFilterFlyout_Opened(object sender, object e)
    {
        ViewModel.TagFilterSearchText = string.Empty;
        _ = ViewModel.RefreshTagUsageAsync();
        TagFilterSearchBox.Focus(FocusState.Programmatic);
    }

    private void TagFilterItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not TagChipItem tag) return;
        TagFilterFlyout.Hide();
        ViewModel.ApplyTagFilter(tag);
    }

    private async void ManageTags_Click(object sender, RoutedEventArgs e)
    {
        TagFilterFlyout.Hide();
        if (ViewModel.IsComicTagsOpen) ViewModel.CloseComicTags();
        await ViewModel.OpenManageTagsAsync();
        NewTagBox.Focus(FocusState.Programmatic);
    }

    private void NewTagBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Enter) return;
        e.Handled = true;
        if (sender is TextBox box) ViewModel.NewTagName = box.Text;
        _ = ViewModel.CreateManagedTagAsync();
    }

    private void ManageTagItem_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is TagChipItem tag) ViewModel.ShowComicsWithTag(tag);
    }

    private async void RenameTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TagChipItem tag }) return;

        var box = new TextBox { Text = tag.Name, FontSize = 15, MinHeight = 40, Header = "New name" };
        box.SelectAll();
        var body = new StackPanel { Spacing = 10 };
        body.Children.Add(box);
        body.Children.Add(ComicDialogXaml.Load($@"<TextBlock {{NS}} Text=""Used on {ComicDialogXaml.E(tag.CountDisplay)}. Typing the name of another tag merges the two."" FontSize=""12"" TextWrapping=""Wrap"" Foreground=""{{ThemeResource KomikSubtleTextBrush}}"" />"));

        var dialog = ComicDialogXaml.Create(XamlRoot, ComicDialogXaml.Header("RENAME", "#00C2FF", "#0B0B12", tag.Name), body, 460);
        dialog.PrimaryButtonText = "Rename";
        dialog.CloseButtonText = "Cancel";
        dialog.DefaultButton = ContentDialogButton.Primary;
        box.KeyDown += (_, args) =>
        {
            if (args.Key == Windows.System.VirtualKey.Enter)
            {
                args.Handled = true;
                dialog.Hide();
                _ = ViewModel.RenameTagAsync(tag, box.Text);
            }
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.RenameTagAsync(tag, box.Text);
        }
    }

    private async void DeleteTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TagChipItem tag }) return;
        if (tag.Count > 0)
        {
            var dialog = ComicDialogXaml.Create(XamlRoot,
                ComicDialogXaml.Header("DELETE", "#FF1F6D", "#FFFFFF", tag.Name),
                ComicDialogXaml.Load($@"<TextBlock {{NS}} Text=""This removes the tag from {ComicDialogXaml.E(tag.CountDisplay)}. The comics themselves stay in your library."" TextWrapping=""Wrap"" FontSize=""13"" />"),
                460);
            dialog.PrimaryButtonText = "Delete Tag";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = ContentDialogButton.Close;
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        }
        await ViewModel.DeleteTagItemAsync(tag);
    }

    private async void DeleteUnusedTags_Click(object sender, RoutedEventArgs e)
    {
        var dialog = ComicDialogXaml.Create(XamlRoot,
            ComicDialogXaml.Header("TIDY UP", "#FF9F1C", "#0B0B12", "Delete unused tags?"),
            ComicDialogXaml.Load($@"<TextBlock {{NS}} Text=""{ViewModel.UnusedTagCount} tag(s) are not on any comic. They will be deleted; tags in use are not touched."" TextWrapping=""Wrap"" FontSize=""13"" />"),
            460);
        dialog.PrimaryButtonText = "Delete Unused";
        dialog.CloseButtonText = "Cancel";
        dialog.DefaultButton = ContentDialogButton.Primary;
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteUnusedTagsAsync();
        }
    }

    private void ComicTagChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TagChipItem tag }) _ = ViewModel.ToggleComicTagAsync(tag);
    }

    /// <summary>Enter adds the typed tag: an existing one is switched on, a new one is created.</summary>
    private void ComicTagSearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Enter) return;
        e.Handled = true;
        if (sender is TextBox box) ViewModel.ComicTagSearchText = box.Text;
        string name = ViewModel.ComicTagSearchText.Trim();
        if (name.Length == 0) return;

        var match = ViewModel.ComicTagItems.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            if (!match.IsSelected) _ = ViewModel.ToggleComicTagAsync(match);
            ViewModel.ComicTagSearchText = string.Empty;
        }
        else if (ViewModel.CanCreateTagFromSearch)
        {
            _ = ViewModel.CreateTagFromSearchAsync();
        }
        else if (ViewModel.ComicTagItems.Count == 1)
        {
            if (!ViewModel.ComicTagItems[0].IsSelected) _ = ViewModel.ToggleComicTagAsync(ViewModel.ComicTagItems[0]);
            ViewModel.ComicTagSearchText = string.Empty;
        }
    }

    #endregion

    #region Comic details

    private async Task ShowComicDetailsDialogAsync(ComicEntity comic)
    {
        var metadata = await ViewModel.GetComicMetadataAsync(comic.Id);
        var tags = await ViewModel.Repository.GetTagsForComicAsync(comic.Id);
        static string E(string? s) => ComicDialogXaml.E(s);

        string status = comic.IsCompleted ? "FINISHED" : comic.IsInProgress ? $"PAGE {comic.LastReadPage + 1} OF {comic.PageCount}" : "UNREAD";
        string statusColor = comic.IsCompleted ? "#2EE59D" : comic.IsInProgress ? "#00C2FF" : "#FF9F1C";
        double progress = comic.IsCompleted ? 1 : comic.PageCount > 0 && comic.IsInProgress ? (comic.LastReadPage + 1) / (double)comic.PageCount : 0;
        string cover = ComicDialogXaml.ImageUri(comic.ThumbnailPath);
        string tagChips = tags.Count == 0
            ? @"<TextBlock Text=""No tags yet"" FontSize=""11"" Foreground=""{ThemeResource KomikSubtleTextBrush}"" />"
            : string.Concat(tags.Take(12).Select(t => $@"<Border Background=""#FFD700"" BorderBrush=""#0B0B12"" BorderThickness=""1.5"" CornerRadius=""10"" Padding=""8,1"" Margin=""0,0,5,5""><TextBlock Text=""{E(t)}"" FontSize=""11"" FontWeight=""Bold"" Foreground=""#0B0B12"" /></Border>"))
              + (tags.Count > 12 ? $@"<TextBlock Text=""+{tags.Count - 12} more"" FontSize=""11"" Margin=""2,2,0,0"" />" : string.Empty);

        string Field(string name, string header, string? value, string extra = "") =>
            $@"<TextBox x:Name=""{name}"" Header=""{header}"" Text=""{E(value)}"" HorizontalAlignment=""Stretch"" {extra} />";

        var root = ComicDialogXaml.Load($@"
<ScrollViewer {{NS}} VerticalScrollBarVisibility=""Auto"" MaxHeight=""600"" Padding=""0,0,12,0"">
<Grid ColumnSpacing=""20"">
  <Grid.ColumnDefinitions><ColumnDefinition Width=""190"" /><ColumnDefinition Width=""*"" /></Grid.ColumnDefinitions>
  <StackPanel Spacing=""10"">
    <Grid Width=""180"" Height=""260"" HorizontalAlignment=""Left"">
      <Border Margin=""7,7,0,0"" CornerRadius=""12"" Background=""{{ThemeResource KomikShadowBrush}}"" />
      <Grid Margin=""0,0,7,7"" CornerRadius=""12"" BorderBrush=""{{ThemeResource KomikInkStrokeBrush}}"" BorderThickness=""2.5"" Background=""{{ThemeResource KomikPanelSunkenBrush}}"">
        <FontIcon Glyph=""&#xE82D;"" FontSize=""36"" Foreground=""{{ThemeResource KomikSubtleTextBrush}}"" />
        {(cover.Length > 0 ? $@"<Image Source=""{cover}"" Stretch=""UniformToFill"" />" : string.Empty)}
        <Border HorizontalAlignment=""Left"" VerticalAlignment=""Top"" Margin=""8"" Background=""#FFD700"" BorderBrush=""#0B0B12"" BorderThickness=""1.5,1.5,2.5,2.5"" CornerRadius=""5"" Padding=""6,0"">
          <Border.RenderTransform><RotateTransform Angle=""-5"" /></Border.RenderTransform>
          <TextBlock Text=""{E(comic.FormatBadge)}"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""14"" CharacterSpacing=""40"" Foreground=""#0B0B12"" />
        </Border>
      </Grid>
    </Grid>
    <Border HorizontalAlignment=""Left"" Background=""{statusColor}"" BorderBrush=""#0B0B12"" BorderThickness=""2"" CornerRadius=""6"" Padding=""8,1"">
      <TextBlock Text=""{E(status)}"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""15"" CharacterSpacing=""40"" Foreground=""#0B0B12"" />
    </Border>
    <ProgressBar Value=""{progress.ToString(System.Globalization.CultureInfo.InvariantCulture)}"" Maximum=""1"" Height=""6"" MinHeight=""6"" CornerRadius=""3"" Foreground=""{{ThemeResource KomikYellowBrush}}"" Background=""{{ThemeResource KomikChartTrackBrush}}"" />
    <Grid ColumnSpacing=""6"" RowSpacing=""6"">
      <Grid.ColumnDefinitions><ColumnDefinition Width=""*"" /><ColumnDefinition Width=""*"" /></Grid.ColumnDefinitions>
      <Grid.RowDefinitions><RowDefinition /><RowDefinition /></Grid.RowDefinitions>
      <Border CornerRadius=""8"" Padding=""8,4"" Background=""{{ThemeResource KomikPanelSunkenBrush}}""><StackPanel><TextBlock Text=""PAGES"" FontSize=""9"" FontWeight=""Bold"" Foreground=""{{ThemeResource KomikSubtleTextBrush}}"" /><TextBlock Text=""{comic.PageCount}"" FontWeight=""Bold"" /></StackPanel></Border>
      <Border Grid.Column=""1"" CornerRadius=""8"" Padding=""8,4"" Background=""{{ThemeResource KomikPanelSunkenBrush}}""><StackPanel><TextBlock Text=""SIZE"" FontSize=""9"" FontWeight=""Bold"" Foreground=""{{ThemeResource KomikSubtleTextBrush}}"" /><TextBlock Text=""{E(comic.FileSizeFormatted)}"" FontWeight=""Bold"" /></StackPanel></Border>
      <Border Grid.Row=""1"" Grid.ColumnSpan=""2"" CornerRadius=""8"" Padding=""8,4"" Background=""{{ThemeResource KomikPanelSunkenBrush}}""><StackPanel><TextBlock Text=""ADDED"" FontSize=""9"" FontWeight=""Bold"" Foreground=""{{ThemeResource KomikSubtleTextBrush}}"" /><TextBlock Text=""{E(comic.DateAdded.ToLocalTime().ToString("d MMM yyyy"))}"" FontWeight=""Bold"" /></StackPanel></Border>
    </Grid>
    {(comic.IsMissing ? @"<Border Background=""#D9A80000"" CornerRadius=""6"" Padding=""8,3""><TextBlock Text=""File is offline or missing"" Foreground=""White"" FontSize=""11"" FontWeight=""Bold"" /></Border>" : string.Empty)}
  </StackPanel>
  <StackPanel Grid.Column=""1"" Spacing=""12"">
    <Border CornerRadius=""12"" Padding=""14,10,14,14"" Background=""{{ThemeResource KomikPanelSunkenBrush}}"" BorderBrush=""{{ThemeResource KomikPanelStrokeBrush}}"" BorderThickness=""1"">
      <StackPanel Spacing=""8"">
        <TextBlock Text=""STORY"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""17"" CharacterSpacing=""40"" Foreground=""{{ThemeResource KomikCyanBrush}}"" />
        {Field("TitleBox", "Title", metadata?.Title ?? comic.Title)}
        <Grid ColumnSpacing=""8""><Grid.ColumnDefinitions><ColumnDefinition Width=""3*"" /><ColumnDefinition Width=""*"" /></Grid.ColumnDefinitions>
          {Field("SeriesBox", "Series", metadata?.SeriesName)}
          <TextBox x:Name=""IssueBox"" Grid.Column=""1"" Header=""Issue #"" Text=""{E(metadata?.IssueNumber)}"" />
        </Grid>
        <TextBox x:Name=""SummaryBox"" Header=""Summary"" Text=""{E(metadata?.Summary)}"" AcceptsReturn=""True"" TextWrapping=""Wrap"" MinHeight=""80"" MaxHeight=""140"" />
      </StackPanel>
    </Border>
    <Border CornerRadius=""12"" Padding=""14,10,14,14"" Background=""{{ThemeResource KomikPanelSunkenBrush}}"" BorderBrush=""{{ThemeResource KomikPanelStrokeBrush}}"" BorderThickness=""1"">
      <StackPanel Spacing=""8"">
        <TextBlock Text=""CREDITS &amp; PUBLICATION"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""17"" CharacterSpacing=""40"" Foreground=""{{ThemeResource KomikMagentaBrush}}"" />
        <Grid ColumnSpacing=""8""><Grid.ColumnDefinitions><ColumnDefinition /><ColumnDefinition /></Grid.ColumnDefinitions>
          {Field("WriterBox", "Writer(s)", metadata?.Writers)}
          <TextBox x:Name=""ArtistBox"" Grid.Column=""1"" Header=""Artist(s)"" Text=""{E(metadata?.Artists)}"" />
        </Grid>
        <Grid ColumnSpacing=""8""><Grid.ColumnDefinitions><ColumnDefinition Width=""2*"" /><ColumnDefinition Width=""*"" /></Grid.ColumnDefinitions>
          {Field("PublisherBox", "Publisher", metadata?.Publisher)}
          <TextBox x:Name=""DateBox"" Grid.Column=""1"" Header=""Year / Date"" Text=""{E(metadata?.ReleaseDate)}"" />
        </Grid>
      </StackPanel>
    </Border>
    <Border CornerRadius=""12"" Padding=""14,10"" Background=""{{ThemeResource KomikPanelSunkenBrush}}"" BorderBrush=""{{ThemeResource KomikPanelStrokeBrush}}"" BorderThickness=""1"">
      <Grid ColumnSpacing=""10"" RowSpacing=""6"">
        <Grid.ColumnDefinitions><ColumnDefinition Width=""*"" /><ColumnDefinition Width=""Auto"" /></Grid.ColumnDefinitions>
        <Grid.RowDefinitions><RowDefinition /><RowDefinition /></Grid.RowDefinitions>
        <TextBlock Text=""TAGS"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""17"" CharacterSpacing=""40"" Foreground=""{{ThemeResource KomikOrangeBrush}}"" VerticalAlignment=""Center"" />
        <Button x:Name=""EditTagsButton"" Grid.Column=""1"" Padding=""10,4"" Content=""Edit tags"" />
        <VariableSizedWrapGrid Grid.Row=""1"" Grid.ColumnSpan=""2"" Orientation=""Horizontal"" ItemHeight=""26"">{tagChips}</VariableSizedWrapGrid>
      </Grid>
    </Border>
    <Border CornerRadius=""12"" Padding=""14,10"" Background=""{{ThemeResource KomikPanelSunkenBrush}}"" BorderBrush=""{{ThemeResource KomikPanelStrokeBrush}}"" BorderThickness=""1"">
      <Grid ColumnSpacing=""10"">
        <Grid.ColumnDefinitions><ColumnDefinition Width=""*"" /><ColumnDefinition Width=""Auto"" /></Grid.ColumnDefinitions>
        <StackPanel>
          <TextBlock Text=""FILE"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""17"" CharacterSpacing=""40"" />
          <TextBlock Text=""{E(comic.FilePath)}"" FontSize=""11"" Foreground=""{{ThemeResource KomikSubtleTextBrush}}"" TextWrapping=""Wrap"" IsTextSelectionEnabled=""True"" />
        </StackPanel>
        <Button x:Name=""ExplorerButton"" Grid.Column=""1"" VerticalAlignment=""Center"" Padding=""10,4"" Content=""Show in Explorer"" />
      </Grid>
    </Border>
  </StackPanel>
</Grid>
</ScrollViewer>");

        TextBox Box(string name) => (TextBox)root.FindName(name);

        var dialog = ComicDialogXaml.Create(XamlRoot, ComicDialogXaml.Header("DETAILS", "#FFD700", "#0B0B12", comic.Title), root, 900);
        dialog.PrimaryButtonText = "Save";
        dialog.SecondaryButtonText = "Save & Read";
        dialog.CloseButtonText = "Cancel";
        dialog.DefaultButton = ContentDialogButton.Primary;

        bool openTags = false;
        ((Button)root.FindName("EditTagsButton")).Click += (_, _) => { openTags = true; dialog.Hide(); };
        ((Button)root.FindName("ExplorerButton")).Click += (_, _) => ViewModel.ShowInExplorer(comic);

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary || result == ContentDialogResult.Secondary)
        {
            static string? Clean(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            var updated = metadata ?? new ComicMetadataEntity { ComicId = comic.Id };
            updated.Title = Clean(Box("TitleBox").Text) ?? comic.Title;
            updated.SeriesName = Clean(Box("SeriesBox").Text);
            updated.IssueNumber = Clean(Box("IssueBox").Text);
            updated.Writers = Clean(Box("WriterBox").Text);
            updated.Artists = Clean(Box("ArtistBox").Text);
            updated.Publisher = Clean(Box("PublisherBox").Text);
            updated.ReleaseDate = Clean(Box("DateBox").Text);
            updated.Summary = Clean(Box("SummaryBox").Text);
            updated.LastUpdated = DateTime.UtcNow;

            await ViewModel.SaveComicMetadataAsync(updated, comic);
            RefreshSeriesIfShown();

            if (result == ContentDialogResult.Secondary) OnComicSelectedForReading(comic.FilePath);
            else ViewModel.ShowNotification("Details Saved", $"Updated details for '{comic.Title}'.", InfoBarSeverity.Success);
        }
        else if (openTags)
        {
            await ViewModel.OpenComicTagsAsync(comic);
        }
    }

    #endregion

    #region Duplicate Detective dialogs

    private static string CopyFacts(DuplicateCopyItem item) => $"{item.FormatBadge} · {item.PageCountFormatted} · {item.FileSizeFormatted} · {item.ProgressText}";

    private static FrameworkElement ChoiceCards(string groupName, (string Name, string Glyph, string Title, string Body, bool Checked)[] choices)
    {
        string cards = string.Concat(choices.Select(c => $@"
    <RadioButton x:Name=""{c.Name}"" GroupName=""{groupName}"" IsChecked=""{(c.Checked ? "True" : "False")}"" HorizontalAlignment=""Stretch"" Padding=""8,6"" Margin=""0,0,0,6"">
      <Grid ColumnSpacing=""10"">
        <Grid.ColumnDefinitions><ColumnDefinition Width=""Auto"" /><ColumnDefinition Width=""*"" /></Grid.ColumnDefinitions>
        <FontIcon Glyph=""{c.Glyph}"" FontSize=""18"" VerticalAlignment=""Center"" />
        <StackPanel Grid.Column=""1"">
          <TextBlock Text=""{ComicDialogXaml.E(c.Title)}"" FontWeight=""Bold"" FontSize=""13"" />
          <TextBlock Text=""{ComicDialogXaml.E(c.Body)}"" FontSize=""11"" Foreground=""{{ThemeResource KomikSubtleTextBrush}}"" TextWrapping=""Wrap"" />
        </StackPanel>
      </Grid>
    </RadioButton>"));
        return ComicDialogXaml.Load($@"<StackPanel {{NS}}>{cards}</StackPanel>");
    }

    private async void ResolveDuplicateGroup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: DuplicateComicGroup group }) return;
        var keep = group.CopyItems.FirstOrDefault(c => c.IsRecommended);
        if (keep == null) return;
        var extras = group.CopyItems.Where(c => !c.IsRecommended).ToList();

        string extraCards = string.Concat(extras.Select(x => ComicDialogXaml.CopyCard(x.ThumbnailPath, x.Title, CopyFacts(x), x.FilePath, "REMOVE", "#FF1F6D", "#FFFFFF", struck: true)));
        var body = ComicDialogXaml.Load($@"
<ScrollViewer {{NS}} VerticalScrollBarVisibility=""Auto"" MaxHeight=""520"" Padding=""0,0,10,0"">
  <StackPanel Spacing=""10"">
    <TextBlock Text=""KEEP"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""17"" CharacterSpacing=""40"" Foreground=""{{ThemeResource KomikGreenBrush}}"" />
    {ComicDialogXaml.CopyCard(keep.ThumbnailPath, keep.Title, CopyFacts(keep), keep.FilePath, "BEST COPY", "#2EE59D", "#0B0B12")}
    <TextBlock Text=""{(extras.Count == 1 ? "REMOVE 1 EXTRA COPY" : $"REMOVE {extras.Count} EXTRA COPIES")}"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""17"" CharacterSpacing=""40"" Foreground=""{{ThemeResource KomikMagentaBrush}}"" Margin=""0,4,0,0"" />
    <StackPanel Spacing=""8"">{extraCards}</StackPanel>
    <Border CornerRadius=""10"" Padding=""10,6"" Background=""{{ThemeResource KomikPanelSunkenBrush}}"">
      <StackPanel Orientation=""Horizontal"" Spacing=""8""><FontIcon Glyph=""&#xE895;"" FontSize=""13"" Foreground=""{{ThemeResource KomikCyanBrush}}"" /><TextBlock Text=""Reading progress and favorites carry over to the kept copy."" FontSize=""12"" TextWrapping=""Wrap"" /></StackPanel>
    </Border>
  </StackPanel>
</ScrollViewer>");

        var choices = ChoiceCards("keepbest", new[]
        {
            ("RecycleChoice", "", "Delete the extra files", "They go to the Recycle Bin, so you can still restore them.", true),
            ("LibraryOnlyChoice", "", "Only remove them from the library", "The files stay on disk. You can add them back from Settings.", false)
        });
        ((StackPanel)((ScrollViewer)body).Content).Children.Add(choices);

        var dialog = ComicDialogXaml.Create(XamlRoot, ComicDialogXaml.Header("CASE FILE", "#FF9F1C", "#0B0B12", "Keep the best copy?", group.GroupTitle), body, 680);
        dialog.PrimaryButtonText = "Keep Best Copy";
        dialog.CloseButtonText = "Cancel";
        dialog.DefaultButton = ContentDialogButton.Primary;

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            bool recycle = ((RadioButton)choices.FindName("RecycleChoice")).IsChecked == true;
            await ViewModel.ResolveDuplicateGroupAsync(group, recycle);
        }
    }

    private async void ResolveAllDuplicates_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.HasDuplicates) return;

        int high = ViewModel.DuplicateGroups.Count(g => g.IsHighConfidence);
        int total = ViewModel.DuplicateGroups.Count;

        var body = new StackPanel { Spacing = 12 };
        body.Children.Add(ComicDialogXaml.Load($@"
<Grid {{NS}} ColumnSpacing=""8"">
  <Grid.ColumnDefinitions><ColumnDefinition /><ColumnDefinition /><ColumnDefinition /></Grid.ColumnDefinitions>
  <Border CornerRadius=""10"" Padding=""10,6"" Background=""#FFD700"" BorderBrush=""#0B0B12"" BorderThickness=""2,2,3,3""><StackPanel><TextBlock Text=""{total}"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""26"" Foreground=""#0B0B12"" /><TextBlock Text=""GROUPS"" FontSize=""10"" FontWeight=""Bold"" Foreground=""#0B0B12"" /></StackPanel></Border>
  <Border Grid.Column=""1"" CornerRadius=""10"" Padding=""10,6"" Background=""#FF1F6D"" BorderBrush=""#0B0B12"" BorderThickness=""2,2,3,3""><StackPanel><TextBlock Text=""{ViewModel.DuplicateExtraCopies}"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""26"" Foreground=""White"" /><TextBlock Text=""EXTRA COPIES"" FontSize=""10"" FontWeight=""Bold"" Foreground=""White"" /></StackPanel></Border>
  <Border Grid.Column=""2"" CornerRadius=""10"" Padding=""10,6"" Background=""#2EE59D"" BorderBrush=""#0B0B12"" BorderThickness=""2,2,3,3""><StackPanel><TextBlock Text=""{high}"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""26"" Foreground=""#0B0B12"" /><TextBlock Text=""SURE MATCHES"" FontSize=""10"" FontWeight=""Bold"" Foreground=""#0B0B12"" /></StackPanel></Border>
</Grid>"));
        body.Children.Add(ComicDialogXaml.Load(@"<TextBlock {NS} Text=""Komik keeps the best copy in each group (reading progress, favorites, page count and format decide) and removes the rest."" TextWrapping=""Wrap"" FontSize=""13"" />"));

        var scope = ChoiceCards("dupscope", new[]
        {
            ("OnlyHighChoice", "", $"Only sure matches ({high} group{(high == 1 ? "" : "s")})", "Identical files and the same issue in another format.", true),
            ("EverythingChoice", "", $"Every group, including possible matches ({total})", "Also near-identical titles. Check them first if unsure.", false)
        });
        var mode = ChoiceCards("dupmode", new[]
        {
            ("RecycleAllChoice", "", "Delete the extra files", "They go to the Recycle Bin, so you can still restore them.", true),
            ("LibraryOnlyAllChoice", "", "Only remove them from the library", "The files stay on disk. You can add them back from Settings.", false)
        });
        body.Children.Add(scope);
        body.Children.Add(mode);

        var dialog = ComicDialogXaml.Create(XamlRoot, ComicDialogXaml.Header("CLEAN UP", "#FF9F1C", "#0B0B12", "Clean up duplicates?"), new ScrollViewer { Content = body, MaxHeight = 560 }, 640);
        dialog.PrimaryButtonText = "Clean Up";
        dialog.CloseButtonText = "Cancel";
        dialog.DefaultButton = ContentDialogButton.Primary;

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            bool recycle = ((RadioButton)mode.FindName("RecycleAllChoice")).IsChecked == true;
            bool onlyHigh = ((RadioButton)scope.FindName("OnlyHighChoice")).IsChecked == true;
            await ViewModel.ResolveAllDuplicatesAsync(recycle, highConfidenceOnly: onlyHigh);
        }
    }

    private async void RemoveDuplicateCopy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: DuplicateCopyItem item }) return;

        var body = new StackPanel { Spacing = 12 };
        body.Children.Add(ComicDialogXaml.Load($@"<StackPanel {{NS}}>{ComicDialogXaml.CopyCard(item.ThumbnailPath, item.Title, CopyFacts(item), item.FilePath, item.IsRecommended ? "BEST COPY" : "THIS COPY", item.IsRecommended ? "#2EE59D" : "#FFD700", "#0B0B12")}</StackPanel>"));
        if (item.IsRecommended)
        {
            body.Children.Add(ComicDialogXaml.Load(@"<Border {NS} CornerRadius=""10"" Padding=""10,6"" Background=""#33FF9F1C"" BorderBrush=""#FF9F1C"" BorderThickness=""1.5""><TextBlock Text=""This is the copy Komik suggests keeping. The next best copy becomes the one to keep."" FontSize=""12"" TextWrapping=""Wrap"" /></Border>"));
        }
        var choices = ChoiceCards("removecopy", new[]
        {
            ("RecycleOneChoice", "", "Delete the file", "Moves it to the Recycle Bin and removes it from the library.", true),
            ("LibraryOnlyOneChoice", "", "Only remove it from the library", "The file stays on disk. You can add it back from Settings.", false)
        });
        body.Children.Add(choices);
        body.Children.Add(ComicDialogXaml.Load(@"<TextBlock {NS} Text=""Its reading progress and favorite carry over to the best remaining copy."" FontSize=""12"" TextWrapping=""Wrap"" Foreground=""{ThemeResource KomikSubtleTextBrush}"" />"));

        var dialog = ComicDialogXaml.Create(XamlRoot, ComicDialogXaml.Header("REMOVE", "#FF1F6D", "#FFFFFF", "Remove this copy?"), body, 600);
        dialog.PrimaryButtonText = "Remove Copy";
        dialog.CloseButtonText = "Cancel";
        dialog.DefaultButton = ContentDialogButton.Primary;

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            bool recycle = ((RadioButton)choices.FindName("RecycleOneChoice")).IsChecked == true;
            await ViewModel.RemoveDuplicateCopyAsync(item.Comic, deleteFile: recycle);
        }
    }

    #endregion
}
