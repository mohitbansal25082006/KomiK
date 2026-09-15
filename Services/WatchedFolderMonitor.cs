using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Komik.Services;

/// <summary>
/// Watches the library's watched folders while Komik runs and indexes comics that appear in them
/// (for example a comic KomiK Downloader just saved). Browsers write downloads under a temporary name
/// and rename them when done, so both new files and renames are handled; a file is only indexed once
/// its size has stopped changing and it can be opened.
/// </summary>
public static class WatchedFolderMonitor
{
    private static readonly HashSet<string> ComicExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cbz", ".zip", ".cbr", ".rar", ".cb7", ".7z", ".pdf"
    };

    private static readonly object Sync = new();
    private static readonly Dictionary<string, FileSystemWatcher> Watchers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, (long Size, int Attempts)> Pending = new(StringComparer.OrdinalIgnoreCase);
    private static Timer? _debounce;
    private static ILibraryScannerService? _scanner;
    private static bool _processing;

    private static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(2);
    private const int MaxAttempts = 90; // about three minutes for a large download to finish

    /// <summary>Raised on a background thread with the number of comics added.</summary>
    public static event Action<int>? ComicsAdded;

    /// <summary>Starts or updates monitoring so exactly the given folders are watched.</summary>
    public static void Watch(ILibraryScannerService scanner, IEnumerable<string> folders)
    {
        lock (Sync)
        {
            _scanner = scanner;
            var wanted = new HashSet<string>(folders.Where(Directory.Exists), StringComparer.OrdinalIgnoreCase);

            foreach (var gone in Watchers.Keys.Where(k => !wanted.Contains(k)).ToList())
            {
                Watchers[gone].Dispose();
                Watchers.Remove(gone);
            }

            foreach (var folder in wanted.Where(f => !Watchers.ContainsKey(f)))
            {
                try
                {
                    var watcher = new FileSystemWatcher(folder)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastWrite,
                        InternalBufferSize = 64 * 1024
                    };
                    watcher.Created += (_, e) => Enqueue(e.FullPath);
                    watcher.Renamed += (_, e) => Enqueue(e.FullPath);
                    watcher.Changed += (_, e) => Enqueue(e.FullPath);
                    watcher.EnableRaisingEvents = true;
                    Watchers[folder] = watcher;
                }
                catch
                {
                    // Network or permission-restricted folders can't be watched; rescans still cover them.
                }
            }
        }
    }

    private static void Enqueue(string path)
    {
        if (!ComicExtensions.Contains(Path.GetExtension(path))) return;
        lock (Sync)
        {
            if (!Pending.ContainsKey(path)) Pending[path] = (-1, 0);
            ScheduleLocked();
        }
    }

    private static void ScheduleLocked()
    {
        _debounce ??= new Timer(_ => _ = ProcessAsync(), null, Timeout.Infinite, Timeout.Infinite);
        _debounce.Change(SettleDelay, Timeout.InfiniteTimeSpan);
    }

    private static async Task ProcessAsync()
    {
        List<string> ready = new();
        ILibraryScannerService? scanner;
        lock (Sync)
        {
            if (_processing)
            {
                ScheduleLocked();
                return;
            }
            _processing = true;
            scanner = _scanner;

            foreach (var (path, state) in Pending.ToList())
            {
                long size = TryGetSize(path);
                if (size < 0 && !File.Exists(path))
                {
                    Pending.Remove(path); // renamed away or deleted
                }
                else if (size > 0 && size == state.Size && CanOpen(path))
                {
                    Pending.Remove(path);
                    ready.Add(path);
                }
                else if (state.Attempts >= MaxAttempts)
                {
                    Pending.Remove(path);
                }
                else
                {
                    Pending[path] = (size, state.Attempts + 1);
                }
            }
        }

        try
        {
            if (scanner != null && ready.Count > 0)
            {
                int added = await scanner.IndexNewSourcesAsync(ready);
                if (added > 0) ComicsAdded?.Invoke(added);
            }
        }
        catch
        {
            // A failed quick index is picked up again by the next rescan.
        }
        finally
        {
            lock (Sync)
            {
                _processing = false;
                if (Pending.Count > 0) ScheduleLocked();
            }
        }
    }

    private static long TryGetSize(string path)
    {
        try { return File.Exists(path) ? new FileInfo(path).Length : -1; }
        catch { return -1; }
    }

    private static bool CanOpen(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
