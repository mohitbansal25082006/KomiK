using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Komik.Models;

namespace Komik.Helpers;

/// <summary>
/// Helper methods to convert raw comic page data into WinUI 3 ImageSource objects.
/// </summary>
public static class ImageHelper
{
    public static async Task<ImageSource> CreateImageSourceAsync(ComicPageData pageData, ColorCorrectionSettings? settings = null)
    {
        bool hasAdjustments = settings != null && settings.HasAdjustments;

        if (pageData.IsRawBgra)
        {
            byte[] pixelData = pageData.Data;
            if (hasAdjustments)
            {
                pixelData = ApplyColorCorrection(pageData.Data, settings!);
            }

            var softwareBitmap = new SoftwareBitmap(
                BitmapPixelFormat.Bgra8,
                pageData.Width,
                pageData.Height,
                BitmapAlphaMode.Premultiplied);

            softwareBitmap.CopyFromBuffer(pixelData.AsBuffer());

            var softwareBitmapSource = new SoftwareBitmapSource();
            await softwareBitmapSource.SetBitmapAsync(softwareBitmap);
            return softwareBitmapSource;
        }
        else
        {
            if (hasAdjustments)
            {
                // Decode to raw BGRA to apply color transformations
                using var inStream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(inStream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(pageData.Data);
                    await writer.StoreAsync();
                }

                var decoder = await BitmapDecoder.CreateAsync(inStream);
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                byte[] pixelBytes = new byte[softwareBitmap.PixelWidth * softwareBitmap.PixelHeight * 4];
                softwareBitmap.CopyToBuffer(pixelBytes.AsBuffer());

                byte[] corrected = ApplyColorCorrection(pixelBytes, settings!);

                var correctedBitmap = new SoftwareBitmap(
                    BitmapPixelFormat.Bgra8,
                    softwareBitmap.PixelWidth,
                    softwareBitmap.PixelHeight,
                    BitmapAlphaMode.Premultiplied);
                correctedBitmap.CopyFromBuffer(corrected.AsBuffer());

                var softwareBitmapSource = new SoftwareBitmapSource();
                await softwareBitmapSource.SetBitmapAsync(correctedBitmap);
                return softwareBitmapSource;
            }
            else
            {
                // Encoded image stream without adjustments
                using var stream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(pageData.Data);
                    await writer.StoreAsync();
                }

                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);
                return bitmapImage;
            }
        }
    }

    /// <summary>
    /// Applies Brightness, Contrast, and Warmth/Cooling (de-yellowing) color adjustments using a 256-element LUT.
    /// </summary>
    public static byte[] ApplyColorCorrection(byte[] bgraBytes, ColorCorrectionSettings settings)
    {
        return ColorCorrectionHelper.ApplyColorCorrection(bgraBytes, settings);
    }
}
