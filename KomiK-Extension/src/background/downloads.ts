// Saves finished comics through the browser's downloads API.

export async function startDownload(url: string, filename: string, saveAs: boolean): Promise<number> {
  return chrome.downloads.download({ url, filename, saveAs, conflictAction: "uniquify" });
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

