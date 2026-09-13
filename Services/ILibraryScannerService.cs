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
}
