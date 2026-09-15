using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Komik.Models;

namespace Komik.Services;

/// <summary>An OCR language installed in Windows.</summary>
public sealed record OcrLanguageOption(string Tag, string DisplayName)
{
    public override string ToString() => DisplayName;
}

public interface IOcrService
{
    bool IsOcrSupported { get; }
    IReadOnlyList<OcrLanguageOption> Languages { get; }

    /// <summary>Language to read (null picks the best match for the user's Windows languages).</summary>
    string? LanguageTag { get; set; }

    /// <summary>Order speech bubbles right-to-left (manga).</summary>
    bool RightToLeft { get; set; }

    Task<OcrPageResult?> RecognizePageAsync(int pageIndex, ComicPageData pageData, CancellationToken cancellationToken = default);
    Task<OcrPageResult?> RecognizePageAsync(int pageIndex, byte[] rawEncodedBytes);
    Task<List<OcrSearchResultItem>> SearchInComicAsync(ComicBook comic, string query, IProgress<double>? progress = null, CancellationToken ct = default, Func<int, CancellationToken, Task<ComicPageData>>? pageProvider = null);
    void ClearCache();
}

/// <summary>
/// On-device OCR tuned for comics: small lettering is upscaled, tall webtoon strips are read in overlapping bands,
/// and the text comes back grouped into speech bubbles in reading order.
/// </summary>
public class OcrService : IOcrService
{
    private readonly ConcurrentDictionary<int, OcrPageResult> _ocrCache = new();
    private readonly Dictionary<string, OcrEngine?> _engines = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _recognizeLock = new(1, 1);
    private string? _languageTag;
    private bool _rightToLeft;
    private IReadOnlyList<OcrLanguageOption>? _languages;

    public bool IsOcrSupported => GetEngine() != null;

    public IReadOnlyList<OcrLanguageOption> Languages
    {
        get
        {
            if (_languages != null) return _languages;
            try
            {
                _languages = OcrEngine.AvailableRecognizerLanguages
                    .Select(l => new OcrLanguageOption(l.LanguageTag, l.DisplayName))
                    .OrderBy(l => l.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
            catch
            {
                _languages = Array.Empty<OcrLanguageOption>();
            }
            return _languages;
        }
    }

    public string? LanguageTag
    {
        get => _languageTag;
        set
        {
            if (string.Equals(_languageTag, value, StringComparison.OrdinalIgnoreCase)) return;
            _languageTag = value;
            ClearCache();
        }
    }

    public bool RightToLeft
    {
        get => _rightToLeft;
        set
        {
            if (_rightToLeft == value) return;
            _rightToLeft = value;
            ClearCache();
        }
    }

    private OcrEngine? GetEngine()
    {
        string key = _languageTag ?? "(user)";
        lock (_engines)
        {
            if (_engines.TryGetValue(key, out var cached)) return cached;
            OcrEngine? engine = null;
            try
            {
                if (_languageTag != null)
                {
                    engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language(_languageTag));
                }
                engine ??= OcrEngine.TryCreateFromUserProfileLanguages();
                if (engine == null && OcrEngine.AvailableRecognizerLanguages.Count > 0)
                {
                    engine = OcrEngine.TryCreateFromLanguage(OcrEngine.AvailableRecognizerLanguages[0]);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OcrService] Failed to create OCR engine: {ex.Message}");
            }
            _engines[key] = engine;
            return engine;
        }
    }

    public Task<OcrPageResult?> RecognizePageAsync(int pageIndex, byte[] rawEncodedBytes)
    {
        if (rawEncodedBytes == null || rawEncodedBytes.Length == 0) return Task.FromResult<OcrPageResult?>(null);
        return RecognizePageAsync(pageIndex, new ComicPageData(rawEncodedBytes));
    }

    public async Task<OcrPageResult?> RecognizePageAsync(int pageIndex, ComicPageData pageData, CancellationToken cancellationToken = default)
    {
        if (pageData?.Data == null || pageData.Data.Length == 0) return null;
        if (_ocrCache.TryGetValue(pageIndex, out var cached)) return cached;

        var engine = GetEngine();
        if (engine == null) return null;

        await _recognizeLock.WaitAsync(cancellationToken);
        try
        {
            if (_ocrCache.TryGetValue(pageIndex, out cached)) return cached;

            using var stream = await ToEncodedStreamAsync(pageData);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            int width = (int)decoder.OrientedPixelWidth;
            int height = (int)decoder.OrientedPixelHeight;
            if (width <= 0 || height <= 0) return null;

            int maxDimension = (int)Math.Max(1024, OcrEngine.MaxImageDimension);

            // Small lettering reads much better enlarged; never exceed what the engine accepts.
            double scale = width < 1400 ? Math.Min(2.0, 1800.0 / width) : 1.0;
            scale = Math.Max(0.25, Math.Min(scale, maxDimension / (double)width));

            // Tall strips (webtoons, long scans) are read in overlapping bands.
            int tileHeight = (int)Math.Max(600, Math.Min(maxDimension / scale, Math.Max(1600, width * 1.6)));
            int overlap = Math.Min(tileHeight / 4, 320);
            var tiles = OcrLayout.PlanTiles(height, tileHeight, overlap);

            uint scaledWidth = (uint)Math.Max(1, Math.Round(width * scale));
            uint scaledHeight = (uint)Math.Max(1, Math.Round(height * scale));

            var result = new OcrPageResult
            {
                PageIndex = pageIndex,
                ImageWidth = width,
                ImageHeight = height,
                LanguageName = engine.RecognizerLanguage?.DisplayName ?? string.Empty
            };

            for (int t = 0; t < tiles.Count; t++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (top, tileH) = tiles[t];
                uint y = (uint)Math.Min(scaledHeight - 1, Math.Round(top * scale));
                uint h = (uint)Math.Max(1, Math.Min(scaledHeight - y, Math.Round(tileH * scale)));

                var transform = new BitmapTransform
                {
                    ScaledWidth = scaledWidth,
                    ScaledHeight = scaledHeight,
                    InterpolationMode = BitmapInterpolationMode.Fant,
                    Bounds = new BitmapBounds { X = 0, Y = y, Width = scaledWidth, Height = h }
                };

                using var bitmap = await decoder.GetSoftwareBitmapAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    transform,
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.DoNotColorManage);

                var ocr = await engine.RecognizeAsync(bitmap);
                if (ocr == null) continue;

                double keepFrom = t == 0 ? double.MinValue : top + overlap / 2.0;
                double keepTo = t == tiles.Count - 1 ? double.MaxValue : top + tileH - overlap / 2.0;

                foreach (var line in ocr.Lines)
                {
                    if (line.Words.Count == 0) continue;
                    double left = line.Words.Min(w => w.BoundingRect.X) / scale;
                    double lineTop = line.Words.Min(w => w.BoundingRect.Y) / scale + top;
                    double right = line.Words.Max(w => w.BoundingRect.X + w.BoundingRect.Width) / scale;
                    double bottom = line.Words.Max(w => w.BoundingRect.Y + w.BoundingRect.Height) / scale + top;
                    double centerY = (lineTop + bottom) / 2;
                    if (centerY < keepFrom || centerY >= keepTo) continue;

                    result.Lines.Add(new OcrLineBox { Text = line.Text, X = left, Y = lineTop, Width = right - left, Height = bottom - lineTop });
                    foreach (var word in line.Words)
                    {
                        result.Words.Add(new OcrWordBox
                        {
                            Text = word.Text,
                            X = word.BoundingRect.X / scale,
                            Y = word.BoundingRect.Y / scale + top,
                            Width = word.BoundingRect.Width / scale,
                            Height = word.BoundingRect.Height / scale
                        });
                    }
                }
            }

            string tag = engine.RecognizerLanguage?.LanguageTag ?? string.Empty;
            bool noSpaces = tag.StartsWith("ja", StringComparison.OrdinalIgnoreCase) || tag.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
            OcrLayout.BuildBlocks(result, _rightToLeft, noSpaces);

            _ocrCache[pageIndex] = result;
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OcrService] OCR recognition failed for page {pageIndex}: {ex.Message}");
            return null;
        }
        finally
        {
            _recognizeLock.Release();
        }
    }

    /// <summary>Decoded PDF pages arrive as raw pixels; everything else is already an encoded image.</summary>
    private static async Task<InMemoryRandomAccessStream> ToEncodedStreamAsync(ComicPageData pageData)
    {
        var stream = new InMemoryRandomAccessStream();
        if (pageData.IsRawBgra && pageData.Width > 0 && pageData.Height > 0)
        {
            using var bitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, pageData.Width, pageData.Height, BitmapAlphaMode.Premultiplied);
            bitmap.CopyFromBuffer(pageData.Data.AsBuffer());
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetSoftwareBitmap(bitmap);
            await encoder.FlushAsync();
        }
        else
        {
            using var writer = new DataWriter(stream.GetOutputStreamAt(0));
            writer.WriteBytes(pageData.Data);
            await writer.StoreAsync();
            await writer.FlushAsync();
            writer.DetachStream();
        }
        stream.Seek(0);
        return stream;
    }

    public async Task<List<OcrSearchResultItem>> SearchInComicAsync(ComicBook comic, string query, IProgress<double>? progress = null, CancellationToken ct = default, Func<int, CancellationToken, Task<ComicPageData>>? pageProvider = null)
    {
        var results = new List<OcrSearchResultItem>();
        if (comic == null || string.IsNullOrWhiteSpace(query) || GetEngine() == null) return results;

        int total = comic.PageCount;
        for (int i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (!_ocrCache.TryGetValue(i, out var pageOcr) && i < comic.Pages.Count)
                {
                    var pageData = pageProvider != null ? await pageProvider(i, ct) : await comic.Pages[i].GetPageDataAsync(ct);
                    pageOcr = await RecognizePageAsync(i, pageData, ct);
                }

                if (pageOcr != null && pageOcr.ContainsText(query))
                {
                    results.Add(new OcrSearchResultItem { PageIndex = i, Snippet = ExtractSnippet(pageOcr, query) });
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Unreadable page: keep searching the rest.
            }

            progress?.Report((double)(i + 1) / total * 100.0);
        }

        return results;
    }

    private static string ExtractSnippet(OcrPageResult page, string query)
    {
        var block = page.Blocks.FirstOrDefault(b => b.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
                    ?? page.Blocks.FirstOrDefault(b => OcrPageResult.Squeeze(b.Text).Contains(OcrPageResult.Squeeze(query), StringComparison.OrdinalIgnoreCase));
        string text = (block?.Text ?? page.FullText).Replace('\r', ' ').Replace('\n', ' ').Trim();

        int idx = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return text.Length > 90 ? text[..90] + "..." : text;

        int start = Math.Max(0, idx - 35);
        int end = Math.Min(text.Length, idx + query.Length + 45);
        string snippet = text[start..end];
        if (start > 0) snippet = "..." + snippet;
        if (end < text.Length) snippet += "...";
        return snippet;
    }

    public void ClearCache()
    {
        _ocrCache.Clear();
    }
}
