using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Komik.Services;

/// <summary>
/// Service abstraction for native Windows file and folder pickers.
/// </summary>
public interface IFilePickerService
{
    Task<string?> PickComicFileAsync();
    Task<string?> PickComicFolderAsync();
}

/// <summary>
/// WinUI 3 implementation of IFilePickerService with Window Handle initialization.
/// </summary>
public sealed class FilePickerService : IFilePickerService
{
    private static readonly string[] ComicExtensions =
    {
        ".cbz", ".zip", ".cbr", ".rar", ".cb7", ".7z", ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".bmp"
    };

    public async Task<string?> PickComicFileAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            ViewMode = PickerViewMode.Thumbnail
        };

        foreach (var ext in ComicExtensions)
        {
            picker.FileTypeFilter.Add(ext);
        }

        nint hwnd = App.WindowHandle;
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    public async Task<string?> PickComicFolderAsync()
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail
        };

        picker.FileTypeFilter.Add("*");

        nint hwnd = App.WindowHandle;
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
