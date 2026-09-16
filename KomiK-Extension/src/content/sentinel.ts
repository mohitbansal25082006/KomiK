// Always-on, lightweight content script. It only counts likely comic pages so the toolbar badge and the
// floating crest button can appear; the full scanner is injected on demand.
import { envelope, isEnvelope } from "@/shared/messages";
import { normalizeSettings } from "@/shared/settings";
import type { Settings } from "@/shared/types";
import { extensionAlive, sendSafely } from "./alive";
import { CREST_SVG, ensureShadow, showToast } from "./ui";

declare global {
  interface Window {
    __komikSentinel?: boolean;
  }
}

(() => {
  if (window.__komikSentinel || window.top !== window) return;
  window.__komikSentinel = true;

  let settings: Settings = normalizeSettings(undefined);
  let lastKey = "";
  let fab: HTMLButtonElement | null = null;
  let dismissedFor = "";
  let stopped = false;

  const LAZY = "img[data-src], img[data-lazy-src], img[data-original], img[data-srcset]";

  function measure(): { count: number } {
    let count = 0;
    for (const img of Array.from(document.images)) {
      if (img.closest("header, footer, nav")) continue;
      const w = img.naturalWidth || img.width;
      const h = img.naturalHeight || img.height;
      if (w >= 380 && h >= 380) count++;
    }
    count += document.querySelectorAll(LAZY).length > 3 ? Math.floor(document.querySelectorAll(LAZY).length / 2) : 0;
    return { count };
  }

  function renderFab(count: number) {
    const show = settings.floatingButton && dismissedFor !== location.href && count >= 4;
    if (!show) {
      fab?.remove();
      fab = null;
      return;
    }
    const root = ensureShadow();
    if (!fab) {
      fab = document.createElement("button");
      fab.className = "fab enter";
      fab.title = "KomiK Downloader: download this comic";
      fab.setAttribute("aria-label", "Open KomiK Downloader");
      fab.innerHTML = `${CREST_SVG}<span class="count"></span><span class="close" title="Hide on this page">×</span>`;
      fab.addEventListener("click", (e) => {
        if ((e.target as HTMLElement).classList.contains("close")) {
          dismissedFor = location.href;
          renderFab(0);
          return;
        }
        if (!extensionAlive()) {
          // KomiK was updated while this tab stayed open: this old copy can't reach it any more.
          showToast("KomiK was updated", "Reload this page to use KomiK here again.", "cyan");
          stop();
          return;
        }
        void sendSafely<{ opened?: string }>(envelope("floating-click", { url: location.href }, "background")).then((res) => {
          if (!res || res.opened === "none") showToast("Open KomiK", "Click the KomiK icon in your toolbar (or press Alt+K) to download this comic.", "cyan");
        });
      });
      root.appendChild(fab);
    }
    const label = fab.querySelector(".count") as HTMLElement;
    label.textContent = String(Math.min(count, 999));
  }

  function update() {
    if (stopped) return;
    if (!extensionAlive()) return stop();
    const { count } = measure();
    renderFab(count);
    const key = `${location.href}|${count}`;
    if (key === lastKey) return;
    lastKey = key;
    void sendSafely(envelope("page-hint", { count, url: location.href, title: document.title }, "background"));
  }

  let timer: ReturnType<typeof setTimeout> | null = null;
  const schedule = () => {
    if (timer || stopped) return;
    timer = setTimeout(() => {
      timer = null;
      update();
    }, 1200);
  };

  const onSettingsChanged = (changes: Record<string, chrome.storage.StorageChange>, area: string) => {
    if (area === "local" && changes.settings) {
      settings = normalizeSettings(changes.settings.newValue as Partial<Settings>);
      update();
    }
  };

  const observer = new MutationObserver(schedule);

  /** Stops watching the page for good once the extension that started this script is gone. */
  function stop() {
    if (stopped) return;
    stopped = true;
    observer.disconnect();
    if (timer) clearTimeout(timer);
    timer = null;
    removeEventListener("load", schedule);
    removeEventListener("scroll", schedule);
    removeEventListener("popstate", schedule);
    fab?.remove();
    fab = null;
    try {
      chrome.storage.onChanged.removeListener(onSettingsChanged);
    } catch {
      /* already disconnected */
    }
  }

  try {
    chrome.storage.local
      .get("settings")
      .then((data) => {
        settings = normalizeSettings(data.settings as Partial<Settings>);
        update();
      })
      .catch(() => undefined);
    chrome.storage.onChanged.addListener(onSettingsChanged);
  } catch {
    return;
  }

  observer.observe(document.documentElement, { childList: true, subtree: true });
  addEventListener("load", schedule, { passive: true });
  addEventListener("scroll", schedule, { passive: true });
  addEventListener("popstate", schedule);

  chrome.runtime.onMessage.addListener((msg, _sender, sendResponse) => {
    if (!isEnvelope(msg) || msg.target !== "content") return false;
    if (msg.type === "content-toast") {
      const p = msg.payload as { title: string; message: string; tone?: "yellow" | "cyan" | "magenta" };
      showToast(p.title, p.message, p.tone);
      sendResponse({ ok: true });
      return false;
    }
    return false;
  });
})();
