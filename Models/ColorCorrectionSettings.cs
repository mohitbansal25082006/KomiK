using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Komik.Models;

/// <summary>
/// User-adjustable color correction and night reading mode settings.
/// </summary>
public sealed class ColorCorrectionSettings : ObservableObject
{
    private double _brightness; // -100 to +100
    private double _contrast = 1.0; // 0.5 to 2.0
    private double _warmth; // -100 to +100 (negative: cooler/de-yellow, positive: warmer/amber)
    private bool _isNightMode;

    public double Brightness
    {
        get => _brightness;
        set => SetProperty(ref _brightness, Math.Clamp(value, -100, 100));
    }

    public double Contrast
    {
        get => _contrast;
        set => SetProperty(ref _contrast, Math.Clamp(value, 0.5, 2.0));
    }

    public double Warmth
    {
        get => _warmth;
        set => SetProperty(ref _warmth, Math.Clamp(value, -100, 100));
    }

    public bool IsNightMode
    {
        get => _isNightMode;
        set
        {
            if (SetProperty(ref _isNightMode, value))
            {
                if (value)
                {
                    ApplyNightModePreset();
                }
                else
                {
                    Reset();
                }
            }
        }
    }

    public bool HasAdjustments => Math.Abs(Brightness) > 0.1 || Math.Abs(Contrast - 1.0) > 0.02 || Math.Abs(Warmth) > 0.1 || IsNightMode;

    public void Reset()
    {
        _isNightMode = false;
        Brightness = 0;
        Contrast = 1.0;
        Warmth = 0;
        OnPropertyChanged(nameof(IsNightMode));
        OnPropertyChanged(nameof(HasAdjustments));
    }

    public void ApplyNightModePreset()
    {
        Brightness = -25;
        Contrast = 1.1;
        Warmth = 15;
        _isNightMode = true;
        OnPropertyChanged(nameof(IsNightMode));
        OnPropertyChanged(nameof(HasAdjustments));
    }
}
