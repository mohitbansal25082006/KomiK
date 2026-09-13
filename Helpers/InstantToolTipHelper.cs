using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Komik.Helpers;

/// \u003csummary\u003e
/// Helper that enables tooltips (especially on (i) info icons) to open instantly (0ms delay)
/// on pointer hover, rather than waiting for the standard Windows hover delay timer.
/// \u003c/summary\u003e
public static class InstantToolTipHelper
{
    public static readonly DependencyProperty IsInstantProperty =
        DependencyProperty.RegisterAttached(
            "IsInstant",
            typeof(bool),
            typeof(InstantToolTipHelper),
            new PropertyMetadata(false, OnIsInstantChanged));

    public static bool GetIsInstant(DependencyObject obj) => (bool)obj.GetValue(IsInstantProperty);
    public static void SetIsInstant(DependencyObject obj, bool value) => obj.SetValue(IsInstantProperty, value);

    public static readonly DependencyProperty InfoTextProperty =
        DependencyProperty.RegisterAttached(
            "InfoText",
            typeof(string),
            typeof(InstantToolTipHelper),
            new PropertyMetadata(null, OnInfoTextChanged));

    public static string? GetInfoText(DependencyObject obj) => (string?)obj.GetValue(InfoTextProperty);
    public static void SetInfoText(DependencyObject obj, string? value) => obj.SetValue(InfoTextProperty, value);

    private static void OnIsInstantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe) return;

        if (e.NewValue is true)
        {
            AttachEvents(fe);
        }
        else
        {
            DetachEvents(fe);
        }
    }

    private static void OnInfoTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe) return;

        var text = e.NewValue as string;
        if (!string.IsNullOrEmpty(text))
        {
            var tip = CreateToolTip(text);
            ToolTipService.SetToolTip(fe, tip);
            AttachEvents(fe);
        }
        else
        {
            ToolTipService.SetToolTip(fe, null);
            DetachEvents(fe);
        }
    }

    private static void AttachEvents(FrameworkElement fe)
    {
        fe.PointerEntered -= Element_PointerEntered;
        fe.PointerExited -= Element_PointerExited;
        fe.PointerCaptureLost -= Element_PointerCaptureLost;
        fe.PointerCanceled -= Element_PointerCanceled;
        fe.Unloaded -= Element_Unloaded;

        fe.PointerEntered += Element_PointerEntered;
        fe.PointerExited += Element_PointerExited;
        fe.PointerCaptureLost += Element_PointerCaptureLost;
        fe.PointerCanceled += Element_PointerCanceled;
        fe.Unloaded += Element_Unloaded;

        if (fe is ButtonBase btn)
        {
            btn.Click -= Element_Clicked;
            btn.Click += Element_Clicked;
        }
    }

    private static void DetachEvents(FrameworkElement fe)
    {
        fe.PointerEntered -= Element_PointerEntered;
        fe.PointerExited -= Element_PointerExited;
        fe.PointerCaptureLost -= Element_PointerCaptureLost;
        fe.PointerCanceled -= Element_PointerCanceled;
        fe.Unloaded -= Element_Unloaded;

        if (fe is ButtonBase btn)
        {
            btn.Click -= Element_Clicked;
        }
    }

    private static void Element_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;

        var currentTip = ToolTipService.GetToolTip(fe);
        if (currentTip == null)
        {
            var text = GetInfoText(fe);
            if (!string.IsNullOrEmpty(text))
            {
                var tip = CreateToolTip(text);
                ToolTipService.SetToolTip(fe, tip);
                tip.IsOpen = true;
            }
        }
        else if (currentTip is string str && !string.IsNullOrEmpty(str))
        {
            var tip = CreateToolTip(str);
            ToolTipService.SetToolTip(fe, tip);
            tip.IsOpen = true;
        }
        else if (currentTip is ToolTip tip)
        {
            tip.IsOpen = true;
        }
    }

    private static void Element_PointerExited(object sender, PointerRoutedEventArgs e) => CloseToolTip(sender);
    private static void Element_PointerCaptureLost(object sender, PointerRoutedEventArgs e) => CloseToolTip(sender);
    private static void Element_PointerCanceled(object sender, PointerRoutedEventArgs e) => CloseToolTip(sender);
    private static void Element_Unloaded(object sender, RoutedEventArgs e) => CloseToolTip(sender);
    private static void Element_Clicked(object sender, RoutedEventArgs e) => CloseToolTip(sender);

    private static void CloseToolTip(object sender)
    {
        if (sender is FrameworkElement fe && ToolTipService.GetToolTip(fe) is ToolTip tip)
        {
            tip.IsOpen = false;
        }
    }

    private static ToolTip CreateToolTip(string text)
    {
        return new ToolTip
        {
            Content = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 340,
                FontSize = 12,
                LineHeight = 18
            },
            MaxWidth = 360,
            Placement = PlacementMode.Top
        };
    }
}
