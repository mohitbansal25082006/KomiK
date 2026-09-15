using System;
using System.IO;
using System.Security;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Komik.Helpers;

/// <summary>
/// Builds comic-styled dialog content from XAML snippets, so theme brushes ({ThemeResource}) follow light and dark
/// exactly like the pages do.
/// </summary>
public static class ComicDialogXaml
{
    private const string Ns = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"";

    public static string E(string? text) => SecurityElement.Escape(text ?? string.Empty) ?? string.Empty;

    public static FrameworkElement Load(string xaml) => (FrameworkElement)XamlReader.Load(xaml.Replace("{NS}", Ns));

    public static string ImageUri(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return string.Empty;
        try { return E(new Uri(path).AbsoluteUri); } catch { return string.Empty; }
    }

    /// <summary>Tilted sticker + big comic lettering + optional subtitle.</summary>
    public static FrameworkElement Header(string sticker, string stickerBackground, string stickerForeground, string title, string? subtitle = null) => Load($@"
<StackPanel {{NS}} Spacing=""4"">
  <StackPanel Orientation=""Horizontal"" Spacing=""10"">
    <Border Background=""{stickerBackground}"" BorderBrush=""#0B0B12"" BorderThickness=""2"" CornerRadius=""6"" Padding=""7,1"" VerticalAlignment=""Center"">
      <Border.RenderTransform><RotateTransform Angle=""-5"" /></Border.RenderTransform>
      <TextBlock Text=""{E(sticker)}"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""14"" CharacterSpacing=""60"" Foreground=""{stickerForeground}"" />
    </Border>
    <TextBlock Text=""{E(title)}"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""28"" CharacterSpacing=""30"" TextWrapping=""Wrap"" VerticalAlignment=""Center"" />
  </StackPanel>
  {(string.IsNullOrEmpty(subtitle) ? string.Empty : $@"<TextBlock Text=""{E(subtitle)}"" FontSize=""12"" Foreground=""{{ThemeResource KomikSubtleTextBrush}}"" TextWrapping=""Wrap"" />")}
</StackPanel>");

    /// <summary>A copy of a comic: cover, sticker, title, facts and where the file lives.</summary>
    public static string CopyCard(string? thumbnail, string title, string facts, string path, string badge, string badgeBackground, string badgeForeground, bool struck = false) => $@"
<Grid ColumnSpacing=""12"" Padding=""10"" CornerRadius=""12"" Background=""{{ThemeResource KomikPanelSunkenBrush}}"" BorderBrush=""{{ThemeResource KomikInkStrokeBrush}}"" BorderThickness=""1.5,1.5,3,3"">
  <Grid.ColumnDefinitions><ColumnDefinition Width=""Auto"" /><ColumnDefinition Width=""*"" /></Grid.ColumnDefinitions>
  <Grid Width=""52"" Height=""74"" CornerRadius=""6"" Background=""{{ThemeResource KomikPanelRaisedBrush}}"" BorderBrush=""{{ThemeResource KomikInkStrokeBrush}}"" BorderThickness=""1.5"">
    <FontIcon Glyph=""&#xE82D;"" FontSize=""18"" Foreground=""{{ThemeResource KomikSubtleTextBrush}}"" />
    {(ImageUri(thumbnail) is { Length: > 0 } uri ? $@"<Image Source=""{uri}"" Stretch=""UniformToFill"" Opacity=""{(struck ? "0.55" : "1")}"" />" : string.Empty)}
  </Grid>
  <StackPanel Grid.Column=""1"" Spacing=""3"" VerticalAlignment=""Center"">
    <Border HorizontalAlignment=""Left"" Background=""{badgeBackground}"" BorderBrush=""#0B0B12"" BorderThickness=""1.5"" CornerRadius=""5"" Padding=""6,0"">
      <TextBlock Text=""{E(badge)}"" FontFamily=""{{StaticResource KomikDisplayFont}}"" FontSize=""13"" CharacterSpacing=""50"" Foreground=""{badgeForeground}"" />
    </Border>
    <TextBlock Text=""{E(title)}"" FontWeight=""Bold"" FontSize=""13"" TextTrimming=""CharacterEllipsis"" />
    <TextBlock Text=""{E(facts)}"" FontSize=""11"" Foreground=""{{ThemeResource KomikCyanBrush}}"" TextTrimming=""CharacterEllipsis"" />
    <TextBlock Text=""{E(path)}"" FontSize=""10"" Foreground=""{{ThemeResource KomikSubtleTextBrush}}"" TextTrimming=""CharacterEllipsis"" />
  </StackPanel>
</Grid>";

    /// <summary>A roomy comic dialog (the default one is too narrow for covers and cards).</summary>
    public static ContentDialog Create(XamlRoot root, object header, UIElement content, double maxWidth = 640)
    {
        var dialog = new ContentDialog
        {
            Title = header,
            Content = content,
            XamlRoot = root,
            RequestedTheme = (root.Content as FrameworkElement)?.ActualTheme ?? ElementTheme.Default
        };
        dialog.Resources["ContentDialogMaxWidth"] = maxWidth;
        dialog.Resources["ContentDialogMaxHeight"] = 900.0;
        return dialog;
    }
}
