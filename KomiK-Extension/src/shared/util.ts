export const IMAGE_EXTENSIONS = ["jpg", "jpeg", "png", "webp", "gif", "avif", "bmp", "jfif", "tif", "tiff", "jxl", "heic"];
/** Formats the Komik reader opens directly (everything else gets converted). */
export const KOMIK_IMAGE_EXTENSIONS = ["jpg", "jpeg", "png", "webp", "gif", "bmp", "jfif", "tif", "tiff"];

export function uid(prefix = ""): string {
  const rand = crypto.getRandomValues(new Uint8Array(8));
  return prefix + Date.now().toString(36) + Array.from(rand, (b) => b.toString(16).padStart(2, "0")).join("");
}

export function formatBytes(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes <= 0) return "0 B";
  const units = ["B", "KB", "MB", "GB", "TB"];
  const i = Math.min(units.length - 1, Math.floor(Math.log(bytes) / Math.log(1024)));
  const v = bytes / 1024 ** i;
  return `${v >= 100 || i === 0 ? Math.round(v) : v.toFixed(1)} ${units[i]}`;
}

export function formatSpeed(bps: number): string {
  return bps > 0 ? `${formatBytes(bps)}/s` : "—";
}

export function formatEta(seconds: number): string {
  if (!Number.isFinite(seconds) || seconds <= 0) return "";
  if (seconds < 60) return `${Math.ceil(seconds)}s left`;
  const m = Math.floor(seconds / 60);
  if (m < 60) return `${m}m ${Math.round(seconds % 60)}s left`;
  return `${Math.floor(m / 60)}h ${m % 60}m left`;
}

export function timeAgo(ts: number, now = Date.now()): string {
  const s = Math.max(0, Math.round((now - ts) / 1000));
  if (s < 45) return "just now";
  const m = Math.round(s / 60);
  if (m < 60) return `${m} min ago`;
  const h = Math.round(m / 60);
  if (h < 24) return `${h} h ago`;
  const d = Math.round(h / 24);
  if (d < 30) return `${d} day${d === 1 ? "" : "s"} ago`;
  return new Date(ts).toLocaleDateString();
}

export function extOf(url: string): string {
  try {
    const path = new URL(url, "https://x.invalid/").pathname;
    const m = /\.([a-z0-9]{1,5})$/i.exec(decodeURIComponent(path));
    return m ? m[1].toLowerCase() : "";
  } catch {
    const m = /\.([a-z0-9]{1,5})(?:[?#]|$)/i.exec(url);
    return m ? m[1].toLowerCase() : "";
  }
}

export function hostOf(url: string): string {
  try {
    return new URL(url).hostname.replace(/^www\./, "");
  } catch {
    return "";
  }
}

export function absoluteUrl(value: string | null | undefined, base: string): string | null {
  if (!value) return null;
  const v = value.trim();
  if (!v || v.startsWith("javascript:") || v.startsWith("#")) return null;
  try {
    const u = new URL(v, base);
    if (u.protocol !== "http:" && u.protocol !== "https:" && u.protocol !== "data:" && u.protocol !== "blob:") return null;
    return u.href;
  } catch {
    return null;
  }
}

/** Collapses whitespace and strips invisible characters. */
export function cleanText(value: string | null | undefined): string {
  return (value ?? "").replace(/[​-‍﻿]/g, "").replace(/\s+/g, " ").trim();
}

export function uniqueBy<T>(items: T[], key: (item: T) => string): T[] {
  const seen = new Set<string>();
  const out: T[] = [];
  for (const item of items) {
    const k = key(item);
    if (seen.has(k)) continue;
    seen.add(k);
    out.push(item);
  }
  return out;
}

export function sleep(ms: number, signal?: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    const t = setTimeout(resolve, ms);
    signal?.addEventListener("abort", () => {
      clearTimeout(t);
      reject(new DOMException("Aborted", "AbortError"));
    }, { once: true });
  });
}

export function throttle<T extends (...args: never[]) => void>(fn: T, ms: number): T {
  let last = 0;
  let timer: ReturnType<typeof setTimeout> | null = null;
  return ((...args: Parameters<T>) => {
    const now = Date.now();
    const run = () => {
      last = Date.now();
      timer = null;
      fn(...args);
    };
    if (now - last >= ms) run();
    else if (!timer) timer = setTimeout(run, ms - (now - last));
  }) as T;
}

export function emptyMeta(): import("./types").ComicMeta {
  return {
    title: "", series: "", number: "", volume: "", chapterTitle: "", summary: "", year: "", month: "", day: "",
    writers: [], artists: [], publisher: "", genres: [], tags: [], language: "", manga: "", ageRating: "",
    status: "", web: "", site: "", coverUrl: "", numberKind: "none"
  };
}

export function arrayBufferToBase64(buffer: ArrayBuffer): string {
  const bytes = new Uint8Array(buffer);
  let binary = "";
  const chunk = 0x8000;
  for (let i = 0; i < bytes.length; i += chunk) {
    binary += String.fromCharCode(...bytes.subarray(i, i + chunk));
  }
  return btoa(binary);
}

export function base64ToUint8(base64: string): Uint8Array {
  const binary = atob(base64);
  const out = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) out[i] = binary.charCodeAt(i);
  return out;
}
