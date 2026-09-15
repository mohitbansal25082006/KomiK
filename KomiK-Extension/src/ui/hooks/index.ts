import { useCallback, useEffect, useRef, useState } from "react";
import { getHistory } from "@/shared/db";
import { onBroadcast, send } from "@/shared/messages";
import { DEFAULT_SETTINGS, loadSettings, onSettingsChanged, saveSettings } from "@/shared/settings";
import type { HistoryEntry, JobsSnapshot, Settings } from "@/shared/types";

export function useSettings(): [Settings, (patch: Partial<Settings>) => Promise<void>, boolean] {
  const [settings, setSettings] = useState<Settings>(DEFAULT_SETTINGS);
  const [ready, setReady] = useState(false);
  useEffect(() => {
    loadSettings().then((s) => {
      setSettings(s);
      setReady(true);
    });
    return onSettingsChanged(setSettings);
  }, []);
  const update = useCallback(async (patch: Partial<Settings>) => {
    setSettings((s) => ({ ...s, ...patch }));
    await saveSettings(patch);
  }, []);
  return [settings, update, ready];
}

/** Applies light / dark / system theme and reduced motion to <html>. */
export function useTheme(settings: Settings, ready = true): void {
  useEffect(() => {
    if (!ready) return;
    const media = matchMedia("(prefers-color-scheme: dark)");
    const apply = () => {
      const dark = settings.theme === "dark" || (settings.theme === "system" && media.matches);
      document.documentElement.classList.toggle("dark", dark);
      document.documentElement.classList.toggle("reduce-motion", settings.reducedMotion);
    };
    apply();
    media.addEventListener("change", apply);
    return () => media.removeEventListener("change", apply);
  }, [settings.theme, settings.reducedMotion, ready]);
}

const EMPTY_SNAPSHOT: JobsSnapshot = { jobs: [], active: 0, queued: 0, speedBps: 0 };

export function useJobs(): JobsSnapshot {
  const [snapshot, setSnapshot] = useState<JobsSnapshot>(EMPTY_SNAPSHOT);
  useEffect(() => {
    let alive = true;
    const refresh = () => send("jobs-snapshot", {}).then((s) => alive && s && "jobs" in s && setSnapshot(s)).catch(() => undefined);
    refresh();
    const off = onBroadcast("jobs", (data) => {
      if (data && typeof data === "object" && "jobs" in (data as JobsSnapshot)) setSnapshot(data as JobsSnapshot);
      else refresh();
    });
    const timer = setInterval(refresh, 4000);
    return () => {
      alive = false;
      off();
      clearInterval(timer);
    };
  }, []);
  return snapshot;
}

export function useHistory(): [HistoryEntry[], () => void] {
  const [items, setItems] = useState<HistoryEntry[]>([]);
  const refresh = useCallback(() => {
    getHistory().then(setItems).catch(() => undefined);
  }, []);
  useEffect(() => {
    refresh();
    return onBroadcast("history", refresh);
  }, [refresh]);
  return [items, refresh];
}

export function useActiveTab(): chrome.tabs.Tab | null {
  const [tab, setTab] = useState<chrome.tabs.Tab | null>(null);
  useEffect(() => {
    // "?tabId=123" pins the view to one tab (used when the page is opened as a normal tab, e.g. in tests).
    const pinned = Number(new URLSearchParams(location.search).get("tabId"));
    const load = () =>
      pinned
        ? chrome.tabs.get(pinned).then(setTab, () => setTab(null))
        : chrome.tabs.query({ active: true, lastFocusedWindow: true }).then(([t]) => setTab(t ?? null));
    load();
    const onActivated = () => load();
    const onUpdated = (id: number, info: chrome.tabs.OnUpdatedInfo, t: chrome.tabs.Tab) => {
      if ((pinned ? id === pinned : t.active) && (info.status === "complete" || info.url)) setTab(t);
    };
    if (pinned) {
      chrome.tabs.onUpdated.addListener(onUpdated);
      return () => chrome.tabs.onUpdated.removeListener(onUpdated);
    }
    chrome.tabs.onActivated.addListener(onActivated);
    chrome.tabs.onUpdated.addListener(onUpdated);
    return () => {
      chrome.tabs.onActivated.removeListener(onActivated);
      chrome.tabs.onUpdated.removeListener(onUpdated);
    };
  }, []);
  return tab;
}

/** Remembers the previous value (for spotting jobs that just finished). */
export function usePrevious<T>(value: T): T | undefined {
  const ref = useRef<T>(undefined);
  useEffect(() => {
    ref.current = value;
  }, [value]);
  return ref.current;
}
