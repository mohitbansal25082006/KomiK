// One IndexedDB database shared by every extension context (service worker, offscreen engine, UI pages).
import type { ComicMeta, HistoryEntry, Job } from "./types";

const DB_NAME = "komik-downloader";
const DB_VERSION = 1;

export interface StoredPage {
  key: string;
  jobId: string;
  index: number;
  data: ArrayBuffer;
  mime: string;
  ext: string;
  width?: number;
  height?: number;
}

export interface SeriesMemory {
  key: string;
  meta: Partial<ComicMeta>;
  updatedAt: number;
}

let dbPromise: Promise<IDBDatabase> | null = null;

function open(): Promise<IDBDatabase> {
  if (dbPromise) return dbPromise;
  dbPromise = new Promise((resolve, reject) => {
    const req = indexedDB.open(DB_NAME, DB_VERSION);
    req.onupgradeneeded = () => {
      const db = req.result;
      if (!db.objectStoreNames.contains("jobs")) db.createObjectStore("jobs", { keyPath: "id" });
      if (!db.objectStoreNames.contains("pages")) {
        const pages = db.createObjectStore("pages", { keyPath: "key" });
        pages.createIndex("jobId", "jobId", { unique: false });
      }
      if (!db.objectStoreNames.contains("history")) {
        const history = db.createObjectStore("history", { keyPath: "id" });
        history.createIndex("finishedAt", "finishedAt", { unique: false });
      }
      if (!db.objectStoreNames.contains("kv")) db.createObjectStore("kv");
      if (!db.objectStoreNames.contains("series")) db.createObjectStore("series", { keyPath: "key" });
    };
    req.onsuccess = () => {
      const db = req.result;
      db.onversionchange = () => {
        db.close();
        dbPromise = null;
      };
      resolve(db);
    };
    req.onerror = () => {
      dbPromise = null;
      reject(req.error);
    };
  });
  return dbPromise;
}

function wrap<T>(req: IDBRequest<T>): Promise<T> {
  return new Promise((resolve, reject) => {
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

async function store(name: string, mode: IDBTransactionMode = "readonly"): Promise<IDBObjectStore> {
  const db = await open();
  return db.transaction(name, mode).objectStore(name);
}

// ── Jobs ──────────────────────────────────────────────────────────────────────────────
export async function putJob(job: Job): Promise<void> {
  await wrap((await store("jobs", "readwrite")).put(job));
}

export async function getJob(id: string): Promise<Job | undefined> {
  return wrap((await store("jobs")).get(id)) as Promise<Job | undefined>;
}

export async function getJobs(): Promise<Job[]> {
  const jobs = (await wrap((await store("jobs")).getAll())) as Job[];
  return jobs.sort((a, b) => a.createdAt - b.createdAt);
}

export async function deleteJob(id: string): Promise<void> {
  await wrap((await store("jobs", "readwrite")).delete(id));
  await deletePages(id);
}

// ── Page cache (so interrupted downloads resume instead of starting over) ────────────
export async function putPage(page: StoredPage): Promise<void> {
  await wrap((await store("pages", "readwrite")).put(page));
}

export async function getPage(jobId: string, index: number): Promise<StoredPage | undefined> {
  return wrap((await store("pages")).get(`${jobId}:${index}`)) as Promise<StoredPage | undefined>;
}

export async function deletePages(jobId: string): Promise<void> {
  const db = await open();
  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction("pages", "readwrite");
    const idx = tx.objectStore("pages").index("jobId");
    const req = idx.openKeyCursor(IDBKeyRange.only(jobId));
    req.onsuccess = () => {
      const cursor = req.result;
      if (!cursor) return;
      tx.objectStore("pages").delete(cursor.primaryKey);
      cursor.continue();
    };
    tx.oncomplete = () => resolve();
    tx.onerror = () => reject(tx.error);
  });
}

export async function clearAllPages(): Promise<void> {
  await wrap((await store("pages", "readwrite")).clear());
}

export async function estimateCacheBytes(): Promise<number> {
  try {
    const estimate = await navigator.storage.estimate();
    return estimate.usage ?? 0;
  } catch {
    return 0;
  }
}

// ── History ───────────────────────────────────────────────────────────────────────────
export async function putHistory(entry: HistoryEntry): Promise<void> {
  await wrap((await store("history", "readwrite")).put(entry));
}

export async function getHistory(): Promise<HistoryEntry[]> {
  const all = (await wrap((await store("history")).getAll())) as HistoryEntry[];
  return all.sort((a, b) => b.finishedAt - a.finishedAt);
}

export async function deleteHistory(id: string): Promise<void> {
  await wrap((await store("history", "readwrite")).delete(id));
}

export async function clearHistory(): Promise<void> {
  await wrap((await store("history", "readwrite")).clear());
}

// ── Key/value (custom folder handle, misc) ───────────────────────────────────────────
export async function kvGet<T>(key: string): Promise<T | undefined> {
  return wrap((await store("kv")).get(key)) as Promise<T | undefined>;
}

export async function kvSet(key: string, value: unknown): Promise<void> {
  await wrap((await store("kv", "readwrite")).put(value, key));
}

export async function kvDelete(key: string): Promise<void> {
  await wrap((await store("kv", "readwrite")).delete(key));
}

// ── Remembered series edits (next chapter of the same series fills in automatically) ─
export function seriesKey(series: string, site: string): string {
  return `${site.toLowerCase()}|${series.toLowerCase().replace(/[^\p{L}\p{N}]+/gu, "")}`;
}

export async function getSeriesMemory(key: string): Promise<SeriesMemory | undefined> {
  return wrap((await store("series")).get(key)) as Promise<SeriesMemory | undefined>;
}

export async function putSeriesMemory(memory: SeriesMemory): Promise<void> {
  await wrap((await store("series", "readwrite")).put(memory));
}
