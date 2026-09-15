using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Komik.Helpers;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public bool IsInverted { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        bool boolVal = value is true;
        if (IsInverted) boolVal = !boolVal;
        return boolVal ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>"#RRGGBB" or "#AARRGGBB" text to a brush (accent colors chosen by view models).</summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is not string hex) return null;
        hex = hex.TrimStart('#');
        try
        {
            byte a = 255;
            if (hex.Length == 8)
            {
                a = System.Convert.ToByte(hex[..2], 16);
                hex = hex[2..];
            }
            if (hex.Length != 6) return null;
            return new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(a,
                System.Convert.ToByte(hex[..2], 16), System.Convert.ToByte(hex[2..4], 16), System.Convert.ToByte(hex[4..6], 16)));
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language) => throw new NotImplementedException();
}

public sealed class ViewModeTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        bool isDouble = value is true;
        return isDouble ? "Double Page" : "Single Page";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is not true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        return value is not true;
    }
}

public sealed class StringToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is string path && !string.IsNullOrWhiteSpace(path))
        {
            try
            {
                if (System.IO.File.Exists(path))
                {
                    var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    bitmap.CreateOptions = Microsoft.UI.Xaml.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                    bitmap.UriSource = new Uri(path);
                    return bitmap;
                }
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed class OcrButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is true ? "Hide OCR Text Layer" : "Detect & Show Text Layer";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}

