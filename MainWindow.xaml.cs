using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;
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
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }
        catch { }
        if (Content is FrameworkElement themedRoot)
        {
            themedRoot.ActualThemeChanged += (_, _) => ApplyCaptionButtonColors();
        }
        ApplyCaptionButtonColors();

        try
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(iconPath))
            {
                AppWindow.SetIcon(iconPath);
            }
        }
        catch { }

        // Window-wide keyboard shortcuts (F11 / Esc full screen) for every page, including Library and Settings.
        // PreviewKeyDown tunnels from the root before focused controls (search boxes, lists) can swallow Esc.
        // Flyouts and dialogs live in their own popup trees, so Esc still closes them first.
        if (Content is UIElement rootElement)
        {
            rootElement.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(Root_PreviewKeyDown), handledEventsToo: true);
        }
        RootFrame.Navigated += RootFrame_Navigated;

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
        if (args.DidPresenterChange)
        {
            SyncFullscreenState();
        }
        if (args.DidSizeChange || args.DidPresenterChange)
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, UpdateTitleBarRegions);
        }

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

    /// <summary>
    /// Raised before the window closes so pages can persist in-flight state (e.g. the active reading session).
    /// </summary>
    public event Func<Task>? ClosingAsync;

    private bool _closeConfirmed;

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_closeConfirmed) return;

        // Defer the close until async persistence has finished, then close for real.
        args.Cancel = true;
        _closeConfirmed = true;

        if (ClosingAsync != null)
        {
            foreach (var handler in ClosingAsync.GetInvocationList())
            {
                try
                {
                    await ((Func<Task>)handler)();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Closing handler failed: {ex.Message}");
                }
            }
        }

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

        Close();
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

    #region Full Screen

    /// <summary>Raised whenever the window enters or leaves full screen (from any page, shortcut or the system).</summary>
    public event Action<bool>? FullscreenChanged;

    private bool _lastKnownFullscreen;

    public bool IsFullscreen => AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen;

    public void ToggleFullscreen(bool isFullscreen) => SetFullscreen(isFullscreen);

    public void SetFullscreen(bool isFullscreen)
    {
        if (isFullscreen == IsFullscreen)
        {
            SyncFullscreenState();
            return;
        }

        AppWindow.SetPresenter(isFullscreen ? AppWindowPresenterKind.FullScreen : AppWindowPresenterKind.Default);
        SyncFullscreenState();
    }

    private void SyncFullscreenState()
    {
        bool fullscreen = IsFullscreen;
        AppTitleBar.Visibility = fullscreen ? Visibility.Collapsed : Visibility.Visible;

        if (fullscreen == _lastKnownFullscreen) return;
        _lastKnownFullscreen = fullscreen;

        if (fullscreen)
        {
            ShowFullscreenHint();
        }
        else
        {
            FullscreenHint.Visibility = Visibility.Collapsed;
        }

        FullscreenChanged?.Invoke(fullscreen);
    }

    private void Root_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled) return;

        if (e.Key == VirtualKey.Escape && IsFullscreen)
        {
            SetFullscreen(false);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.F11)
        {
            SetFullscreen(!IsFullscreen);
            e.Handled = true;
        }
    }

    private void ExitFullscreenHint_Click(object sender, RoutedEventArgs e) => SetFullscreen(false);

    private DispatcherTimer? _hintTimer;

    private void ShowFullscreenHint()
    {
        FullscreenHint.Visibility = Visibility.Visible;
        FullscreenHintShow.Begin();

        _hintTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.6) };
        _hintTimer.Stop();
        _hintTimer.Tick -= HintTimer_Tick;
        _hintTimer.Tick += HintTimer_Tick;
        _hintTimer.Start();
    }

    private void HintTimer_Tick(object? sender, object e)
    {
        _hintTimer?.Stop();
        FullscreenHintHide.Begin();
    }

    private void FullscreenHintHide_Completed(object? sender, object e)
    {
        FullscreenHint.Visibility = Visibility.Collapsed;
    }

    private void RootFrame_Navigated(object sender, NavigationEventArgs e)
    {
        // Pages without a focused element would never receive key events (so Esc/F11 would do nothing).
        // Give the new page keyboard focus unless something inside it already took it.
        if (e.Content is not Page page) return;
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (page.XamlRoot == null) return;
            if (FocusManager.GetFocusedElement(page.XamlRoot) is DependencyObject focused && IsDescendantOf(focused, page)) return;

            if (!page.IsTabStop)
            {
                page.IsTabStop = true;
                page.UseSystemFocusVisuals = false;
            }
            page.Focus(FocusState.Programmatic);
        });
    }

    private static bool IsDescendantOf(DependencyObject node, DependencyObject ancestor)
    {
        for (var current = node; current != null; current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, ancestor)) return true;
        }
        return false;
    }

    #endregion

    /// <summary>Caption buttons (minimize / maximize / close) follow the app theme so they stay visible in light mode.</summary>
    private void ApplyCaptionButtonColors()
    {
        try
        {
            bool light = Content is FrameworkElement root && root.ActualTheme == ElementTheme.Light;
            var tb = AppWindow.TitleBar;
            var fg = light ? Windows.UI.Color.FromArgb(255, 0x11, 0x11, 0x18) : Windows.UI.Color.FromArgb(255, 0xF2, 0xF2, 0xF7);
            tb.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            tb.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
            tb.ButtonForegroundColor = fg;
            tb.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 0x0B, 0x0B, 0x12);
            tb.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(255, 0xFF, 0xD7, 0x00);
            tb.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 0x0B, 0x0B, 0x12);
            tb.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(255, 0x00, 0xC2, 0xFF);
            tb.ButtonInactiveForegroundColor = light ? Windows.UI.Color.FromArgb(255, 0x8A, 0x8A, 0x99) : Windows.UI.Color.FromArgb(255, 0x7A, 0x7A, 0x8C);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Caption colors failed: {ex.Message}");
        }
    }

    #region Title bar content

    private FrameworkElement[] _titleBarInteractive = Array.Empty<FrameworkElement>();

    /// <summary>
    /// Shows page content inside the window title bar (or restores the plain title when null).
    /// <paramref name="interactive"/> elements stay clickable; the rest of the bar still drags the window.
    /// </summary>
    public void SetTitleBarContent(FrameworkElement? content, params FrameworkElement[] interactive)
    {
        foreach (var old in _titleBarInteractive) old.SizeChanged -= TitleBarElement_SizeChanged;
        TitleBarHost.Content = content;
        TitleTextBlock.Visibility = content == null ? Visibility.Visible : Visibility.Collapsed;
        _titleBarInteractive = interactive ?? Array.Empty<FrameworkElement>();
        foreach (var el in _titleBarInteractive) el.SizeChanged += TitleBarElement_SizeChanged;
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, UpdateTitleBarRegions);
    }

    private void TitleBarElement_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateTitleBarRegions();

    private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateTitleBarRegions();

    private void UpdateTitleBarRegions()
    {
        try
        {
            if (AppTitleBar.XamlRoot == null) return;
            double scale = AppTitleBar.XamlRoot.RasterizationScale;
            if (scale > 0)
            {
                CaptionButtonsColumn.Width = new GridLength(Math.Max(0, AppWindow.TitleBar.RightInset / scale));
            }

            var rects = new System.Collections.Generic.List<Windows.Graphics.RectInt32>();
            if (TitleBarHost.Content != null)
            {
                foreach (var el in _titleBarInteractive)
                {
                    if (el.Visibility != Visibility.Visible || el.ActualWidth <= 0 || el.XamlRoot == null) continue;
                    var bounds = el.TransformToVisual(null).TransformBounds(new Windows.Foundation.Rect(0, 0, el.ActualWidth, el.ActualHeight));
                    rects.Add(new Windows.Graphics.RectInt32(
                        (int)Math.Round(bounds.X * scale), (int)Math.Round(bounds.Y * scale),
                        (int)Math.Round(bounds.Width * scale), (int)Math.Round(bounds.Height * scale)));
                }
            }
            Microsoft.UI.Input.InputNonClientPointerSource.GetForWindowId(AppWindow.Id)
                .SetRegionRects(Microsoft.UI.Input.NonClientRegionKind.Passthrough, rects.ToArray());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Title bar regions failed: {ex.Message}");
        }
    }

    #endregion

    public void UpdateTitle(string title)
    {
        Title = string.IsNullOrWhiteSpace(title) || title == "Komik" ? "Komik" : $"{title} — Komik";
        TitleTextBlock.Text = Title;
    }
}
