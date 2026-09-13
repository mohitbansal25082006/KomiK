using System.Threading;
using System.Threading.Tasks;
using Komik.Models;

namespace Komik.Services;

/// <summary>
/// Defines an abstraction for loading comics from various sources (folders, ZIP/CBZ archives, PDFs).
/// </summary>
public interface IComicLoader
{
    /// <summary>
    /// Checks whether this loader can handle the specified file or directory path.
    /// </summary>
    bool CanLoad(string path);

    /// <summary>
    /// Asynchronously loads comic book metadata and pages from the given path.
    /// </summary>
    Task<ComicBook> LoadAsync(string path, CancellationToken cancellationToken = default);
}
