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

    private ReadingPreset _preset = ReadingPreset.Original;

    public ReadingPreset Preset
    {
        get => _preset;
        set
        {
            if (SetProperty(ref _preset, value))
            {
                ApplyPreset(value);
            }
        }
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
                    Preset = ReadingPreset.NightMode;
                }
                else if (_preset == ReadingPreset.NightMode)
                {
                    Preset = ReadingPreset.Original;
                }
            }
        }
    }

    public bool HasAdjustments =>
        Math.Abs(Brightness) > 0.1 ||
        Math.Abs(Contrast - 1.0) > 0.02 ||
        Math.Abs(Warmth) > 0.1 ||
        Preset != ReadingPreset.Original;

    public void Reset()
    {
        _preset = ReadingPreset.Original;
        _isNightMode = false;
        Brightness = 0;
        Contrast = 1.0;
        Warmth = 0;
        OnPropertyChanged(nameof(Preset));
        OnPropertyChanged(nameof(IsNightMode));
        OnPropertyChanged(nameof(HasAdjustments));
    }

    public void ApplyNightModePreset()
    {
        ApplyPreset(ReadingPreset.NightMode);
    }

    public void ApplyPreset(ReadingPreset preset)
    {
        _preset = preset;
        switch (preset)
        {
            case ReadingPreset.NightMode:
                _brightness = -25;
                _contrast = 1.1;
                _warmth = 18;
                _isNightMode = true;
                break;
            case ReadingPreset.Sepia:
                _brightness = -10;
                _contrast = 1.05;
                _warmth = 38;
                _isNightMode = false;
                break;
            case ReadingPreset.HighContrast:
                _brightness = 5;
                _contrast = 1.55;
                _warmth = 0;
                _isNightMode = false;
                break;
            case ReadingPreset.Grayscale:
                _brightness = 0;
                _contrast = 1.1;
                _warmth = 0;
                _isNightMode = false;
                break;
            case ReadingPreset.Inverted:
                _brightness = 0;
                _contrast = 1.0;
                _warmth = 0;
                _isNightMode = false;
                break;
            case ReadingPreset.Original:
            default:
                _brightness = 0;
                _contrast = 1.0;
                _warmth = 0;
                _isNightMode = false;
                break;
        }

        OnPropertyChanged(nameof(Preset));
        OnPropertyChanged(nameof(Brightness));
        OnPropertyChanged(nameof(Contrast));
        OnPropertyChanged(nameof(Warmth));
        OnPropertyChanged(nameof(IsNightMode));
        OnPropertyChanged(nameof(HasAdjustments));
    }
}

