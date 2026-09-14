using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Komik.Models;

namespace Komik.Services;

public interface IOcrService
{
    bool IsOcrSupported { get; }
    Task<OcrPageResult?> RecognizePageAsync(int pageIndex, ComicPageData pageData);
    Task<OcrPageResult?> RecognizePageAsync(int pageIndex, byte[] rawEncodedBytes);
    Task<List<OcrSearchResultItem>> SearchInComicAsync(ComicBook comic, string query, IProgress<double>? progress = null, CancellationToken ct = default);
    void ClearCache();
}

public class OcrService : IOcrService
{
    private OcrEngine? _ocrEngine;
    private bool _engineInitialized;
    private readonly ConcurrentDictionary<int, OcrPageResult> _ocrCache = new();

    public bool IsOcrSupported
    {
        get
        {
            EnsureEngine();
            return _ocrEngine != null;
        }
    }

    private void EnsureEngine()
    {
        if (_engineInitialized) return;
        _engineInitialized = true;
        try
        {
            _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (_ocrEngine == null && OcrEngine.AvailableRecognizerLanguages.Count > 0)
            {
                _ocrEngine = OcrEngine.TryCreateFromLanguage(OcrEngine.AvailableRecognizerLanguages[0]);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OcrService] Failed to initialize Windows OcrEngine: {ex.Message}");
        }
    }

    public Task<OcrPageResult?> RecognizePageAsync(int pageIndex, byte[] rawEncodedBytes)
    {
        if (rawEncodedBytes == null || rawEncodedBytes.Length == 0) return Task.FromResult<OcrPageResult?>(null);
        return RecognizePageAsync(pageIndex, new ComicPageData(rawEncodedBytes));
    }

    public async Task<OcrPageResult?> RecognizePageAsync(int pageIndex, ComicPageData pageData)
    {
        if (pageData == null || pageData.Data == null || pageData.Data.Length == 0) return null;

        if (_ocrCache.TryGetValue(pageIndex, out var cached))
        {
            return cached;
        }

        EnsureEngine();
        if (_ocrEngine == null) return null;

        try
        {
            SoftwareBitmap softwareBitmap;
            if (pageData.IsRawBgra && pageData.Width > 0 && pageData.Height > 0)
            {
                softwareBitmap = new SoftwareBitmap(
                    BitmapPixelFormat.Bgra8,
                    pageData.Width,
                    pageData.Height,
                    BitmapAlphaMode.Premultiplied);
                softwareBitmap.CopyFromBuffer(pageData.Data.AsBuffer());
            }
            else
            {
                using var stream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(pageData.Data);
                    await writer.StoreAsync();
                }
                stream.Seek(0);

                var decoder = await BitmapDecoder.CreateAsync(stream);
                softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied);
            }

            using (softwareBitmap)
            {
                var ocrResult = await _ocrEngine.RecognizeAsync(softwareBitmap);
                if (ocrResult == null) return null;

                var pageResult = new OcrPageResult
                {
                    PageIndex = pageIndex,
                    FullText = ocrResult.Text,
                    ImageWidth = softwareBitmap.PixelWidth,
                    ImageHeight = softwareBitmap.PixelHeight
                };

                foreach (var line in ocrResult.Lines)
                {
                    foreach (var word in line.Words)
                    {
                        pageResult.Words.Add(new OcrWordBox
                        {
                            Text = word.Text,
                            X = word.BoundingRect.X,
                            Y = word.BoundingRect.Y,
                            Width = word.BoundingRect.Width,
                            Height = word.BoundingRect.Height
                        });
                    }
                }

                _ocrCache[pageIndex] = pageResult;
                return pageResult;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OcrService] OCR recognition failed for page {pageIndex}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<OcrSearchResultItem>> SearchInComicAsync(ComicBook comic, string query, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var results = new List<OcrSearchResultItem>();
        if (comic == null || string.IsNullOrWhiteSpace(query)) return results;

        EnsureEngine();
        if (_ocrEngine == null) return results;

        int total = comic.PageCount;
        for (int i = 0; i < total; i++)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                OcrPageResult? pageOcr = null;
                if (_ocrCache.TryGetValue(i, out var cached))
                {
                    pageOcr = cached;
                }
                else if (i < comic.Pages.Count)
                {
                    var pageData = await comic.Pages[i].GetPageDataAsync(ct);
                    pageOcr = await RecognizePageAsync(i, pageData);
                }

                if (pageOcr != null && pageOcr.ContainsText(query))
                {
                    string snippet = ExtractSnippet(pageOcr.FullText, query);
                    results.Add(new OcrSearchResultItem
                    {
                        PageIndex = i,
                        Snippet = snippet
                    });
                }
            }
            catch { }

            progress?.Report((double)(i + 1) / total * 100.0);
        }

        return results;
    }

    private static string ExtractSnippet(string text, string query)
    {
        int idx = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return text.Length > 80 ? text.Substring(0, 80) + "..." : text;

        int start = Math.Max(0, idx - 30);
        int end = Math.Min(text.Length, idx + query.Length + 40);
        string snippet = text.Substring(start, end - start).Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (start > 0) snippet = "..." + snippet;
        if (end < text.Length) snippet += "...";
        return snippet;
    }

    public void ClearCache()
    {
        _ocrCache.Clear();
    }
}
