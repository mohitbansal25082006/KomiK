// Typed runtime messages between the UI pages, background worker, offscreen engine and content scripts.
import type { ChapterRef, ComicMeta, DetectResult, FileLink, JobsSnapshot, OutputFormat, PageRef, Settings } from "./types";

export interface JobRequest {
  meta: ComicMeta;
  format: OutputFormat;
  pages: PageRef[];
  coverIndex?: number;
  sourceUrl: string;
  tabId?: number;
  chapterUrl?: string;
  file?: FileLink;
  batchId?: string;
  batchLabel?: string;
}

export type JobAction = "pause" | "resume" | "cancel" | "retry" | "remove";

export interface MessageMap {
  // UI → background
  "scan-tab": { req: { tabId: number; deep?: boolean; force?: boolean }; res: DetectResult | { error: string } };
  "queue-jobs": { req: { jobs: JobRequest[] }; res: { ids: string[] } };
  "queue-chapters": { req: { base: DetectResult; meta: ComicMeta; chapters: ChapterRef[]; format: OutputFormat; tabId?: number }; res: { ids: string[] } };
  "job-action": { req: { id: string; action: JobAction }; res: { ok: boolean } };
  "jobs-snapshot": { req: Record<string, never>; res: JobsSnapshot };
  "clear-finished": { req: Record<string, never>; res: { ok: boolean } };
  "start-picker": { req: { tabId: number }; res: { ok: boolean; error?: string } };
  "open-download": { req: { downloadId: number }; res: { ok: boolean; error?: string } };
  "show-download": { req: { downloadId?: number; relativePath?: string }; res: { ok: boolean; error?: string } };
  "open-side-panel": { req: { tabId?: number; windowId?: number }; res: { ok: boolean } };
  "enrich-meta": { req: { meta: ComicMeta }; res: ComicMeta };
  "remember-series": { req: { meta: ComicMeta }; res: { ok: boolean } };
  "folder-status": { req: Record<string, never>; res: { mode: Settings["folderMode"]; name: string; granted: boolean } };

  // content → background
  "page-hint": { req: { count: number; files: number; url: string; title: string }; res: { ok: boolean } };
  "picker-finished": { req: { pages: PageRef[]; url: string }; res: { ok: boolean } };
  "floating-click": { req: { url: string }; res: { ok: boolean; opened: "panel" | "popup" | "none" } };
  /** Loads a preview the browser refuses to show on an extension page (hotlink or cross-origin rules). */
  "fetch-image": { req: { url: string; referer?: string }; res: { base64?: string; mime?: string; error?: string } };

  // offscreen → background
  "off-ensure-referer": { req: { url: string; referer: string }; res: { ok: boolean } };
  "off-download": { req: { url: string; filename: string; saveAs: boolean }; res: { downloadId?: number; error?: string } };
  "off-page-fetch": { req: { tabId: number; url: string }; res: { base64?: string; mime?: string; error?: string } };
  "off-resolve-chapter": { req: { url: string }; res: DetectResult | { error: string } };
  "off-notify": { req: { title: string; message: string; jobId?: string; iconUrl?: string }; res: { ok: boolean } };
  "off-settings": { req: Record<string, never>; res: Settings };
  "off-wait-download": { req: { downloadId: number }; res: { state: "complete" | "interrupted"; filename?: string; error?: string } };

  // background → offscreen
  "engine-kick": { req: Record<string, never>; res: { ok: boolean } };
  "engine-action": { req: { id: string; action: JobAction }; res: { ok: boolean } };
  "engine-snapshot": { req: Record<string, never>; res: JobsSnapshot };
  "engine-clear-finished": { req: Record<string, never>; res: { ok: boolean } };

  // background → content
  "content-scan": { req: { deep: boolean; rules: Settings["siteRules"]; autoScroll: boolean; minPageWidth: number }; res: DetectResult };
  "content-fetch": { req: { url: string }; res: { base64?: string; mime?: string; error?: string } };
  "content-picker": { req: Record<string, never>; res: { ok: boolean } };
  "content-toast": { req: { title: string; message: string; tone?: "yellow" | "cyan" | "magenta" }; res: { ok: boolean } };
  "content-ping": { req: Record<string, never>; res: { engine: boolean } };
}

export type MessageType = keyof MessageMap;

export interface Envelope<T extends MessageType = MessageType> {
  komik: true;
  type: T;
  /** "offscreen" / "background" / "content": only that context answers. */
  target?: "background" | "offscreen" | "content";
  payload: MessageMap[T]["req"];
}

export function envelope<T extends MessageType>(type: T, payload: MessageMap[T]["req"], target?: Envelope["target"]): Envelope<T> {
  return { komik: true, type, payload, target };
}

export async function send<T extends MessageType>(type: T, payload: MessageMap[T]["req"], target: Envelope["target"] = "background"): Promise<MessageMap[T]["res"]> {
  return chrome.runtime.sendMessage(envelope(type, payload, target)) as Promise<MessageMap[T]["res"]>;
}

export async function sendToTab<T extends MessageType>(tabId: number, type: T, payload: MessageMap[T]["req"]): Promise<MessageMap[T]["res"]> {
  return chrome.tabs.sendMessage(tabId, envelope(type, payload, "content")) as Promise<MessageMap[T]["res"]>;
}

export function isEnvelope(msg: unknown): msg is Envelope {
  return !!msg && typeof msg === "object" && (msg as Envelope).komik === true && typeof (msg as Envelope).type === "string";
}

export type Handler<T extends MessageType> = (payload: MessageMap[T]["req"], sender: chrome.runtime.MessageSender) => Promise<MessageMap[T]["res"]> | MessageMap[T]["res"];

/** Registers handlers for one context; messages for other targets are left for their own listener. */
export function listen(target: Envelope["target"], handlers: { [K in MessageType]?: Handler<K> }): void {
  chrome.runtime.onMessage.addListener((msg, sender, sendResponse) => {
    if (!isEnvelope(msg)) return false;
    if (msg.target && msg.target !== target) return false;
    const handler = handlers[msg.type] as Handler<MessageType> | undefined;
    if (!handler) return false;
    Promise.resolve()
      .then(() => handler(msg.payload as never, sender))
      .then((res) => sendResponse(res))
      .catch((err: unknown) => sendResponse({ error: err instanceof Error ? err.message : String(err) }));
    return true;
  });
}

/** Broadcasts (no response expected) that jobs or history changed. */
export const BROADCAST = {
  jobs: "komik:jobs-changed",
  history: "komik:history-changed",
  detect: "komik:detect-changed"
} as const;

export function broadcast(kind: keyof typeof BROADCAST, data?: unknown): void {
  chrome.runtime.sendMessage({ komikBroadcast: BROADCAST[kind], data }).catch(() => undefined);
}

export function onBroadcast(kind: keyof typeof BROADCAST, listener: (data: unknown) => void): () => void {
  const handler = (msg: unknown) => {
    if (msg && typeof msg === "object" && (msg as { komikBroadcast?: string }).komikBroadcast === BROADCAST[kind]) {
      listener((msg as { data?: unknown }).data);
    }
    return false;
  };
  chrome.runtime.onMessage.addListener(handler);
  return () => chrome.runtime.onMessage.removeListener(handler);
}
