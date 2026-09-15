using System;
using System.Threading;
using System.Threading.Tasks;
using Komik.Models;

namespace Komik.Services;

public sealed class ScanProgressEventArgs : EventArgs
{
    public string StatusMessage { get; }
    public int DiscoveredCount { get; }
    public bool IsCompleted { get; }

    public ScanProgressEventArgs(string statusMessage, int discoveredCount, bool isCompleted = false)
    {
        StatusMessage = statusMessage;
        DiscoveredCount = discoveredCount;
        IsCompleted = isCompleted;
    }
}

/// <summary>
/// Service abstraction for scanning directories and indexing comics into the library database.
/// </summary>
public interface ILibraryScannerService
{
    event EventHandler<ScanProgressEventArgs>? ProgressChanged;

    bool IsScanning { get; }

    Task ScanFolderAsync(string rootPath, CancellationToken cancellationToken = default);
    Task RescanAllAsync(CancellationToken cancellationToken = default);
    Task<ComicEntity?> IndexSingleComicAsync(string path, CancellationToken cancellationToken = default);
    Task<System.Collections.Generic.List<(string Path, ComicSourceType Format)>> FindComicSourcesAsync(System.Collections.Generic.IEnumerable<string> roots, CancellationToken cancellationToken = default);
    Task<int> IndexComicsAsync(System.Collections.Generic.IReadOnlyList<(string Path, ComicSourceType Format)> sources, IProgress<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Quietly indexes new comics (e.g. finished downloads in a watched folder), skipping known and removed ones.</summary>
    Task<int> IndexNewSourcesAsync(System.Collections.Generic.IEnumerable<string> paths, CancellationToken cancellationToken = default);

    /// <summary>Indexes comics added to watched folders while Komik was closed and imports embedded ComicInfo.xml metadata once.</summary>
    Task<int> SyncWatchedFoldersQuietlyAsync(CancellationToken cancellationToken = default);

    /// <summary>Fills title, details and tags from ComicInfo.xml for comics that have no metadata yet (once per library unless forced).</summary>
    Task<int> ImportEmbeddedMetadataAsync(bool force = false, CancellationToken cancellationToken = default);
}
