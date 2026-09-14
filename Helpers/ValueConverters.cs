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

