// KomiK Downloader background service worker: routes messages, scans tabs, owns the downloads,
// notifications, context menus, keyboard shortcuts and toolbar badge.
import { getJobs } from "@/shared/db";
import { listen, sendToTab } from "@/shared/messages";
import { loadSettings, saveSettings } from "@/shared/settings";
import type { DetectResult, JobsSnapshot } from "@/shared/types";
import { COMIC_FILE_EXTENSIONS, emptyMeta, extOf, hostOf } from "@/shared/util";
import { startDownload, startDownloadRouting, waitForDownload } from "./downloads";
import { queueChapters, queueJobs, quickDownload } from "./jobs";
import { startNetworkObserver } from "./network";
import { ensureOffscreen, toEngine } from "./offscreen";
import { ensureReferer } from "./referer";
import { injectEngine, rememberPicks, scanTab, scanUrlInBackgroundTab } from "./scan";

startNetworkObserver();
startDownloadRouting();

const notificationDownloads = new Map<string, number>();

// ── Install / startup ────────────────────────────────────────────────────────────────
chrome.runtime.onInstalled.addListener(async (details) => {
  const settings = await loadSettings();
  await saveSettings(settings);
  createMenus();
  chrome.sidePanel?.setPanelBehavior?.({ openPanelOnActionClick: false }).catch(() => undefined);
  if (details.reason === "install" && !settings.onboarded) {
    chrome.tabs.create({ url: chrome.runtime.getURL("options.html#welcome") });
  }
});

chrome.runtime.onStartup.addListener(async () => {
  createMenus();
  const settings = await loadSettings();
  const jobs = await getJobs();
  if (settings.autoResume && jobs.some((j) => ["queued", "resolving", "downloading", "packing", "saving"].includes(j.status))) {
    await toEngine("engine-kick", {}).catch(() => undefined);
  }
});

// ── Context menus ────────────────────────────────────────────────────────────────────
function createMenus() {
  chrome.contextMenus.removeAll(() => {
    const contexts = ["page", "image", "link", "selection"] as [`${chrome.contextMenus.ContextType}`, ...`${chrome.contextMenus.ContextType}`[]];
    chrome.contextMenus.create({ id: "komik-root", title: "KomiK Downloader", contexts });
    chrome.contextMenus.create({ id: "komik-download-page", parentId: "komik-root", title: "Download the comic on this page", contexts });
    chrome.contextMenus.create({ id: "komik-download-link", parentId: "komik-root", title: "Download linked comic file", contexts: ["link"] as typeof contexts, targetUrlPatterns: COMIC_FILE_EXTENSIONS.flatMap((e) => [`*://*/*.${e}`, `*://*/*.${e}?*`]) });
    chrome.contextMenus.create({ id: "komik-pick", parentId: "komik-root", title: "Pick pages by clicking them…", contexts });
    chrome.contextMenus.create({ id: "komik-panel", parentId: "komik-root", title: "Open download manager", contexts });
  });
}

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  const tabId = tab?.id;
  if (info.menuItemId === "komik-panel" && tab?.windowId !== undefined) {
    await chrome.sidePanel.open({ windowId: tab.windowId }).catch(() => undefined);
    return;
  }
  if (tabId === undefined) return;
  if (info.menuItemId === "komik-pick") {
    await injectEngine(tabId);
    await sendToTab(tabId, "content-picker", {});
    return;
  }
  if (info.menuItemId === "komik-download-link" && info.linkUrl) {
    const ext = extOf(info.linkUrl);
    const settings = await loadSettings();
    const name = decodeURIComponent(info.linkUrl.split("/").pop()?.split("?")[0] ?? `comic.${ext}`);
    const meta = { ...emptyMeta(), title: name.replace(/\.[^.]+$/, ""), site: hostOf(tab?.url ?? info.linkUrl) };
    await queueJobs([{ meta, format: settings.defaultFormat, pages: [], sourceUrl: tab?.url ?? info.linkUrl, tabId, file: { url: info.linkUrl, ext, name, label: "Linked file" } }]);
    toast(tabId, "QUEUED!", `${name} is downloading with KomiK.`, "cyan");
    return;
  }
  if (info.menuItemId === "komik-download-page") await quick(tabId);
});

// ── Keyboard shortcuts ───────────────────────────────────────────────────────────────
chrome.commands.onCommand.addListener(async (command, tab) => {
  if (command === "open-side-panel" && tab?.windowId !== undefined) {
    await chrome.sidePanel.open({ windowId: tab.windowId }).catch(() => undefined);
  }
  if (command === "quick-download" && tab?.id !== undefined) await quick(tab.id);
});

async function quick(tabId: number) {
  try {
    const result = await scanTab(tabId);
    const { kind, ids } = await quickDownload(result, tabId);
    if (kind === "none") toast(tabId, "NOTHING TO GRAB!", "No comic pages or comic files were found here. Try Pick pages from the KomiK menu.", "magenta");
    else toast(tabId, "QUEUED!", `${result.meta.title || "This comic"} · ${kind === "file" ? "site file" : `${result.pages.length} pages`}${ids.length > 1 ? ` · ${ids.length} jobs` : ""}`, "yellow");
  } catch (err) {
    toast(tabId, "OOPS!", err instanceof Error ? err.message : String(err), "magenta");
  }
}

function toast(tabId: number, title: string, message: string, tone: "yellow" | "cyan" | "magenta") {
  sendToTab(tabId, "content-toast", { title, message, tone }).catch(() => undefined);
}

// ── Badge ────────────────────────────────────────────────────────────────────────────
chrome.action.setBadgeBackgroundColor({ color: "#FFD700" }).catch(() => undefined);
chrome.action.setBadgeTextColor?.({ color: "#08080A" })?.catch?.(() => undefined);

async function setBadge(tabId: number, count: number, files: number) {
  const settings = await loadSettings();
  const text = !settings.showBadge ? "" : count >= 4 ? (count > 999 ? "999+" : String(count)) : files > 0 ? "CBZ" : "";
  await chrome.action.setBadgeText({ tabId, text }).catch(() => undefined);
}

// ── Notifications ────────────────────────────────────────────────────────────────────
chrome.notifications.onClicked.addListener((id) => {
  const downloadId = notificationDownloads.get(id);
  if (downloadId !== undefined) chrome.downloads.show(downloadId);
  chrome.notifications.clear(id);
});
chrome.notifications.onButtonClicked.addListener((id, button) => {
  const downloadId = notificationDownloads.get(id);
  if (downloadId === undefined) return;
  if (button === 0) chrome.downloads.open(downloadId);
  else chrome.downloads.show(downloadId);
  chrome.notifications.clear(id);
});

// ── Messages ─────────────────────────────────────────────────────────────────────────
listen("background", {
  "scan-tab": async ({ tabId, deep, force }) => {
    try {
      return await scanTab(tabId, !!deep, !!force);
    } catch (err) {
      return { error: err instanceof Error ? err.message : String(err) };
    }
  },
  "queue-jobs": async ({ jobs }) => ({ ids: await queueJobs(jobs) }),
  "queue-chapters": async ({ base, meta, chapters, format, tabId }) => ({ ids: await queueChapters(base, meta, chapters, format, tabId) }),
  "job-action": async ({ id, action }) => toEngine("engine-action", { id, action }),
  "jobs-snapshot": async (): Promise<JobsSnapshot> => {
    try {
      const contexts = await chrome.runtime.getContexts({ contextTypes: [chrome.runtime.ContextType.OFFSCREEN_DOCUMENT] });
      if (contexts.length) return await toEngine("engine-snapshot", {});
    } catch {
      /* fall back to the stored jobs */
    }
    const jobs = await getJobs();
    return { jobs, active: 0, queued: jobs.filter((j) => j.status === "queued").length, speedBps: 0 };
  },
  "clear-finished": async () => toEngine("engine-clear-finished", {}),
  "start-picker": async ({ tabId }) => {
    try {
      await injectEngine(tabId);
      await sendToTab(tabId, "content-picker", {});
      return { ok: true };
    } catch (err) {
      return { ok: false, error: err instanceof Error ? err.message : String(err) };
    }
  },
  "open-side-panel": async ({ tabId, windowId }) => {
    try {
      if (windowId !== undefined) await chrome.sidePanel.open({ windowId });
      else if (tabId !== undefined) await chrome.sidePanel.open({ tabId });
      return { ok: true };
    } catch {
      return { ok: false };
    }
  },
  "open-download": async ({ downloadId }) => {
    try {
      await chrome.downloads.open(downloadId);
      return { ok: true };
    } catch (err) {
      chrome.downloads.show(downloadId);
      return { ok: false, error: String(err) };
    }
  },
  "show-download": async ({ downloadId }) => {
    if (downloadId !== undefined) chrome.downloads.show(downloadId);
    else chrome.downloads.showDefaultFolder();
    return { ok: true };
  },
  "page-hint": async ({ count, files }, sender) => {
    if (sender.tab?.id !== undefined) await setBadge(sender.tab.id, count, files);
    return { ok: true };
  },
  "picker-finished": async ({ pages, url }, sender) => {
    if (sender.tab?.id !== undefined) rememberPicks(sender.tab.id, url, pages);
    await chrome.action.openPopup?.().catch(() => undefined);
    return { ok: true };
  },
  "floating-click": async (_payload, sender) => {
    const tab = sender.tab;
    if (!tab?.id) return { ok: false, opened: "none" as const };
    try {
      await chrome.sidePanel.open({ tabId: tab.id });
      return { ok: true, opened: "panel" as const };
    } catch {
      try {
        await chrome.action.openPopup({ windowId: tab.windowId });
        return { ok: true, opened: "popup" as const };
      } catch {
        return { ok: false, opened: "none" as const };
      }
    }
  },

  // Offscreen engine requests
  "off-settings": () => loadSettings(),
  "off-ensure-referer": async ({ url, referer }) => {
    await ensureReferer(url, referer).catch(() => undefined);
    return { ok: true };
  },
  "off-download": async ({ url, filename, saveAs }) => {
    try {
      return { downloadId: await startDownload(url, filename, saveAs) };
    } catch (err) {
      return { error: err instanceof Error ? err.message : String(err) };
    }
  },
  "off-wait-download": ({ downloadId }) => waitForDownload(downloadId),
  "off-page-fetch": async ({ tabId, url }) => {
    try {
      await injectEngine(tabId);
      return await sendToTab(tabId, "content-fetch", { url });
    } catch (err) {
      return { error: err instanceof Error ? err.message : String(err) };
    }
  },
  "off-resolve-chapter": async ({ url }): Promise<DetectResult | { error: string }> => {
    try {
      return await scanUrlInBackgroundTab(url);
    } catch (err) {
      return { error: err instanceof Error ? err.message : String(err) };
    }
  },
  "off-notify": async ({ title, message, jobId, iconUrl }) => {
    const settings = await loadSettings();
    if (!settings.notifyOnFinish) return { ok: true };
    const jobs = jobId ? await getJobs() : [];
    const downloadId = jobs.find((j) => j.id === jobId)?.downloadId;
    const id = await chrome.notifications.create({
      type: "basic",
      iconUrl: iconUrl || "icons/icon-128.png",
      title,
      message,
      priority: 0,
      buttons: downloadId !== undefined ? [{ title: "Open in Komik" }, { title: "Show in folder" }] : undefined
    });
    if (downloadId !== undefined) notificationDownloads.set(id, downloadId);
    return { ok: true };
  }
});

// Keep the engine alive across service-worker restarts while work is pending.
chrome.alarms.create("komik-heartbeat", { periodInMinutes: 1 });
chrome.alarms.onAlarm.addListener(async (alarm) => {
  if (alarm.name !== "komik-heartbeat") return;
  const jobs = await getJobs().catch(() => []);
  if (jobs.some((j) => ["queued", "resolving", "downloading", "packing", "saving"].includes(j.status))) {
    await ensureOffscreen().catch(() => undefined);
  }
});
