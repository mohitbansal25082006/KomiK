// Saves finished files into Downloads/KomiK/… (downloads API) or into a folder the user picked.
import { kvGet } from "@/shared/db";
import { send } from "@/shared/messages";
import type { Settings } from "@/shared/types";

export const CUSTOM_FOLDER_KEY = "customFolderHandle";

export interface SavedFile {
  path: string;
  downloadId?: number;
  customFolder: boolean;
}

async function customFolder(): Promise<FileSystemDirectoryHandle | null> {
  const handle = await kvGet<FileSystemDirectoryHandle>(CUSTOM_FOLDER_KEY).catch(() => undefined);
  if (!handle) return null;
  try {
    const state = await (handle as unknown as { queryPermission(o: { mode: string }): Promise<PermissionState> }).queryPermission({ mode: "readwrite" });
    return state === "granted" ? handle : null;
  } catch {
    return null;
  }
}

async function uniqueName(dir: FileSystemDirectoryHandle, name: string, isFolder: boolean): Promise<string> {
  const dot = isFolder ? -1 : name.lastIndexOf(".");
  const stem = dot > 0 ? name.slice(0, dot) : name;
  const ext = dot > 0 ? name.slice(dot) : "";
  for (let n = 1; n < 1000; n++) {
    const candidate = n === 1 ? name : `${stem} (${n})${ext}`;
    try {
      if (isFolder) await dir.getDirectoryHandle(candidate);
      else await dir.getFileHandle(candidate);
    } catch {
      return candidate;
    }
  }
  return `${stem} (${Date.now()})${ext}`;
}

async function writeToFolder(root: FileSystemDirectoryHandle, folder: string, name: string, blob: Blob): Promise<string> {
  let dir = root;
  for (const part of folder.split("/").filter(Boolean)) dir = await dir.getDirectoryHandle(part, { create: true });
  const finalName = await uniqueName(dir, name, false);
  const file = await dir.getFileHandle(finalName, { create: true });
  const writable = await file.createWritable();
  await writable.write(blob);
  await writable.close();
  return [root.name, folder, finalName].filter(Boolean).join("/");
}

async function viaDownloads(relativePath: string, blob: Blob, saveAs: boolean): Promise<SavedFile> {
  const url = URL.createObjectURL(blob);
  try {
    const started = await send("off-download", { url, filename: relativePath, saveAs });
    if (started.downloadId === undefined) throw new Error(started.error || "The browser refused the download.");
    const result = await send("off-wait-download", { downloadId: started.downloadId });
    if (result.state !== "complete") throw new Error(result.error ? `Download ${result.error.toLowerCase().replace(/_/g, " ")}` : "The download was interrupted.");
    return { path: result.filename ?? relativePath, downloadId: started.downloadId, customFolder: false };
  } finally {
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  }
}

/** Saves one file. Returns a warning when a custom folder was chosen but its access has lapsed. */
export async function saveFile(folder: string, fileName: string, blob: Blob, settings: Settings): Promise<SavedFile & { warning?: string }> {
  if (settings.folderMode === "custom") {
    const root = await customFolder();
    if (root) return { path: await writeToFolder(root, folder, fileName, blob), customFolder: true };
    const fallbackFolder = [settings.downloadsSubfolder || "KomiK", folder].filter(Boolean).join("/");
    const saved = await viaDownloads(fallbackFolder ? `${fallbackFolder}/${fileName}` : fileName, blob, settings.askWhereToSave);
    return { ...saved, warning: "Your custom folder needs permission again (Options → Folder), so this was saved to Downloads." };
  }
  return viaDownloads(folder ? `${folder}/${fileName}` : fileName, blob, settings.askWhereToSave);
}

/** Saves an image-folder comic: every page plus ComicInfo.xml into its own folder. */
export async function saveFolder(folder: string, name: string, files: Array<{ name: string; blob: Blob }>, settings: Settings): Promise<SavedFile & { warning?: string }> {
  if (settings.folderMode === "custom") {
    const root = await customFolder();
    if (root) {
      let dir = root;
      for (const part of folder.split("/").filter(Boolean)) dir = await dir.getDirectoryHandle(part, { create: true });
      const finalName = await uniqueName(dir, name, true);
      const target = await dir.getDirectoryHandle(finalName, { create: true });
      for (const f of files) {
        const handle = await target.getFileHandle(f.name, { create: true });
        const w = await handle.createWritable();
        await w.write(f.blob);
        await w.close();
      }
      return { path: [root.name, folder, finalName].filter(Boolean).join("/"), customFolder: true };
    }
  }
  const base = settings.folderMode === "custom" ? [settings.downloadsSubfolder || "KomiK", folder].filter(Boolean).join("/") : folder;
  let last: SavedFile | null = null;
  // Pages first, ComicInfo.xml last, a few at a time.
  for (let i = 0; i < files.length; i += 4) {
    const batch = files.slice(i, i + 4);
    const results = await Promise.all(batch.map((f) => viaDownloads(`${base ? `${base}/` : ""}${name}/${f.name}`, f.blob, false)));
    last = results[results.length - 1];
  }
  const path = last?.path ? last.path.replace(/[\\/][^\\/]+$/, "") : `${base}/${name}`;
  return { path, downloadId: last?.downloadId, customFolder: false };
}
