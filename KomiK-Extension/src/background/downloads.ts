// Saves files through the browser's downloads API and routes the site's own comic downloads into the
// KomiK folder with a proper name.
import { planSave } from "@/shared/naming";
import { loadSettings } from "@/shared/settings";
import { COMIC_FILE_EXTENSIONS, emptyMeta, extOf, hostOf } from "@/shared/util";
import { applySeriesMemory } from "@/shared/enrich";
import { cachedScanForUrl } from "./scan";
import { queueJobs } from "./jobs";

/** Filenames this extension asked for, so onDeterminingFilename keeps them exactly. */
const ownFilenames = new Map<string, string>();

export async function startDownload(url: string, filename: string, saveAs: boolean): Promise<number> {
  ownFilenames.set(url, filename);
  try {
    return await chrome.downloads.download({ url, filename, saveAs, conflictAction: "uniquify" });
  } catch (err) {
    ownFilenames.delete(url);
    throw err;
  }
}

export function waitForDownload(downloadId: number): Promise<{ state: "complete" | "interrupted"; filename?: string; error?: string }> {
  return new Promise((resolve) => {
    const finish = (item?: chrome.downloads.DownloadItem) => {
      chrome.downloads.onChanged.removeListener(listener);
      resolve({ state: item?.state === "complete" ? "complete" : "interrupted", filename: item?.filename, error: item?.error });
    };
    const listener = (delta: chrome.downloads.DownloadDelta) => {
      if (delta.id !== downloadId || !delta.state) return;
      if (delta.state.current === "complete" || delta.state.current === "interrupted") {
        chrome.downloads.search({ id: downloadId }).then(([item]) => finish(item));
      }
    };
    chrome.downloads.onChanged.addListener(listener);
    chrome.downloads.search({ id: downloadId }).then(([item]) => {
      if (item && (item.state === "complete" || item.state === "interrupted")) finish(item);
    });
  });
}

export function startDownloadRouting(): void {
  chrome.downloads.onDeterminingFilename.addListener((item, suggest) => {
    // Our own downloads: keep the name we chose (Chrome otherwise names blob: downloads by UUID).
    if (item.byExtensionId === chrome.runtime.id) {
      const wanted = ownFilenames.get(item.url) ?? ownFilenames.get(item.finalUrl);
      if (wanted) {
        ownFilenames.delete(item.url);
        suggest({ filename: wanted, conflictAction: "uniquify" });
      } else {
        suggest();
      }
      return false;
    }

    const ext = extOf(item.filename) || extOf(item.finalUrl || item.url);
    if (!COMIC_FILE_EXTENSIONS.includes(ext)) {
      suggest();
      return false;
    }

    (async () => {
      const settings = await loadSettings();
      if (!settings.catchComicDownloads) return suggest();

      const scan = item.referrer ? cachedScanForUrl(item.referrer) : undefined;
      const base = scan ? await applySeriesMemory(scan.meta) : { ...emptyMeta(), site: hostOf(item.referrer || item.url) };
      const originalName = (item.filename.split(/[\\/]/).pop() ?? `comic.${ext}`).replace(/\.[^.]+$/, "");
      const meta = { ...base, title: scan?.meta.title || originalName, series: scan?.meta.series || "" };

      // Re-fetch through KomiK to embed metadata (only when asked: some links work only once).
      const canEmbed = settings.embedIntoCaughtDownloads && ["cbz", "zip", "pdf"].includes(ext) && /^https?:/.test(item.finalUrl || item.url);
      if (canEmbed) {
        suggest();
        await chrome.downloads.cancel(item.id).catch(() => undefined);
        await chrome.downloads.erase({ id: item.id }).catch(() => undefined);
        await queueJobs([{ meta, format: ext === "pdf" ? "pdf" : "cbz", pages: [], sourceUrl: item.referrer || item.url, file: { url: item.finalUrl || item.url, ext, name: `${originalName}.${ext}`, label: "Site download" } }]);
        return;
      }

      const plan = planSave(meta, ext === "pdf" ? "pdf" : "cbz", { ...settings, folderMode: "downloads" });
      const fileName = plan.fileName.replace(/\.(cbz|pdf)$/i, `.${ext}`);
      suggest({ filename: plan.folder ? `${plan.folder}/${fileName}` : fileName, conflictAction: "uniquify" });
    })().catch(() => suggest());
    return true;
  });
}
