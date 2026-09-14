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

        // Apply saved theme and restore window geometry/ratio preference
        _ = ApplyStartupThemeAndGeometryAsync();

        AppWindow.Changed += AppWindow_Changed;
        AppWindow.Closing += AppWindow_Closing;
    }

    private int _savedWidth = 1240;
    private int _savedHeight = 820;
    private int _savedX = -1;
    private int _savedY = -1;
    private bool _savedIsMaximized = false;

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (sender.Presenter is OverlappedPresenter presenter)
        {
            if (presenter.State == OverlappedPresenterState.Maximized)
            {
                _savedIsMaximized = true;
            }
            else if (presenter.State == OverlappedPresenterState.Restored)
            {
                _savedIsMaximized = false;
                if (sender.Size.Width > 300 && sender.Size.Height > 300)
                {
                    _savedWidth = sender.Size.Width;
                    _savedHeight = sender.Size.Height;
                    _savedX = sender.Position.X;
                    _savedY = sender.Position.Y;
                }
            }
        }
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        try
        {
            var repo = new LibraryRepository();
            var settings = await repo.GetAppSettingsAsync();
            settings.IsMaximized = _savedIsMaximized;
            settings.WindowWidth = _savedWidth;
            settings.WindowHeight = _savedHeight;
            settings.WindowX = _savedX;
            settings.WindowY = _savedY;
            await repo.SaveAppSettingsAsync(settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to save window geometry: {ex.Message}");
        }
    }

    private async Task ApplyStartupThemeAndGeometryAsync()
    {
        try
        {
            var repo = new LibraryRepository();
            await repo.InitializeAsync();
            var settings = await repo.GetAppSettingsAsync();

            // Restore theme
            if (Content is FrameworkElement root)
            {
                root.RequestedTheme = settings.Theme switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }

            // Restore window ratio, size, and maximized state
            _savedIsMaximized = settings.IsMaximized;
            _savedWidth = settings.WindowWidth > 300 ? settings.WindowWidth : 1240;
            _savedHeight = settings.WindowHeight > 300 ? settings.WindowHeight : 820;
            _savedX = settings.WindowX;
            _savedY = settings.WindowY;

            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                if (settings.IsMaximized)
                {
                    presenter.Maximize();
                }
                else
                {
                    AppWindow.Resize(new Windows.Graphics.SizeInt32(_savedWidth, _savedHeight));
                    if (_savedX >= 0 && _savedY >= 0)
                    {
                        AppWindow.Move(new Windows.Graphics.PointInt32(_savedX, _savedY));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to load startup theme/geometry: {ex.Message}");
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
