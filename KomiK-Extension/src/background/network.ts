// Remembers the large images each tab loaded, so readers that swap one <img> between pages (or draw
// pages into a canvas) still reveal their page URLs to the scanner.

interface SeenImage {
  url: string;
  size?: number;
  at: number;
}

const perTab = new Map<number, SeenImage[]>();
const MAX_PER_TAB = 800;
const MIN_BYTES = 45_000;

export function startNetworkObserver(): void {
  chrome.webRequest.onCompleted.addListener(
    (details) => {
      if (details.tabId < 0 || details.statusCode >= 400) return;
      const lengthHeader = details.responseHeaders?.find((h) => h.name.toLowerCase() === "content-length")?.value;
      const size = lengthHeader ? Number.parseInt(lengthHeader, 10) : undefined;
      if (size !== undefined && size < MIN_BYTES) return;
      if (/favicon|avatar|logo|sprite|emoji|icon/i.test(details.url)) return;
      const list = perTab.get(details.tabId) ?? [];
      if (list.some((i) => i.url === details.url)) return;
      list.push({ url: details.url, size, at: details.timeStamp });
      if (list.length > MAX_PER_TAB) list.splice(0, list.length - MAX_PER_TAB);
      perTab.set(details.tabId, list);
    },
    { urls: ["http://*/*", "https://*/*"], types: ["image"] },
    ["responseHeaders"]
  );

  chrome.tabs.onUpdated.addListener((tabId, info) => {
    if (info.url) perTab.delete(tabId);
  });
  chrome.tabs.onRemoved.addListener((tabId) => perTab.delete(tabId));
}

export function networkImagesFor(tabId: number): Array<{ url: string; size?: number }> {
  return (perTab.get(tabId) ?? []).map(({ url, size }) => ({ url, size }));
}
