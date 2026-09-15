using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace Komik.Controls;

/// <summary>
/// Floating comic-style notification. Bind <see cref="IsOpen"/> (TwoWay), <see cref="Title"/>, <see cref="Message"/>
/// and <see cref="Severity"/>. It pops in over the page, counts down and hides itself; hovering pauses the countdown.
/// A new message while open restarts the countdown.
/// </summary>
public sealed partial class ComicToast : UserControl
{
    private readonly DispatcherTimer _timer = new();
    private DateTime _deadline;
    private TimeSpan _remaining;
    private bool _hovered;
    private bool _closing;
    private double _totalSeconds;

    public ComicToast()
    {
        InitializeComponent();
        _timer.Interval = TimeSpan.FromMilliseconds(50);
        _timer.Tick += Timer_Tick;
        Unloaded += (_, _) => _timer.Stop();
    }

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(ComicToast), new PropertyMetadata(false, (d, e) => ((ComicToast)d).OnIsOpenChanged((bool)e.NewValue)));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(ComicToast), new PropertyMetadata(string.Empty, (d, _) => ((ComicToast)d).OnContentChanged()));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(ComicToast), new PropertyMetadata(string.Empty, (d, _) => ((ComicToast)d).OnContentChanged()));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly DependencyProperty SeverityProperty = DependencyProperty.Register(
        nameof(Severity), typeof(InfoBarSeverity), typeof(ComicToast), new PropertyMetadata(InfoBarSeverity.Informational, (d, _) => ((ComicToast)d).ApplySeverity()));

    public InfoBarSeverity Severity
    {
        get => (InfoBarSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    /// <summary>Seconds before hiding. 0 picks a length from the message (longer for errors).</summary>
    public double DurationSeconds { get; set; }

    private void OnIsOpenChanged(bool open)
    {
        if (open) Show();
        else Hide(updateBinding: false);
    }

    private void OnContentChanged()
    {
        TitleText.Text = Title ?? string.Empty;
        TitleText.Visibility = string.IsNullOrWhiteSpace(Title) ? Visibility.Collapsed : Visibility.Visible;
        MessageText.Text = Message ?? string.Empty;
        MessageText.Visibility = string.IsNullOrWhiteSpace(Message) ? Visibility.Collapsed : Visibility.Visible;

        if (IsOpen && Visibility == Visibility.Visible)
        {
            // A fresh message while visible: restart the countdown and give it a little bump.
            StartCountdown();
            Pop(fromHidden: false);
        }
    }

    private void ApplySeverity()
    {
        (string glyph, Color color) = Severity switch
        {
            InfoBarSeverity.Success => ("", Color.FromArgb(255, 0x2E, 0xE5, 0x9D)),
            InfoBarSeverity.Warning => ("", Color.FromArgb(255, 0xFF, 0xD7, 0x00)),
            InfoBarSeverity.Error => ("", Color.FromArgb(255, 0xFF, 0x1F, 0x6D)),
            _ => ("", Color.FromArgb(255, 0x00, 0xC2, 0xFF))
        };
        BadgeIcon.Glyph = glyph;
        BadgeIcon.Foreground = new SolidColorBrush(Severity == InfoBarSeverity.Error ? Microsoft.UI.Colors.White : Color.FromArgb(255, 0x0B, 0x0B, 0x12));
        BadgeBurst.Background = new SolidColorBrush(color);
        CountdownBar.Background = new SolidColorBrush(color);
    }

    private void Show()
    {
        _closing = false;
        OnContentChanged();
        ApplySeverity();
        Visibility = Visibility.Visible;
        Pop(fromHidden: true);
        StartCountdown();
    }

    private void StartCountdown()
    {
        double seconds = DurationSeconds > 0
            ? DurationSeconds
            : Math.Clamp(3.2 + ((Title?.Length ?? 0) + (Message?.Length ?? 0)) / 45.0, 3.5, 7.5) + (Severity == InfoBarSeverity.Error ? 2 : 0);
        _remaining = TimeSpan.FromSeconds(seconds);
        _deadline = DateTime.UtcNow + _remaining;
        CountdownScale.ScaleX = 1;
        _totalSeconds = seconds;
        _timer.Start();
    }

    private void Timer_Tick(object? sender, object e)
    {
        if (_hovered)
        {
            _deadline = DateTime.UtcNow + _remaining;
            return;
        }

        _remaining = _deadline - DateTime.UtcNow;
        double total = _totalSeconds > 0 ? _totalSeconds : 4;
        CountdownScale.ScaleX = Math.Clamp(_remaining.TotalSeconds / total, 0, 1);
        if (_remaining <= TimeSpan.Zero)
        {
            Hide(updateBinding: true);
        }
    }

    private void Hide(bool updateBinding)
    {
        _timer.Stop();
        if (_closing || Visibility == Visibility.Collapsed)
        {
            if (updateBinding && IsOpen) IsOpen = false;
            return;
        }
        _closing = true;

        var visual = ElementCompositionPreview.GetElementVisual(ToastRoot);
        var compositor = visual.Compositor;
        ElementCompositionPreview.SetIsTranslationEnabled(ToastRoot, true);

        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(1f, 0f);
        fade.Duration = TimeSpan.FromMilliseconds(200);
        var slide = compositor.CreateVector3KeyFrameAnimation();
        slide.InsertKeyFrame(1f, new Vector3(0, -26, 0), compositor.CreateCubicBezierEasingFunction(new Vector2(0.5f, 0f), new Vector2(0.9f, 0.4f)));
        slide.Duration = TimeSpan.FromMilliseconds(220);
        visual.StartAnimation("Opacity", fade);
        visual.StartAnimation("Translation", slide);
        batch.End();
        batch.Completed += (_, _) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (!_closing) return;
                Visibility = Visibility.Collapsed;
                _closing = false;
            });
        };

        if (updateBinding && IsOpen) IsOpen = false;
    }

    private void Pop(bool fromHidden)
    {
        var visual = ElementCompositionPreview.GetElementVisual(ToastRoot);
        var compositor = visual.Compositor;
        ElementCompositionPreview.SetIsTranslationEnabled(ToastRoot, true);
        if (ToastRoot.ActualWidth > 0)
        {
            visual.CenterPoint = new Vector3((float)ToastRoot.ActualWidth / 2f, 0, 0);
        }
        var bounce = compositor.CreateCubicBezierEasingFunction(new Vector2(0.3f, 1.5f), new Vector2(0.5f, 1f));

        var fade = compositor.CreateScalarKeyFrameAnimation();
        if (fromHidden) fade.InsertKeyFrame(0f, 0f);
        fade.InsertKeyFrame(1f, 1f);
        fade.Duration = TimeSpan.FromMilliseconds(180);

        var slide = compositor.CreateVector3KeyFrameAnimation();
        slide.InsertKeyFrame(0f, new Vector3(0, fromHidden ? -40 : -6, 0));
        slide.InsertKeyFrame(1f, Vector3.Zero, bounce);
        slide.Duration = TimeSpan.FromMilliseconds(420);

        var scale = compositor.CreateVector3KeyFrameAnimation();
        scale.InsertKeyFrame(0f, new Vector3(fromHidden ? 0.85f : 1.04f, fromHidden ? 0.85f : 1.04f, 1));
        scale.InsertKeyFrame(1f, Vector3.One, bounce);
        scale.Duration = TimeSpan.FromMilliseconds(420);

        visual.StartAnimation("Opacity", fade);
        visual.StartAnimation("Translation", slide);
        visual.StartAnimation("Scale", scale);

        // Badge wiggle
        var wiggle = new Storyboard();
        var rot = new DoubleAnimationUsingKeyFrames();
        rot.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(0), Value = -30 });
        rot.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(260), Value = 12, EasingFunction = new BackEase { Amplitude = 0.6, EasingMode = EasingMode.EaseOut } });
        rot.KeyFrames.Add(new EasingDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(460), Value = -8 });
        Storyboard.SetTarget(rot, BadgeRotate);
        Storyboard.SetTargetProperty(rot, "Angle");
        wiggle.Children.Add(rot);
        wiggle.Begin();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide(updateBinding: true);

    private void ToastRoot_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => _hovered = true;

    private void ToastRoot_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => _hovered = false;
}
