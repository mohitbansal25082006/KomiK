using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Komik.Services;

/// <summary>
/// Deletes comic files without any Windows prompts: Recycle Bin first, retried while a lingering handle closes,
/// then a permanent delete as the last resort.
/// </summary>
public static class FileDeletion
{
    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_SILENT = 0x0004;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOERRORUI = 0x0400;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

    private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    public static async Task DeleteToRecycleBinAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Exists(path)) return;

        int[] waits = { 0, 150, 300, 500, 800, 1200 };
        foreach (int wait in waits)
        {
            if (wait > 0)
            {
                // Pages or archives the app itself still had open are released by their finalizers.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(wait);
            }

            TryRecycle(path);
            if (!Exists(path)) return;
        }

        try
        {
            File.SetAttributes(path, FileAttributes.Normal);
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
            else File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"Windows couldn't delete '{Path.GetFileName(path)}' because another program is using it. Close that program and try again.", ex);
        }
    }

    private static void TryRecycle(string path)
    {
        try
        {
            var op = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = path + "\0\0",
                fFlags = (ushort)(FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_SILENT)
            };
            SHFileOperation(ref op);
        }
        catch
        {
            // Fall through to the retry / permanent delete path.
        }
    }
}
