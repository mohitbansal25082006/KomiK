using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Komik.Services;

namespace Komik;

/// <summary>
/// The application window. Hosts TitleBar and Frame for MainPage.
/// </summary>
public sealed partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; } = null!;

    public MainWindow() : this(null) { }

    public MainWindow(string? initialComicPath)
    {
        Instance = this;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        try
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(iconPath))
            {
                AppWindow.SetIcon(iconPath);
            }
        }
        catch { }

        // Immediately navigate to the appropriate page
        if (!string.IsNullOrWhiteSpace(initialComicPath) && (File.Exists(initialComicPath) || Directory.Exists(initialComicPath)))
        {
            RootFrame.Navigate(typeof(MainPage), initialComicPath);
        }
        else
        {
            RootFrame.Navigate(typeof(LibraryPage));
        }

        // Apply saved theme preference
        _ = ApplyStartupThemeAsync();
    }

    private async Task ApplyStartupThemeAsync()
    {
        try
        {
            var repo = new LibraryRepository();
            await repo.InitializeAsync();
            var settings = await repo.GetAppSettingsAsync();

            if (Content is FrameworkElement root)
            {
                root.RequestedTheme = settings.Theme switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to load startup theme: {ex.Message}");
        }
    }

    public void SetTheme(string theme)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }

    public void ToggleFullscreen(bool isFullscreen)
    {
        if (isFullscreen)
        {
            AppTitleBar.Visibility = Visibility.Collapsed;
            AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
        else
        {
            AppTitleBar.Visibility = Visibility.Visible;
            AppWindow.SetPresenter(AppWindowPresenterKind.Default);
        }
    }

    public void UpdateTitle(string title)
    {
        Title = string.IsNullOrWhiteSpace(title) || title == "Komik" ? "Komik" : $"{title} — Komik";
        TitleTextBlock.Text = Title;
    }
}
