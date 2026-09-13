using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Komik.Models;

namespace Komik.Services;

/// <summary>
/// Service interface for dispatching comic file/folder paths to the appropriate IComicLoader.
/// </summary>
public interface IComicLoaderService
{
    Task<ComicBook> LoadComicAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of IComicLoaderService, managing Folder, Zip/CBZ, Rar/CBR, 7Z/CB7, and PDF loaders.
/// </summary>
public sealed class ComicLoaderService : IComicLoaderService
{
    private readonly List<IComicLoader> _loaders;

    public ComicLoaderService(IEnumerable<IComicLoader>? loaders = null)
    {
        _loaders = loaders?.ToList() ?? new List<IComicLoader>
        {
            new FolderComicLoader(),
            new ZipComicLoader(),
            new RarComicLoader(),
            new SevenZipComicLoader(),
            new PdfComicLoader()
        };
    }

    public async Task<ComicBook> LoadComicAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be empty.", nameof(path));
        }

        foreach (var loader in _loaders)
        {
            if (loader.CanLoad(path))
            {
                return await loader.LoadAsync(path, cancellationToken);
            }
        }

        string fileName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(fileName)) fileName = path;

        if (Directory.Exists(path))
        {
            throw new InvalidOperationException($"No supported comic images found in folder '{fileName}'.");
        }

        throw new NotSupportedException($"The format of '{fileName}' is not supported. Supported formats: Folders of images, .cbz/.zip archives, .cbr/.rar archives, .cb7/.7z archives, and .pdf files.");
    }
}
