using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using Komik.Models;

namespace Komik.Services;

/// <summary>
/// Loads comic pages from PDF documents using PDFium via Docnet.Core.
/// </summary>
public sealed class PdfComicLoader : IComicLoader
{
    private const double RenderScale = 2.0; // ~144 DPI for crisp reading

    public bool CanLoad(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        return string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ComicBook> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The PDF file '{path}' does not exist.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        IDocReader docReader;
        try
        {
            docReader = DocLib.Instance.GetDocReader(path, new PageDimensions(RenderScale));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load PDF document '{Path.GetFileName(path)}': {ex.Message}", ex);
        }

        int pageCount = docReader.GetPageCount();
        if (pageCount <= 0)
        {
            docReader.Dispose();
            throw new InvalidOperationException($"PDF document '{Path.GetFileName(path)}' contains no pages.");
        }

        string title = Path.GetFileNameWithoutExtension(path);
        var pdfContext = new PdfDocumentContext(docReader);

        var pages = new List<IComicPage>(pageCount);
        for (int i = 0; i < pageCount; i++)
        {
            pages.Add(new PdfComicPage(i, $"Page {i + 1}", pdfContext));
        }

        var comicBook = new ComicBook(title, path, ComicSourceType.PdfDocument, pages);
        return Task.FromResult(comicBook);
    }

    private sealed class PdfDocumentContext : IDisposable
    {
        private readonly IDocReader _docReader;
        private readonly object _syncLock = new();
        private bool _disposed;

        public PdfDocumentContext(IDocReader docReader)
        {
            _docReader = docReader;
        }

        public (byte[] Data, int Width, int Height) RenderPage(int pageIndex)
        {
            lock (_syncLock)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(PdfDocumentContext));

                using var pageReader = _docReader.GetPageReader(pageIndex);
                int width = pageReader.GetPageWidth();
                int height = pageReader.GetPageHeight();
                byte[] rawBgra = pageReader.GetImage();
                return (rawBgra, width, height);
            }
        }

        public void Dispose()
        {
            lock (_syncLock)
            {
                if (_disposed) return;
                _disposed = true;
                _docReader.Dispose();
            }
        }
    }

    private sealed class PdfComicPage : IComicPage
    {
        private readonly PdfDocumentContext _context;

        public int Index { get; }
        public string DisplayName { get; }

        public PdfComicPage(int index, string displayName, PdfDocumentContext context)
        {
            Index = index;
            DisplayName = displayName;
            _context = context;
        }

        public Task<ComicPageData> GetPageDataAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (bytes, width, height) = _context.RenderPage(Index);
            return Task.FromResult(new ComicPageData(bytes, isRawBgra: true, width, height));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
