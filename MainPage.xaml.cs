using System;
using System.ComponentModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Komik.Models;
using Komik.ViewModels;

namespace Komik;

/// <summary>
/// The main reading page hosting canvas, controls, gestures, and animations.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = new();

    private readonly DispatcherTimer _autoHideTimer = new();
    private bool _isPointerOverToolbar;
    private bool _isPanning;
    private Point _panStartPoint;
    private double _panStartHOffset;
    private double _panStartVOffset;
    private bool _hasMovedSignificantly;
    private bool _isScrubbing;
    private bool _isUserScrollingWebtoon;
    private bool _isProgrammaticScroll;

    /// <summary>Where the reader's Back button returns to (set by the Library before opening a comic).</summary>
    public static string ReturnLabel { get; set; } = "Library";

    public string BackButtonText => string.IsNullOrWhiteSpace(ReturnLabel) ? "Back" : ReturnLabel;

    public MainPage()
    {
        InitializeComponent();

        _autoHideTimer.Interval = TimeSpan.FromSeconds(2.5);
        _autoHideTimer.Tick += AutoHideTimer_Tick;

        Loaded += MainPage_Loaded;
        Unloaded += MainPage_Unloaded;

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.ComicOpened += AnchorWebtoonPage;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Full screen is owned by the window (it can be toggled from any page), so mirror it here.
        if (MainWindow.Instance is { } window)
        {
            ViewModel.IsFullscreen = window.IsFullscreen;
            window.FullscreenChanged += Window_FullscreenChanged;
            window.ClosingAsync += Window_ClosingAsync;
        }

        await ViewModel.ApplyDefaultSettingsAsync();
        UpdateLayoutForFitMode();
        if (e.Parameter is string filePath && !string.IsNullOrWhiteSpace(filePath))
        {
            await ViewModel.LoadComicPathAsync(filePath);
        }
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        if (MainWindow.Instance is { } window)
        {
            window.FullscreenChanged -= Window_FullscreenChanged;
            window.ClosingAsync -= Window_ClosingAsync;
        }
        ViewModel.StopSessionTracking();
        var flush = ViewModel.FlushReadingProgressAsync();
        // Close the archive as soon as progress is saved so the file isn't locked (e.g. for duplicate cleanup).
        _ = flush.ContinueWith(_ => DispatcherQueue.TryEnqueue(ViewModel.CloseComic), TaskScheduler.Default);
    }

    private void Window_FullscreenChanged(bool isFullscreen)
    {
        if (ViewModel.IsFullscreen != isFullscreen)
        {
            ViewModel.IsFullscreen = isFullscreen;
        }
    }

    private System.Threading.Tasks.Task Window_ClosingAsync() => ViewModel.FlushReadingProgressAsync();

    /// <summary>Back to exactly where the comic was opened (series detail, series section or library).</summary>
    private async void ReaderBack_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.FlushReadingProgressAsync();
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
        else
        {
            Frame.Navigate(typeof(LibraryPage));
        }
    }

    /// <summary>Keeps the floating toolbar inside the window on any screen size by dropping labels as it narrows.</summary>
    private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        double w = e.NewSize.Width;
        TopToolbar.MaxWidth = Math.Max(320, w - 24);
        ReaderTitleBox.Visibility = w >= 1480 ? Visibility.Visible : Visibility.Collapsed;
        ReaderTitleBox.MaxWidth = w >= 1700 ? 360 : 240;
        var labels = w >= 1180 ? Visibility.Visible : Visibility.Collapsed;
        SpreadLabel.Visibility = labels;
        WebtoonLabel.Visibility = labels;
        ReaderBackLabel.Visibility = w >= 760 ? Visibility.Visible : Visibility.Collapsed;
        BottomOverlay.Margin = w >= 700 ? new Thickness(24, 0, 24, 18) : new Thickness(8, 0, 8, 10);
    }

    private void SpreadToggle_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleViewModeCommand.Execute(null);
        SyncReaderToggles();
    }

    private void WebtoonToggle_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleWebtoonModeCommand.Execute(null);
        SyncReaderToggles();
    }

    /// <summary>Toggle buttons flip themselves on click; put them back in step with the real reader state.</summary>
    private void SyncReaderToggles()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            FitHeightToggle.IsChecked = ViewModel.IsFitHeight;
            FitWidthToggle.IsChecked = ViewModel.IsFitWidth;
            FitActualToggle.IsChecked = ViewModel.IsFitActual;
            SpreadToggle.IsChecked = ViewModel.IsDoublePageMode;
            WebtoonToggle.IsChecked = ViewModel.IsWebtoonMode;
        });
    }

    /// <summary>Shows or hides the floating toolbars with a fade and a short slide.</summary>
    private void SetChromeVisible(bool visible)
    {
        TopToolbar.Opacity = visible ? 1 : 0;
        BottomOverlay.Opacity = visible ? 1 : 0;
        TopToolbar.Translation = visible ? System.Numerics.Vector3.Zero : new System.Numerics.Vector3(0, -18, 0);
        BottomOverlay.Translation = visible ? System.Numerics.Vector3.Zero : new System.Numerics.Vector3(0, 18, 0);
    }

    private async void BackToLibrary_Click(object sender, RoutedEventArgs e)
    {
        LibraryPage.ReturnToLibraryHome = true;
        await ViewModel.FlushReadingProgressAsync();
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
        else
        {
            Frame.Navigate(typeof(LibraryPage));
        }
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        foreach (var element in new UIElement[] { FitGroup, SpreadToggle, DirectionButton, OcrTextOverlayCard })
        {
            AttachPopVisibilityAnimations(element);
        }
        ReaderScrollViewer.SizeChanged += ReaderScrollViewer_SizeChanged;
        UpdateLayoutForFitMode();
        Focus(FocusState.Programmatic);
    }

    /// <summary>Toolbar pieces pop out (shrink and fade) when hidden and pop back in when shown.</summary>
    private static void AttachPopVisibilityAnimations(UIElement element)
    {
        var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;
        var ease = compositor.CreateCubicBezierEasingFunction(new System.Numerics.Vector2(0.3f, 1.4f), new System.Numerics.Vector2(0.5f, 1f));

        var show = compositor.CreateAnimationGroup();
        var showFade = compositor.CreateScalarKeyFrameAnimation();
        showFade.Target = "Opacity";
        showFade.InsertKeyFrame(0f, 0f);
        showFade.InsertKeyFrame(1f, 1f);
        showFade.Duration = TimeSpan.FromMilliseconds(220);
        var showScale = compositor.CreateVector3KeyFrameAnimation();
        showScale.Target = "Scale";
        showScale.InsertKeyFrame(0f, new System.Numerics.Vector3(0.6f, 0.6f, 1f));
        showScale.InsertKeyFrame(1f, System.Numerics.Vector3.One, ease);
        showScale.Duration = TimeSpan.FromMilliseconds(320);
        show.Add(showFade);
        show.Add(showScale);

        var hide = compositor.CreateAnimationGroup();
        var hideFade = compositor.CreateScalarKeyFrameAnimation();
        hideFade.Target = "Opacity";
        hideFade.InsertKeyFrame(1f, 0f);
        hideFade.Duration = TimeSpan.FromMilliseconds(160);
        var hideScale = compositor.CreateVector3KeyFrameAnimation();
        hideScale.Target = "Scale";
        hideScale.InsertKeyFrame(1f, new System.Numerics.Vector3(0.6f, 0.6f, 1f));
        hideScale.Duration = TimeSpan.FromMilliseconds(160);
        hide.Add(hideFade);
        hide.Add(hideScale);

        if (element is FrameworkElement fe)
        {
            fe.SizeChanged += (_, args) => visual.CenterPoint = new System.Numerics.Vector3((float)args.NewSize.Width / 2f, (float)args.NewSize.Height / 2f, 0f);
        }
        Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.SetImplicitShowAnimation(element, show);
        Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.SetImplicitHideAnimation(element, hide);
    }

    private void CopyOcrBlock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: OcrTextBlock block }) ViewModel.CopyOcrBlock(block);
    }

    private void MainPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _autoHideTimer.Stop();
        ReaderScrollViewer.SizeChanged -= ReaderScrollViewer_SizeChanged;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.ComicTitle))
        {
            MainWindow.Instance?.UpdateTitle(ViewModel.ComicTitle);
        }
        else if (e.PropertyName == nameof(ViewModel.IsFullscreen))
        {
            MainWindow.Instance?.ToggleFullscreen(ViewModel.IsFullscreen);
        }
        else if (e.PropertyName == nameof(ViewModel.FitMode) ||
                 e.PropertyName == nameof(ViewModel.ViewMode) ||
                 e.PropertyName == nameof(ViewModel.IsDoublePageMode) ||
                 e.PropertyName == nameof(ViewModel.CurrentPageImage) ||
                 e.PropertyName == nameof(ViewModel.SecondPageImage))
        {
            UpdateLayoutForFitMode();
            AnchorWebtoonPage();
        }
        else if (e.PropertyName == nameof(ViewModel.ZoomFactor))
        {
            ApplyZoomFactor();
        }
        else if (e.PropertyName == nameof(ViewModel.IsPageOverlayVisible) && ViewModel.IsPageOverlayVisible)
        {
            ShowChromeBriefly();
        }
        else if (e.PropertyName == nameof(ViewModel.CurrentPageIndex))
        {
            if (!ViewModel.IsWebtoonMode)
            {
                if (ReaderScrollViewer.VerticalOffset > 1)
                {
                    ReaderScrollViewer.ChangeView(null, 0, null, disableAnimation: false);
                }
                AnimatePageTransition();
            }
            else
            {
                if (!_isUserScrollingWebtoon)
                {
                    ScrollToWebtoonPage(ViewModel.CurrentPageIndex);
                }
            }
        }
        else if (e.PropertyName == nameof(ViewModel.IsWebtoonMode))
        {
            SyncReaderToggles();
        }
    }

    #region Layout & Fit Modes

    private void ReaderScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateLayoutForFitMode();
    }

    private void UpdateLayoutForFitMode()
    {
        double viewportW = ReaderScrollViewer.ActualWidth > 0 ? ReaderScrollViewer.ActualWidth : ActualWidth;
        double viewportH = ReaderScrollViewer.ActualHeight > 0 ? ReaderScrollViewer.ActualHeight : ActualHeight;

        if (viewportW <= 0 || viewportH <= 0) return;

        if (ViewModel.IsWebtoonMode)
        {
            ReaderScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            ReaderScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            PageDisplayContainer.VerticalAlignment = VerticalAlignment.Top;
            PageDisplayContainer.HorizontalAlignment = HorizontalAlignment.Center;
            WebtoonContainer.MaxWidth = Math.Min(viewportW - 32, 1000);
            return;
        }

        bool isDoubleSpread = ViewModel.IsDoublePageMode &&
            (ViewModel.SecondPageImage != null || (ViewModel.HasComic && ViewModel.CurrentPageIndex + 1 < ViewModel.TotalPages));

        switch (ViewModel.FitMode)
        {
            case FitMode.FitToHeight:
                ReaderScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                ReaderScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                PageDisplayContainer.VerticalAlignment = VerticalAlignment.Center;
                PageDisplayContainer.HorizontalAlignment = HorizontalAlignment.Center;
                if (isDoubleSpread)
                {
                    DoublePageLeftImage.Height = viewportH;
                    DoublePageLeftImage.Width = double.NaN;
                    DoublePageLeftImage.MaxHeight = viewportH;
                    DoublePageLeftImage.MaxWidth = (viewportW - 32) / 2;
                    DoublePageLeftImage.Stretch = Stretch.Uniform;

                    DoublePageRightImage.Visibility = Visibility.Visible;
                    DoublePageRightImage.Height = viewportH;
                    DoublePageRightImage.Width = double.NaN;
                    DoublePageRightImage.MaxHeight = viewportH;
                    DoublePageRightImage.MaxWidth = (viewportW - 32) / 2;
                    DoublePageRightImage.Stretch = Stretch.Uniform;
                }
                else if (ViewModel.IsDoublePageMode)
                {
                    DoublePageRightImage.Visibility = Visibility.Collapsed;
                    DoublePageLeftImage.Height = viewportH;
                    DoublePageLeftImage.Width = double.NaN;
                    DoublePageLeftImage.MaxHeight = viewportH;
                    DoublePageLeftImage.MaxWidth = viewportW - 24;
                    DoublePageLeftImage.Stretch = Stretch.Uniform;
                }
                else
                {
                    PrimaryPageImage.Height = viewportH;
                    PrimaryPageImage.Width = double.NaN;
                    PrimaryPageImage.MaxHeight = viewportH;
                    PrimaryPageImage.MaxWidth = viewportW - 24;
                    PrimaryPageImage.Stretch = Stretch.Uniform;
                }
                break;

            case FitMode.FitToWidth:
                ReaderScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                ReaderScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                PageDisplayContainer.VerticalAlignment = VerticalAlignment.Top;
                PageDisplayContainer.HorizontalAlignment = HorizontalAlignment.Center;
                if (isDoubleSpread)
                {
                    double halfWidth = Math.Max(100, (viewportW - 32) / 2);
                    DoublePageLeftImage.Width = halfWidth;
                    DoublePageLeftImage.Height = double.NaN;
                    DoublePageLeftImage.MaxWidth = halfWidth;
                    DoublePageLeftImage.MaxHeight = double.PositiveInfinity;
                    DoublePageLeftImage.Stretch = Stretch.Uniform;

                    DoublePageRightImage.Visibility = Visibility.Visible;
                    DoublePageRightImage.Width = halfWidth;
                    DoublePageRightImage.Height = double.NaN;
                    DoublePageRightImage.MaxWidth = halfWidth;
                    DoublePageRightImage.MaxHeight = double.PositiveInfinity;
                    DoublePageRightImage.Stretch = Stretch.Uniform;
                }
                else if (ViewModel.IsDoublePageMode)
                {
                    DoublePageRightImage.Visibility = Visibility.Collapsed;
                    DoublePageLeftImage.Width = Math.Max(100, viewportW - 24);
                    DoublePageLeftImage.Height = double.NaN;
                    DoublePageLeftImage.MaxWidth = Math.Max(100, viewportW - 24);
                    DoublePageLeftImage.MaxHeight = double.PositiveInfinity;
                    DoublePageLeftImage.Stretch = Stretch.Uniform;
                }
                else
                {
                    PrimaryPageImage.Width = Math.Max(100, viewportW - 24);
                    PrimaryPageImage.Height = double.NaN;
                    PrimaryPageImage.MaxWidth = Math.Max(100, viewportW - 24);
                    PrimaryPageImage.MaxHeight = double.PositiveInfinity;
                    PrimaryPageImage.Stretch = Stretch.Uniform;
                }
                break;

            case FitMode.ActualSize:
                ReaderScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                ReaderScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                PageDisplayContainer.VerticalAlignment = VerticalAlignment.Top;
                PageDisplayContainer.HorizontalAlignment = HorizontalAlignment.Center;
                PrimaryPageImage.Width = double.NaN;
                PrimaryPageImage.Height = double.NaN;
                PrimaryPageImage.MaxWidth = double.PositiveInfinity;
                PrimaryPageImage.MaxHeight = double.PositiveInfinity;
                PrimaryPageImage.Stretch = Stretch.None;

                DoublePageLeftImage.Width = double.NaN;
                DoublePageLeftImage.Height = double.NaN;
                DoublePageLeftImage.MaxWidth = double.PositiveInfinity;
                DoublePageLeftImage.MaxHeight = double.PositiveInfinity;
                DoublePageLeftImage.Stretch = Stretch.None;

                if (isDoubleSpread)
                {
                    DoublePageRightImage.Visibility = Visibility.Visible;
                    DoublePageRightImage.Width = double.NaN;
                    DoublePageRightImage.Height = double.NaN;
                    DoublePageRightImage.MaxWidth = double.PositiveInfinity;
                    DoublePageRightImage.MaxHeight = double.PositiveInfinity;
                    DoublePageRightImage.Stretch = Stretch.None;
                }
                else
                {
                    DoublePageRightImage.Visibility = Visibility.Collapsed;
                }
                break;
        }
    }

    private bool _isSyncingZoomFromViewer;

    private void ApplyZoomFactor()
    {
        if (_isSyncingZoomFromViewer) return;
        float targetZoom = (float)Math.Clamp(ViewModel.ZoomFactor, 0.2, 5.0);
        double oldZoom = Math.Max(0.01, ReaderScrollViewer.ZoomFactor);
        if (Math.Abs(oldZoom - targetZoom) > 0.02)
        {
            // Offsets are in zoomed pixels: keep the spot being read (a third down, centred) under the same screen point,
            // otherwise zooming out in webtoon mode slides far down the strip.
            double lineY = ReaderScrollViewer.ViewportHeight * 0.33;
            double centerX = ReaderScrollViewer.ViewportWidth / 2;
            double contentY = (ReaderScrollViewer.VerticalOffset + lineY) / oldZoom;
            double contentX = (ReaderScrollViewer.HorizontalOffset + centerX) / oldZoom;
            double newY = Math.Max(0, contentY * targetZoom - lineY);
            double newX = Math.Max(0, contentX * targetZoom - centerX);
            ReaderScrollViewer.ChangeView(newX, newY, targetZoom, false);
        }
    }

    private void ReaderScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        ViewModel.NotifyReadingActivity();

        // Pinch / Ctrl+wheel zoom: the percent follows every frame, without pushing the value back to the viewer.
        double viewerZoom = Math.Round(ReaderScrollViewer.ZoomFactor, 2);
        if (Math.Abs(ViewModel.ZoomFactor - viewerZoom) >= 0.01)
        {
            _isSyncingZoomFromViewer = true;
            try { ViewModel.ZoomFactor = viewerZoom; }
            finally { _isSyncingZoomFromViewer = false; }
        }

        if (_isProgrammaticScroll)
        {
            if (!e.IsIntermediate) _isProgrammaticScroll = false;
            return;
        }

        if (ViewModel.IsWebtoonMode && ViewModel.TotalPages > 0)
        {
            int page = GetWebtoonPageAtViewport();
            if (page >= 0 && page != ViewModel.CurrentPageIndex)
            {
                try
                {
                    _isUserScrollingWebtoon = true;
                    ViewModel.CurrentPageIndex = page;
                }
                finally
                {
                    _isUserScrollingWebtoon = false;
                }
            }
        }
    }

    /// <summary>Top of a webtoon page inside the strip, in unzoomed content pixels (NaN when not laid out).</summary>
    private double WebtoonPageTop(int index)
    {
        if (WebtoonContainer?.ContainerFromIndex(index) is not FrameworkElement fe || fe.ActualHeight <= 0) return double.NaN;
        return fe.TransformToVisual(PageDisplayContainer).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
    }

    /// <summary>The page covering the reading line (a third down the screen), found by binary search over real page positions.</summary>
    private int GetWebtoonPageAtViewport()
    {
        int count = ViewModel.TotalPages;
        if (count <= 0) return -1;
        double zoom = Math.Max(0.01, ReaderScrollViewer.ZoomFactor);
        double line = (ReaderScrollViewer.VerticalOffset + ReaderScrollViewer.ViewportHeight * 0.33) / zoom;

        int lo = 0, hi = count - 1, found = 0;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            double top = WebtoonPageTop(mid);
            if (double.IsNaN(top)) return -1;
            if (top <= line)
            {
                found = mid;
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }
        return found;
    }

    private void ScrollToWebtoonPage(int targetIndex)
    {
        if (!ViewModel.IsWebtoonMode || targetIndex < 0 || ViewModel.TotalPages <= 0) return;

        try
        {
            double top = WebtoonPageTop(targetIndex);
            double zoom = Math.Max(0.01, ReaderScrollViewer.ZoomFactor);
            double targetY = !double.IsNaN(top)
                ? top * zoom
                : ViewModel.TotalPages > 1 ? (double)targetIndex / (ViewModel.TotalPages - 1) * ReaderScrollViewer.ScrollableHeight : 0;
            targetY = Math.Clamp(targetY, 0, Math.Max(0, ReaderScrollViewer.ScrollableHeight));
            if (Math.Abs(targetY - ReaderScrollViewer.VerticalOffset) < 1) return;

            _isProgrammaticScroll = true;
            ReaderScrollViewer.ChangeView(null, targetY, null, disableAnimation: false);

            // Safety net in case the viewer reports no final (non-intermediate) change.
            DispatcherQueue.TryEnqueue(async () =>
            {
                await Task.Delay(900);
                _isProgrammaticScroll = false;
            });
        }
        catch (Exception ex)
        {
            _isProgrammaticScroll = false;
            System.Diagnostics.Debug.WriteLine($"[MainPage] ScrollToWebtoonPage error: {ex.Message}");
        }
    }

    private int _anchorVersion;

    /// <summary>
    /// Opening or switching into a webtoon: jump to the saved page, and keep it in view while the pages above it
    /// load and grow (stops as soon as the reader moves on).
    /// </summary>
    private void AnchorWebtoonPage()
    {
        if (!ViewModel.IsWebtoonMode || ViewModel.CurrentPageIndex <= 0) return;
        int anchor = ViewModel.CurrentPageIndex;
        int version = ++_anchorVersion;
        DispatcherQueue.TryEnqueue(async () =>
        {
            foreach (int wait in new[] { 120, 350, 700, 1200, 1900, 2800 })
            {
                await Task.Delay(wait);
                if (version != _anchorVersion || !ViewModel.IsWebtoonMode || ViewModel.CurrentPageIndex != anchor) break;
                ScrollToWebtoonPage(anchor);
            }
        });
    }

    /// <summary>Webtoon strips have no reading direction: left/back always goes up a page, right/forward down.</summary>
    private void WebtoonStep(int delta)
    {
        if (delta > 0 && ViewModel.CanGoNext) _ = ViewModel.NextPageAsync();
        else if (delta < 0 && ViewModel.CanGoPrevious) _ = ViewModel.PreviousPageAsync();
    }

    private void FitHeight_Click(object sender, RoutedEventArgs e)
    {
        SyncReaderToggles();
        ViewModel.SetFitMode(FitMode.FitToHeight);
        UpdateLayoutForFitMode();
        ReaderScrollViewer.ChangeView(0, 0, 1.0f, true);
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateLayoutForFitMode();
            ReaderScrollViewer.ChangeView(0, 0, 1.0f, true);
        });
    }

    private void FitWidth_Click(object sender, RoutedEventArgs e)
    {
        SyncReaderToggles();
        ViewModel.SetFitMode(FitMode.FitToWidth);
        UpdateLayoutForFitMode();
        ReaderScrollViewer.ChangeView(0, 0, 1.0f, true);
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateLayoutForFitMode();
            ReaderScrollViewer.ChangeView(0, 0, 1.0f, true);
        });
    }

    private void FitActual_Click(object sender, RoutedEventArgs e)
    {
        SyncReaderToggles();
        ViewModel.SetFitMode(FitMode.ActualSize);
        UpdateLayoutForFitMode();
        ReaderScrollViewer.ChangeView(0, 0, 1.0f, true);
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateLayoutForFitMode();
            ReaderScrollViewer.ChangeView(0, 0, 1.0f, true);
        });
    }

    #endregion

    #region Auto-Hide Chrome & Overlays

    private void Page_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.NotifyReadingActivity();
        var pt = e.GetCurrentPoint(this).Position;
        if (pt.Y <= 65 || pt.Y >= ActualHeight - 65)
        {
            ShowChromeBriefly();
        }
    }

    private void ShowChromeBriefly()
    {
        if (!ViewModel.HasComic) return;

        SetChromeVisible(true);

        if (!_isPointerOverToolbar)
        {
            _autoHideTimer.Stop();
            _autoHideTimer.Start();
        }
    }

    private void AutoHideTimer_Tick(object? sender, object e)
    {
        _autoHideTimer.Stop();

        if (!_isPointerOverToolbar && !_isScrubbing)
        {
            SetChromeVisible(false);
            ViewModel.IsPageOverlayVisible = false;
        }
    }

    private void Toolbar_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOverToolbar = true;
        _autoHideTimer.Stop();
        SetChromeVisible(true);
    }

    private void Toolbar_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOverToolbar = false;
        _autoHideTimer.Stop();
        _autoHideTimer.Start();
    }

    private void NavZone_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (ReferenceEquals(sender, LeftNavZone))
        {
            PreviousPageButton.Opacity = 0.85;
        }
        else if (ReferenceEquals(sender, RightNavZone))
        {
            NextPageButton.Opacity = 0.85;
        }
    }

    private void NavZone_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (ReferenceEquals(sender, LeftNavZone))
        {
            PreviousPageButton.Opacity = 0;
        }
        else if (ReferenceEquals(sender, RightNavZone))
        {
            NextPageButton.Opacity = 0;
        }
    }

    private void LeftNavZone_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        NavigateLeft();
        e.Handled = true;
    }

    private void RightNavZone_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        NavigateRight();
        e.Handled = true;
    }

    private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateLeft();
    }

    private void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
        NavigateRight();
    }

    private void NavigateLeft()
    {
        if (ViewModel.IsWebtoonMode)
        {
            WebtoonStep(-1);
            return;
        }
        if (ViewModel.ReadingDirection == ReadingDirection.RightToLeft)
        {
            if (ViewModel.CanGoNext) _ = ViewModel.NextPageAsync();
        }
        else
        {
            if (ViewModel.CanGoPrevious) _ = ViewModel.PreviousPageAsync();
        }
    }

    private void NavigateRight()
    {
        if (ViewModel.IsWebtoonMode)
        {
            WebtoonStep(1);
            return;
        }
        if (ViewModel.ReadingDirection == ReadingDirection.RightToLeft)
        {
            if (ViewModel.CanGoPrevious) _ = ViewModel.PreviousPageAsync();
        }
        else
        {
            if (ViewModel.CanGoNext) _ = ViewModel.NextPageAsync();
        }
    }

    #endregion

    #region Mouse Pan & Wheel Navigation

    private void ReaderScrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel.IsWebtoonMode)
        {
            // In Webtoon mode, do not pan or drag on pointer move
            return;
        }

        var point = e.GetCurrentPoint(ReaderScrollViewer);
        if (point.Properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _hasMovedSignificantly = false;
            _panStartPoint = point.Position;
            _panStartHOffset = ReaderScrollViewer.HorizontalOffset;
            _panStartVOffset = ReaderScrollViewer.VerticalOffset;
            ReaderScrollViewer.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void ReaderScrollViewer_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel.IsWebtoonMode) return;

        if (_isPanning)
        {
            var point = e.GetCurrentPoint(ReaderScrollViewer);
            double deltaX = point.Position.X - _panStartPoint.X;
            double deltaY = point.Position.Y - _panStartPoint.Y;

            if (Math.Abs(deltaX) > 6 || Math.Abs(deltaY) > 6)
            {
                _hasMovedSignificantly = true;
            }

            ReaderScrollViewer.ChangeView(_panStartHOffset - deltaX, _panStartVOffset - deltaY, null, true);
            e.Handled = true;
        }
    }

    private void ReaderScrollViewer_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (ViewModel.IsWebtoonMode)
        {
            ToggleOrDismissChrome();
            e.Handled = true;
            return;
        }

        if (_isPanning)
        {
            _isPanning = false;
            ReaderScrollViewer.ReleasePointerCapture(e.Pointer);
            e.Handled = true;

            if (!_hasMovedSignificantly)
            {
                var clickPosition = e.GetCurrentPoint(this).Position;
                double totalWidth = this.ActualWidth;

                if (totalWidth > 0)
                {
                    double xRatio = clickPosition.X / totalWidth;

                    if (xRatio <= 0.25)
                    {
                        // Left ~25% of screen: turn page to left
                        NavigateLeft();
                    }
                    else if (xRatio >= 0.75)
                    {
                        // Right ~25% of screen: turn page to right
                        NavigateRight();
                    }
                    else
                    {
                        // Middle ~50% of screen: toggle toolbar / overlays (original functionality)
                        ToggleOrDismissChrome();
                    }
                }
                else
                {
                    ToggleOrDismissChrome();
                }
            }
        }
    }

    private void ToggleOrDismissChrome()
    {
        if (TopToolbar.Opacity > 0 || BottomOverlay.Opacity > 0)
        {
            SetChromeVisible(false);
            _autoHideTimer.Stop();
            ViewModel.IsPageOverlayVisible = false;
        }
        else
        {
            ShowChromeBriefly();
        }
    }

    private void ReaderScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(ReaderScrollViewer).Properties;
        var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        bool isCtrlDown = ctrlState.HasFlag(CoreVirtualKeyStates.Down);

        if (!isCtrlDown)
        {
            int delta = properties.MouseWheelDelta;

            if (ViewModel.IsWebtoonMode)
            {
                // Smooth scroll in Webtoon mode without page turns
                // Follows the wheel/trackpad amount, so precision touchpads scroll smoothly instead of in fixed jumps.
                double target = ReaderScrollViewer.VerticalOffset - delta * 1.5;
                target = Math.Clamp(target, 0, ReaderScrollViewer.ScrollableHeight);
                ReaderScrollViewer.ChangeView(null, target, null, disableAnimation: false);
                e.Handled = true;
                return;
            }

            // If scrolled down/up, flip page if in fit-to-height mode or at vertical scroll boundary
            if (ViewModel.FitMode == FitMode.FitToHeight ||
                (delta < 0 && ReaderScrollViewer.VerticalOffset >= ReaderScrollViewer.ScrollableHeight - 5) ||
                (delta > 0 && ReaderScrollViewer.VerticalOffset <= 5))
            {
                if (delta < 0 && ViewModel.CanGoNext)
                {
                    _ = ViewModel.NextPageAsync();
                    e.Handled = true;
                }
                else if (delta > 0 && ViewModel.CanGoPrevious)
                {
                    _ = ViewModel.PreviousPageAsync();
                    e.Handled = true;
                }
            }
        }
    }

    #endregion

    #region Keyboard Navigation

    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        var altState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
        bool isCtrl = ctrlState.HasFlag(CoreVirtualKeyStates.Down);
        bool isShift = shiftState.HasFlag(CoreVirtualKeyStates.Down);
        bool isAlt = altState.HasFlag(CoreVirtualKeyStates.Down);

        switch (e.Key)
        {
            case VirtualKey.Back:
                ReaderBack_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;

            case VirtualKey.Up:
                if (ViewModel.IsWebtoonMode)
                {
                    double target = Math.Max(0, ReaderScrollViewer.VerticalOffset - 180);
                    ReaderScrollViewer.ChangeView(null, target, null, disableAnimation: false);
                    e.Handled = true;
                }
                break;

            case VirtualKey.Down:
                if (ViewModel.IsWebtoonMode)
                {
                    double target = Math.Min(ReaderScrollViewer.ScrollableHeight, ReaderScrollViewer.VerticalOffset + 180);
                    ReaderScrollViewer.ChangeView(null, target, null, disableAnimation: false);
                    e.Handled = true;
                }
                break;

            case VirtualKey.PageDown:
                if (ViewModel.IsWebtoonMode)
                {
                    double target = Math.Min(ReaderScrollViewer.ScrollableHeight, ReaderScrollViewer.VerticalOffset + Math.Max(200, ReaderScrollViewer.ViewportHeight * 0.85));
                    ReaderScrollViewer.ChangeView(null, target, null, disableAnimation: false);
                    e.Handled = true;
                    break;
                }
                if (ViewModel.ReadingDirection == ReadingDirection.RightToLeft)
                {
                    if (ViewModel.CanGoPrevious) _ = ViewModel.PreviousPageAsync();
                }
                else
                {
                    if (ViewModel.CanGoNext) _ = ViewModel.NextPageAsync();
                }
                e.Handled = true;
                break;

            case VirtualKey.PageUp:
                if (ViewModel.IsWebtoonMode)
                {
                    double target = Math.Max(0, ReaderScrollViewer.VerticalOffset - Math.Max(200, ReaderScrollViewer.ViewportHeight * 0.85));
                    ReaderScrollViewer.ChangeView(null, target, null, disableAnimation: false);
                    e.Handled = true;
                    break;
                }
                if (ViewModel.ReadingDirection == ReadingDirection.RightToLeft)
                {
                    if (ViewModel.CanGoNext) _ = ViewModel.NextPageAsync();
                }
                else
                {
                    if (ViewModel.CanGoPrevious) _ = ViewModel.PreviousPageAsync();
                }
                e.Handled = true;
                break;

            case VirtualKey.Right:
                if (ViewModel.IsWebtoonMode)
                {
                    WebtoonStep(1);
                    e.Handled = true;
                    break;
                }
                if (ViewModel.ReadingDirection == ReadingDirection.RightToLeft)
                {
                    if (ViewModel.CanGoPrevious) _ = ViewModel.PreviousPageAsync();
                }
                else
                {
                    if (ViewModel.CanGoNext) _ = ViewModel.NextPageAsync();
                }
                e.Handled = true;
                break;

            case VirtualKey.Left:
                if (isAlt)
                {
                    ReaderBack_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (ViewModel.IsWebtoonMode)
                {
                    WebtoonStep(-1);
                    e.Handled = true;
                }
                else
                {
                    if (ViewModel.ReadingDirection == ReadingDirection.RightToLeft)
                    {
                        if (ViewModel.CanGoNext) _ = ViewModel.NextPageAsync();
                    }
                    else
                    {
                        if (ViewModel.CanGoPrevious) _ = ViewModel.PreviousPageAsync();
                    }
                    e.Handled = true;
                }
                break;


            case VirtualKey.Space:
                if (ViewModel.IsWebtoonMode)
                {
                    WebtoonStep(isShift ? -1 : 1);
                    e.Handled = true;
                    break;
                }
                if (isShift)
                {
                    if (ViewModel.ReadingDirection == ReadingDirection.RightToLeft)
                    {
                        if (ViewModel.CanGoNext) _ = ViewModel.NextPageAsync();
                    }
                    else
                    {
                        if (ViewModel.CanGoPrevious) _ = ViewModel.PreviousPageAsync();
                    }
                }
                else
                {
                    if (ViewModel.ReadingDirection == ReadingDirection.RightToLeft)
                    {
                        if (ViewModel.CanGoPrevious) _ = ViewModel.PreviousPageAsync();
                    }
                    else
                    {
                        if (ViewModel.CanGoNext) _ = ViewModel.NextPageAsync();
                    }
                }
                e.Handled = true;
                break;

            case VirtualKey.Home:
                _ = ViewModel.FirstPageAsync();
                e.Handled = true;
                break;

            case VirtualKey.End:
                _ = ViewModel.LastPageAsync();
                e.Handled = true;
                break;

            case VirtualKey.F11:
                ViewModel.ToggleFullscreen();
                e.Handled = true;
                break;

            case VirtualKey.Escape:
                if (ViewModel.IsFullscreen)
                {
                    ViewModel.IsFullscreen = false;
                    e.Handled = true;
                }
                else if (ViewModel.IsError)
                {
                    ViewModel.DismissError();
                    e.Handled = true;
                }
                break;

            case VirtualKey.O:
                if (isCtrl)
                {
                    if (isShift)
                    {
                        _ = ViewModel.OpenFolderAsync();
                    }
                    else
                    {
                        _ = ViewModel.OpenFileAsync();
                    }
                    e.Handled = true;
                }
                break;

            case VirtualKey.Add:
                if (isCtrl)
                {
                    ViewModel.ZoomIn();
                    e.Handled = true;
                }
                break;

            case VirtualKey.Subtract:
                if (isCtrl)
                {
                    ViewModel.ZoomOut();
                    e.Handled = true;
                }
                break;

            case VirtualKey.Number0:
                if (isCtrl)
                {
                    ViewModel.ResetZoom();
                    e.Handled = true;
                }
                break;

            case VirtualKey.D:
                if (isCtrl)
                {
                    _ = ViewModel.ToggleBookmarkCurrentPageAsync();
                    e.Handled = true;
                }
                else
                {
                    _ = ViewModel.ToggleViewModeAsync();
                    e.Handled = true;
                }
                break;

            case VirtualKey.B:
                if (isCtrl)
                {
                    _ = ViewModel.ToggleBookmarkCurrentPageAsync();
                    e.Handled = true;
                }
                break;

            case VirtualKey.R:
                if (isCtrl)
                {
                    _ = ViewModel.ToggleReadingDirectionAsync();
                    e.Handled = true;
                }
                break;

            case VirtualKey.W:
                if (!isCtrl)
                {
                    ViewModel.SetFitMode(FitMode.FitToWidth);
                    e.Handled = true;
                }
                break;

            case VirtualKey.H:
                if (!isCtrl)
                {
                    ViewModel.SetFitMode(FitMode.FitToHeight);
                    e.Handled = true;
                }
                break;

            case VirtualKey.A:
                if (!isCtrl)
                {
                    ViewModel.SetFitMode(FitMode.ActualSize);
                    e.Handled = true;
                }
                break;

            case VirtualKey.V:
                if (!isCtrl)
                {
                    _ = ViewModel.ToggleWebtoonModeAsync();
                    e.Handled = true;
                }
                break;

            case VirtualKey.F:
                if (isCtrl)
                {
                    OcrDropDownButton.Flyout?.ShowAt(OcrDropDownButton);
                    e.Handled = true;
                }
                break;
        }
    }

    #endregion

    #region Drag and Drop Support

    private void Page_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Open in Komik";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
        }
        else
        {
            e.AcceptedOperation = DataPackageOperation.None;
        }
    }

    private async void Page_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var def = e.GetDeferral();
            try
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    string path = items[0].Path;
                    await ViewModel.LoadComicPathAsync(path);
                }
            }
            finally
            {
                def.Complete();
            }
        }
    }

    #endregion

    #region Scrubber & Error InfoBar

    private void PageScrubber_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        int targetPage = (int)Math.Round(e.NewValue) - 1;
        if (targetPage >= 0 && targetPage != ViewModel.CurrentPageIndex && targetPage < ViewModel.TotalPages)
        {
            _isScrubbing = true;
            _ = ViewModel.GoToPageAsync(targetPage);
            _isScrubbing = false;
        }
    }

    private void ErrorInfoBar_CloseButtonClick(InfoBar sender, object args)
    {
        ViewModel.DismissError();
    }

    #endregion

    #region Bookmarks & Color Settings Handlers

    private async void AddBookmarkNote_Click(object sender, RoutedEventArgs e)
    {
        var textBox = new TextBox { Width = 260, PlaceholderText = "Optional note (e.g. Action scene)" };
        var dialog = new ContentDialog
        {
            Title = $"Bookmark Page {ViewModel.CurrentPageIndex + 1}",
            Content = textBox,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.AddBookmarkWithNoteAsync(textBox.Text.Trim());
        }
    }

    private void JumpToBookmark_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: BookmarkEntity bookmark })
        {
            _ = ViewModel.JumpToBookmarkAsync(bookmark);
        }
    }

    private void DeleteBookmark_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: BookmarkEntity bookmark })
        {
            _ = ViewModel.RemoveBookmarkAsync(bookmark);
        }
    }

    private void ResetColorSettings_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ColorSettings.Reset();
    }

    #endregion

    #region Page Transition Animation

    private void AnimatePageTransition()
    {
        try
        {
            var fadeIn = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
            {
                From = 0.35,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(220)),
                EasingFunction = new Microsoft.UI.Xaml.Media.Animation.CubicEase { EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut }
            };
            var sb = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
            sb.Children.Add(fadeIn);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeIn, PageDisplayContainer);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeIn, "Opacity");
            sb.Begin();
        }
        catch
        {
            PageDisplayContainer.Opacity = 1.0;
        }
    }

    #endregion

    #region Reading Preset & OCR Handlers

    private void PresetOriginal_Click(object sender, RoutedEventArgs e) => ViewModel.SetReadingPreset(ReadingPreset.Original);
    private void PresetNight_Click(object sender, RoutedEventArgs e) => ViewModel.SetReadingPreset(ReadingPreset.NightMode);
    private void PresetSepia_Click(object sender, RoutedEventArgs e) => ViewModel.SetReadingPreset(ReadingPreset.Sepia);
    private void PresetHighContrast_Click(object sender, RoutedEventArgs e) => ViewModel.SetReadingPreset(ReadingPreset.HighContrast);
    private void PresetGrayscale_Click(object sender, RoutedEventArgs e) => ViewModel.SetReadingPreset(ReadingPreset.Grayscale);
    private void PresetInverted_Click(object sender, RoutedEventArgs e) => ViewModel.SetReadingPreset(ReadingPreset.Inverted);

    private void OcrSearch_Click(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.SearchInComicOcrAsync(OcrSearchBox.Text.Trim());
    }

    private void OcrSearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            _ = ViewModel.SearchInComicOcrAsync(OcrSearchBox.Text.Trim());
            e.Handled = true;
        }
    }

    private void OcrSearchResult_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: OcrSearchResultItem item })
        {
            _ = ViewModel.GoToPageAsync(item.PageIndex);
        }
    }

    #endregion
}
