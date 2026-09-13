using System;
using System.Threading;
using System.Threading.Tasks;

namespace Komik.Models;

/// <summary>
/// Represents a single page in a comic book.
/// </summary>
public interface IComicPage : IDisposable
{
    /// <summary>
    /// Zero-based page index.
    /// </summary>
    int Index { get; }

    /// <summary>
    /// User-facing display name or filename of the page.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Asynchronously retrieves the page image data on demand.
    /// </summary>
    Task<ComicPageData> GetPageDataAsync(CancellationToken cancellationToken = default);
}
