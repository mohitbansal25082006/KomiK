using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace Komik.Helpers;

/// <summary>
/// Lightweight Composition animations used across the app (the desktop take on the website's comic motion):
/// cards that lift toward the pointer, panels that pop in when shown, and a gentle press squash.
/// Everything respects the Windows "Animation effects" setting.
/// </summary>
public static class MotionHelper
{
    private static readonly Windows.UI.ViewManagement.UISettings UiSettings = new();

    private static bool AnimationsEnabled
    {
        get
        {
            try
            {
                return UiSettings.AnimationsEnabled;
            }
            catch
            {
                return true;
            }
        }
    }

    #region HoverLift

    public static readonly DependencyProperty HoverLiftProperty = DependencyProperty.RegisterAttached(
        "HoverLift", typeof(bool), typeof(MotionHelper), new PropertyMetadata(false, OnHoverLiftChanged));

    public static bool GetHoverLift(DependencyObject obj) => (bool)obj.GetValue(HoverLiftProperty);
    public static void SetHoverLift(DependencyObject obj, bool value) => obj.SetValue(HoverLiftProperty, value);

    private static void OnHoverLiftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;
        element.PointerEntered -= Lift_PointerEntered;
        element.PointerExited -= Lift_PointerExited;
        element.PointerCaptureLost -= Lift_PointerExited;
        element.PointerPressed -= Lift_PointerPressed;
        element.PointerReleased -= Lift_PointerReleased;

        if ((bool)e.NewValue)
        {
            element.PointerEntered += Lift_PointerEntered;
            element.PointerExited += Lift_PointerExited;
            element.PointerCaptureLost += Lift_PointerExited;
            element.PointerPressed += Lift_PointerPressed;
            element.PointerReleased += Lift_PointerReleased;
        }
    }

    private static void Lift_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AnimateTo(sender as UIElement, scale: 1.035f, translateY: -6f, durationMs: 260, rotation: (float)GetHoverTilt((DependencyObject)sender));
        PlayHoverRoles(sender as UIElement, hovered: true);
    }

    private static void Lift_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AnimateTo(sender as UIElement, scale: 1f, translateY: 0f, durationMs: 320, rotation: 0f);
        PlayHoverRoles(sender as UIElement, hovered: false);
    }

    private static void Lift_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) =>
        AnimateTo(sender as UIElement, scale: 0.975f, translateY: -2f, durationMs: 120, rotation: 0f);

    private static void Lift_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) =>
        AnimateTo(sender as UIElement, scale: 1.035f, translateY: -6f, durationMs: 220, rotation: (float)GetHoverTilt((DependencyObject)sender));

    #endregion

    #region HoverRole

    /// <summary>Degrees a HoverLift element tilts while hovered (comic "wobble").</summary>
    public static readonly DependencyProperty HoverTiltProperty = DependencyProperty.RegisterAttached(
        "HoverTilt", typeof(double), typeof(MotionHelper), new PropertyMetadata(0.0));

    public static double GetHoverTilt(DependencyObject obj) => (double)obj.GetValue(HoverTiltProperty);
    public static void SetHoverTilt(DependencyObject obj, double value) => obj.SetValue(HoverTiltProperty, value);

    /// <summary>
    /// How a child reacts when its HoverLift ancestor is hovered:
    /// Zoom (cover push-in), Reveal (pops in from hidden), Shine (light sweep), FanLeft / FanRight (stacked pages spread),
    /// Rise (slides up a little), Spin (sticker wiggle).
    /// </summary>
    public static readonly DependencyProperty HoverRoleProperty = DependencyProperty.RegisterAttached(
        "HoverRole", typeof(string), typeof(MotionHelper), new PropertyMetadata(null, OnHoverRoleChanged));

    public static string? GetHoverRole(DependencyObject obj) => (string?)obj.GetValue(HoverRoleProperty);
    public static void SetHoverRole(DependencyObject obj, string? value) => obj.SetValue(HoverRoleProperty, value);

    private static void OnHoverRoleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe) return;
        if (e.NewValue is "Reveal" or "Shine")
        {
            fe.Loaded -= HiddenRole_Loaded;
            fe.Loaded += HiddenRole_Loaded;
            if (fe.IsLoaded) ResetHidden(fe);
        }
    }

    private static void HiddenRole_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe) ResetHidden(fe);
    }

    private static void ResetHidden(FrameworkElement fe)
    {
        ElementCompositionPreview.GetElementVisual(fe).Opacity = 0f;
    }

    private static void PlayHoverRoles(UIElement? host, bool hovered)
    {
        if (host == null) return;
        var targets = new List<FrameworkElement>();
        CollectRoles(host, targets, 0);
        foreach (var target in targets)
        {
            string role = GetHoverRole(target) ?? string.Empty;
            if (!AnimationsEnabled)
            {
                if (role == "Reveal") ElementCompositionPreview.GetElementVisual(target).Opacity = hovered ? 1f : 0f;
                continue;
            }

            switch (role)
            {
                case "Zoom":
                    ClipToParent(target);
                    AnimateTo(target, scale: hovered ? 1.09f : 1f, translateY: 0f, durationMs: hovered ? 520 : 380);
                    break;
                case "Sink":
                    AnimateTo(target, scale: 1f, translateY: hovered ? 5f : 0f, durationMs: 300, translateX: hovered ? 5f : 0f);
                    break;
                case "Rise":
                    AnimateTo(target, scale: 1f, translateY: hovered ? -4f : 0f, durationMs: 300);
                    break;
                case "Spin":
                    AnimateTo(target, scale: hovered ? 1.12f : 1f, translateY: 0f, durationMs: 420, rotation: hovered ? -8f : 0f);
                    break;
                case "FanLeft":
                    AnimateTo(target, scale: 1f, translateY: hovered ? -4f : 0f, durationMs: 360, rotation: hovered ? -7f : 0f, translateX: hovered ? -16f : 0f);
                    break;
                case "FanRight":
                    AnimateTo(target, scale: 1f, translateY: hovered ? -8f : 0f, durationMs: 360, rotation: hovered ? 8f : 0f, translateX: hovered ? 18f : 0f);
                    break;
                case "Reveal":
                    PlayPop(target, hovered);
                    break;
                case "Shine":
                    ClipToParent(target);
                    if (hovered) PlayShine(target);
                    break;
            }
        }
    }

    private static void CollectRoles(DependencyObject parent, List<FrameworkElement> found, int depth)
    {
        if (depth > 16) return;
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement fe && GetHoverRole(fe) != null) found.Add(fe);
            // A nested HoverLift element owns its own roles.
            if (child is UIElement ui && GetHoverLift(ui)) continue;
            CollectRoles(child, found, depth + 1);
        }
    }

    /// <summary>Keeps zooming covers and light sweeps inside their rounded cover frame.</summary>
    private static void ClipToParent(FrameworkElement target)
    {
        if (VisualTreeHelper.GetParent(target) is not FrameworkElement parent || parent.ActualWidth <= 0) return;
        var visual = ElementCompositionPreview.GetElementVisual(parent);
        var compositor = visual.Compositor;
        var size = new Vector2((float)parent.ActualWidth, (float)parent.ActualHeight);
        if (visual.Clip is CompositionGeometricClip existing &&
            existing.Geometry is CompositionRoundedRectangleGeometry g && g.Size == size) return;

        float radius = parent is Microsoft.UI.Xaml.Controls.Grid grid ? (float)grid.CornerRadius.TopLeft
            : parent is Microsoft.UI.Xaml.Controls.Border border ? (float)border.CornerRadius.TopLeft : 0f;
        var geometry = compositor.CreateRoundedRectangleGeometry();
        geometry.Size = size;
        geometry.CornerRadius = new Vector2(radius, radius);
        visual.Clip = compositor.CreateGeometricClip(geometry);
    }

    private static void PlayPop(FrameworkElement target, bool show)
    {
        var visual = ElementCompositionPreview.GetElementVisual(target);
        var compositor = visual.Compositor;
        CenterVisual(target, visual);
        var bounce = compositor.CreateCubicBezierEasingFunction(new Vector2(0.3f, 1.6f), new Vector2(0.5f, 1f));

        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(1f, show ? 1f : 0f);
        fade.Duration = TimeSpan.FromMilliseconds(show ? 180 : 140);

        var scale = compositor.CreateVector3KeyFrameAnimation();
        var spin = compositor.CreateScalarKeyFrameAnimation();
        if (show)
        {
            scale.InsertKeyFrame(0f, new Vector3(0.4f, 0.4f, 1f));
            scale.InsertKeyFrame(1f, Vector3.One, bounce);
            scale.Duration = TimeSpan.FromMilliseconds(420);
            spin.InsertKeyFrame(0f, -18f);
            spin.InsertKeyFrame(1f, -6f, bounce);
            spin.Duration = TimeSpan.FromMilliseconds(420);
        }
        else
        {
            scale.InsertKeyFrame(1f, new Vector3(0.7f, 0.7f, 1f));
            scale.Duration = TimeSpan.FromMilliseconds(160);
            spin.InsertKeyFrame(1f, -12f);
            spin.Duration = TimeSpan.FromMilliseconds(160);
        }

        visual.StartAnimation("Opacity", fade);
        visual.StartAnimation("Scale", scale);
        visual.StartAnimation("RotationAngleInDegrees", spin);
    }

    private static void PlayShine(FrameworkElement target)
    {
        var visual = ElementCompositionPreview.GetElementVisual(target);
        var compositor = visual.Compositor;
        ElementCompositionPreview.SetIsTranslationEnabled(target, true);
        float width = (float)Math.Max(120, (target.Parent as FrameworkElement)?.ActualWidth ?? target.ActualWidth);

        var move = compositor.CreateVector3KeyFrameAnimation();
        move.InsertKeyFrame(0f, new Vector3(-width, 0, 0));
        move.InsertKeyFrame(1f, new Vector3(width * 1.2f, 0, 0), compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0f), new Vector2(0.2f, 1f)));
        move.Duration = TimeSpan.FromMilliseconds(720);

        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0f, 0f);
        fade.InsertKeyFrame(0.15f, 1f);
        fade.InsertKeyFrame(0.8f, 1f);
        fade.InsertKeyFrame(1f, 0f);
        fade.Duration = TimeSpan.FromMilliseconds(720);

        visual.StartAnimation("Translation", move);
        visual.StartAnimation("Opacity", fade);
    }

    #endregion

    #region PressPop

    /// <summary>Buttons and fields: a small springy grow on hover and a comic "squash" when pressed.</summary>
    public static readonly DependencyProperty PressPopProperty = DependencyProperty.RegisterAttached(
        "PressPop", typeof(bool), typeof(MotionHelper), new PropertyMetadata(false, OnPressPopChanged));

    public static bool GetPressPop(DependencyObject obj) => (bool)obj.GetValue(PressPopProperty);
    public static void SetPressPop(DependencyObject obj, bool value) => obj.SetValue(PressPopProperty, value);

    private static void OnPressPopChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;
        element.PointerEntered -= Pop_PointerEntered;
        element.PointerExited -= Pop_PointerExited;
        element.PointerCaptureLost -= Pop_PointerExited;
        element.RemoveHandler(UIElement.PointerPressedEvent, (Microsoft.UI.Xaml.Input.PointerEventHandler)Pop_PointerPressed);
        element.RemoveHandler(UIElement.PointerReleasedEvent, (Microsoft.UI.Xaml.Input.PointerEventHandler)Pop_PointerReleased);
        if (!(bool)e.NewValue) return;

        element.PointerEntered += Pop_PointerEntered;
        element.PointerExited += Pop_PointerExited;
        element.PointerCaptureLost += Pop_PointerExited;
        // Buttons mark pointer presses handled, so listen to handled events too.
        element.AddHandler(UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(Pop_PointerPressed), true);
        element.AddHandler(UIElement.PointerReleasedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler(Pop_PointerReleased), true);
    }

    private static void Pop_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Microsoft.UI.Xaml.Controls.Control { IsEnabled: false }) return;
        AnimateTo(sender as UIElement, scale: 1.045f, translateY: -1.5f, durationMs: 240);
    }

    private static void Pop_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) =>
        AnimateTo(sender as UIElement, scale: 1f, translateY: 0f, durationMs: 260);

    private static void Pop_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) =>
        AnimateTo(sender as UIElement, scale: 0.92f, translateY: 1f, durationMs: 110);

    private static void Pop_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) =>
        AnimateTo(sender as UIElement, scale: 1.045f, translateY: -1.5f, durationMs: 300);

    #endregion

    #region RevealOnShow

    /// <summary>Pops an element in (fade + scale + slide) every time its Visibility becomes Visible.</summary>
    public static readonly DependencyProperty RevealOnShowProperty = DependencyProperty.RegisterAttached(
        "RevealOnShow", typeof(bool), typeof(MotionHelper), new PropertyMetadata(false, OnRevealOnShowChanged));

    public static bool GetRevealOnShow(DependencyObject obj) => (bool)obj.GetValue(RevealOnShowProperty);
    public static void SetRevealOnShow(DependencyObject obj, bool value) => obj.SetValue(RevealOnShowProperty, value);

    private static readonly DependencyProperty RevealTokenProperty = DependencyProperty.RegisterAttached(
        "RevealToken", typeof(long), typeof(MotionHelper), new PropertyMetadata(0L));

    private static void OnRevealOnShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element || !(bool)e.NewValue) return;
        if ((long)element.GetValue(RevealTokenProperty) != 0) return;

        long token = element.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, (s, _) =>
        {
            if (s is UIElement el && el.Visibility == Visibility.Visible)
            {
                PlayReveal(el);
            }
        });
        element.SetValue(RevealTokenProperty, token);

        if (element is FrameworkElement fe)
        {
            fe.Loaded += (s, _) =>
            {
                if (s is UIElement el && el.Visibility == Visibility.Visible) PlayReveal(el);
            };
        }
    }

    public static void PlayReveal(UIElement element)
    {
        if (!AnimationsEnabled) return;
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);
        CenterVisual(element, visual);

        var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 0.9f), new Vector2(0.3f, 1.08f));

        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0f, 0f);
        fade.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.6f), new Vector2(0.3f, 1f)));
        fade.Duration = TimeSpan.FromMilliseconds(220);

        var scale = compositor.CreateVector3KeyFrameAnimation();
        scale.InsertKeyFrame(0f, new Vector3(0.94f, 0.94f, 1f));
        scale.InsertKeyFrame(1f, Vector3.One, easing);
        scale.Duration = TimeSpan.FromMilliseconds(380);

        var slide = compositor.CreateVector3KeyFrameAnimation();
        slide.InsertKeyFrame(0f, new Vector3(0, 18, 0));
        slide.InsertKeyFrame(1f, Vector3.Zero, easing);
        slide.Duration = TimeSpan.FromMilliseconds(380);

        visual.StartAnimation("Opacity", fade);
        visual.StartAnimation("Scale", scale);
        visual.StartAnimation("Translation", slide);
    }

    #endregion

    #region StaggerIndex

    /// <summary>Plays a short rise-in for list/grid items, delayed by index (capped) for a staggered cascade.</summary>
    public static void PlayItemEntrance(UIElement element, int index)
    {
        if (!AnimationsEnabled) return;
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);

        var delay = TimeSpan.FromMilliseconds(Math.Min(index, 18) * 28);
        var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 0.85f), new Vector2(0.3f, 1f));

        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0f, 0f);
        fade.InsertKeyFrame(1f, 1f, easing);
        fade.Duration = TimeSpan.FromMilliseconds(260);
        fade.DelayTime = delay;
        fade.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

        var slide = compositor.CreateVector3KeyFrameAnimation();
        slide.InsertKeyFrame(0f, new Vector3(0, 22, 0));
        slide.InsertKeyFrame(1f, Vector3.Zero, easing);
        slide.Duration = TimeSpan.FromMilliseconds(420);
        slide.DelayTime = delay;
        slide.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;

        visual.StartAnimation("Opacity", fade);
        visual.StartAnimation("Translation", slide);
    }

    #endregion

    private static void AnimateTo(UIElement? element, float scale, float translateY, int durationMs, float rotation = 0f, float translateX = 0f)
    {
        if (element == null || !AnimationsEnabled) return;
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);
        CenterVisual(element, visual);

        var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 1.25f), new Vector2(0.5f, 1f));

        var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
        scaleAnim.InsertKeyFrame(1f, new Vector3(scale, scale, 1f), easing);
        scaleAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

        var translateAnim = compositor.CreateVector3KeyFrameAnimation();
        translateAnim.InsertKeyFrame(1f, new Vector3(translateX, translateY, 0), easing);
        translateAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

        var rotateAnim = compositor.CreateScalarKeyFrameAnimation();
        rotateAnim.InsertKeyFrame(1f, rotation, easing);
        rotateAnim.Duration = TimeSpan.FromMilliseconds(durationMs);

        visual.StartAnimation("Scale", scaleAnim);
        visual.StartAnimation("Translation", translateAnim);
        visual.StartAnimation("RotationAngleInDegrees", rotateAnim);
    }

    private static void CenterVisual(UIElement element, Visual visual)
    {
        if (element is FrameworkElement fe && fe.ActualWidth > 0)
        {
            visual.CenterPoint = new Vector3((float)fe.ActualWidth / 2f, (float)fe.ActualHeight / 2f, 0f);
        }
    }
}
