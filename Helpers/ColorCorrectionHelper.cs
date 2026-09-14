using System;
using Komik.Models;

namespace Komik.Helpers;

/// <summary>
/// High-performance channel lookup table (LUT) processing for comic pages.
/// Pure C# memory manipulation with zero WinUI / GPU dependencies.
/// </summary>
public static class ColorCorrectionHelper
{
    /// <summary>
    /// Applies Brightness, Contrast, and Warmth/Cooling (de-yellowing) color adjustments using a 256-element LUT.
    /// </summary>
    public static byte[] ApplyColorCorrection(byte[] bgraBytes, ColorCorrectionSettings settings)
    {
        byte[] output = new byte[bgraBytes.Length];

        // Build 256-entry lookup tables for B, G, R channels
        byte[] lutB = new byte[256];
        byte[] lutG = new byte[256];
        byte[] lutR = new byte[256];

        double bAdj = settings.Brightness * 1.5; // -150 to +150
        double contrast = settings.Contrast;    // 0.5 to 2.0
        double warmth = settings.Warmth;        // -100 (cooler) to +100 (warmer)

        if (settings.IsNightMode && Math.Abs(bAdj) < 0.001 && Math.Abs(contrast - 1.0) < 0.01 && Math.Abs(warmth) < 0.001)
        {
            bAdj = -25 * 1.5;
            contrast = 1.1;
            warmth = 15;
        }

        for (int i = 0; i < 256; i++)
        {
            double baseVal = (i - 128.0) * contrast + 128.0 + bAdj;

            double rVal = baseVal + (warmth * 0.4);
            double gVal = baseVal + (warmth * 0.1);
            double bVal = baseVal - (warmth * 0.4);

            lutR[i] = (byte)Math.Clamp(Math.Round(rVal), 0, 255);
            lutG[i] = (byte)Math.Clamp(Math.Round(gVal), 0, 255);
            lutB[i] = (byte)Math.Clamp(Math.Round(bVal), 0, 255);
        }

        Span<byte> srcSpan = bgraBytes.AsSpan();
        Span<byte> destSpan = output.AsSpan();

        if (settings.Preset == ReadingPreset.Grayscale)
        {
            for (int i = 0; i < srcSpan.Length; i += 4)
            {
                byte b = lutB[srcSpan[i]];
                byte g = lutG[srcSpan[i + 1]];
                byte r = lutR[srcSpan[i + 2]];
                byte gray = (byte)Math.Clamp((int)Math.Round(0.299 * r + 0.587 * g + 0.114 * b), 0, 255);

                destSpan[i] = gray;     // B
                destSpan[i + 1] = gray; // G
                destSpan[i + 2] = gray; // R
                destSpan[i + 3] = srcSpan[i + 3]; // Alpha
            }
            return output;
        }

        if (settings.Preset == ReadingPreset.Sepia)
        {
            for (int i = 0; i < srcSpan.Length; i += 4)
            {
                byte b = lutB[srcSpan[i]];
                byte g = lutG[srcSpan[i + 1]];
                byte r = lutR[srcSpan[i + 2]];

                int sepiaR = (int)(0.393 * r + 0.769 * g + 0.189 * b);
                int sepiaG = (int)(0.349 * r + 0.686 * g + 0.168 * b);
                int sepiaB = (int)(0.272 * r + 0.534 * g + 0.131 * b);

                destSpan[i] = (byte)Math.Clamp(sepiaB, 0, 255);
                destSpan[i + 1] = (byte)Math.Clamp(sepiaG, 0, 255);
                destSpan[i + 2] = (byte)Math.Clamp(sepiaR, 0, 255);
                destSpan[i + 3] = srcSpan[i + 3];
            }
            return output;
        }

        if (settings.Preset == ReadingPreset.Inverted)
        {
            for (int i = 0; i < srcSpan.Length; i += 4)
            {
                destSpan[i] = (byte)(255 - lutB[srcSpan[i]]);
                destSpan[i + 1] = (byte)(255 - lutG[srcSpan[i + 1]]);
                destSpan[i + 2] = (byte)(255 - lutR[srcSpan[i + 2]]);
                destSpan[i + 3] = srcSpan[i + 3];
            }
            return output;
        }

        for (int i = 0; i < srcSpan.Length; i += 4)
        {
            destSpan[i] = lutB[srcSpan[i]];         // B
            destSpan[i + 1] = lutG[srcSpan[i + 1]]; // G
            destSpan[i + 2] = lutR[srcSpan[i + 2]]; // R
            destSpan[i + 3] = srcSpan[i + 3];       // Alpha unchanged
        }

        return output;
    }
}

