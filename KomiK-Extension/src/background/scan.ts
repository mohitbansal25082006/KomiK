// Scans tabs with the on-demand content engine and caches results per tab.
import { sendToTab } from "@/shared/messages";
import { loadSettings } from "@/shared/settings";
import type { DetectResult, PageRef } from "@/shared/types";
import { networkImagesFor } from "./network";

const cache = new Map<number, DetectResult>();
const picks = new Map<number, { url: string; pages: PageRef[] }>();

export async function injectEngine(tabId: number): Promise<void> {
  try {
    const res = await sendToTab(tabId, "content-ping", {});
    if (res?.engine) return;
  } catch {
    /* not injected yet */
  }
  await chrome.scripting.executeScript({ target: { tabId }, files: ["content/engine.js"] });
}

export async function scanTab(tabId: number, deep = false, force = false): Promise<DetectResult> {
  const tab = await chrome.tabs.get(tabId);
  if (!tab.url || !/^https?:/.test(tab.url)) {
    throw new Error("KomiK can only scan web pages (http and https).");
  }
  const cached = cache.get(tabId);
  if (!force && !deep && cached && cached.url === tab.url && Date.now() - cached.scannedAt < 60_000) return withPicks(tabId, cached);

  const settings = await loadSettings();
  await injectEngine(tabId);
  const payload = { deep, rules: settings.siteRules, autoScroll: settings.autoScroll, minPageWidth: settings.minPageWidth, networkImages: networkImagesFor(tabId) };
  const result = (await sendToTab(tabId, "content-scan", payload)) as DetectResult | { error: string } | undefined;
  if (!result || "error" in result) throw new Error(result && "error" in result ? result.error : "The page did not answer. Reload it and try again.");
  cache.set(tabId, result);
  return withPicks(tabId, result);
}

function withPicks(tabId: number, result: DetectResult): DetectResult {
  const picked = picks.get(tabId);
  if (picked && picked.url === result.url && picked.pages.length) {
    return { ...result, pages: picked.pages, adapter: "Your picks", confidence: "high" };
  }
  return result;
}

export function rememberPicks(tabId: number, url: string, pages: PageRef[]): void {
  picks.set(tabId, { url, pages });
  cache.delete(tabId);
}

export function forgetTab(tabId: number): void {
  cache.delete(tabId);
  picks.delete(tabId);
}

// A closed tab's scan and picks are never needed again.
chrome.tabs.onRemoved.addListener((tabId) => forgetTab(tabId));

/** Opens a chapter in a hidden background tab, lets its reader script run, scans it and closes it. */
export async function scanUrlInBackgroundTab(url: string): Promise<DetectResult> {
  const tab = await chrome.tabs.create({ url, active: false });
  const tabId = tab.id!;
  try {
    await new Promise<void>((resolve, reject) => {
      const timeout = setTimeout(() => {
        chrome.tabs.onUpdated.removeListener(listener);
        reject(new Error("The chapter page took too long to load."));
      }, 45_000);
      const listener = (id: number, info: chrome.tabs.OnUpdatedInfo) => {
        if (id === tabId && info.status === "complete") {
          clearTimeout(timeout);
          chrome.tabs.onUpdated.removeListener(listener);
          resolve();
        }
      };
      chrome.tabs.onUpdated.addListener(listener);
    });
    await new Promise((r) => setTimeout(r, 1200));
    return await scanTab(tabId, true, true);
  } finally {
    forgetTab(tabId);
    chrome.tabs.remove(tabId).catch(() => undefined);
  }
}
