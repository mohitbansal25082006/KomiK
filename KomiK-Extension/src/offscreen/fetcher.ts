// Downloads one page image: referer rules, retries with backoff, rate-limit handling, in-page fallback
// for hotlink-protected hosts, type sniffing and conversion of formats Komik can't open.
import { looksLikeText, sniffImage } from "@/shared/imageinfo";
import { send } from "@/shared/messages";
import type { Settings } from "@/shared/types";
import { base64ToUint8, hostOf, KOMIK_IMAGE_EXTENSIONS, sleep } from "@/shared/util";
import type { ConnectionLimiter, SpeedMeter } from "./limiter";

export interface FetchedImage {
  data: ArrayBuffer;
  mime: string;
  ext: string;
  width?: number;
  height?: number;
  converted: boolean;
}

export interface FetchContext {
  settings: Settings;
  limiter: ConnectionLimiter;
  meter: SpeedMeter;
  tabId?: number;
  signal: AbortSignal;
  onBytes?: (bytes: number) => void;
}

const refererDone = new Set<string>();

async function ensureReferer(url: string, referer?: string) {
  if (!referer) return;
  const key = `${hostOf(url)}|${referer}`;
  if (refererDone.has(key)) return;
  await send("off-ensure-referer", { url, referer });
  refererDone.add(key);
}

class HttpError extends Error {
  constructor(public status: number, public retryAfter: number) {
    super(`HTTP ${status}`);
  }
}

async function fetchBytes(url: string, ctx: FetchContext): Promise<{ bytes: Uint8Array; mime: string }> {
  if (url.startsWith("data:")) {
    const res = await fetch(url);
    return { bytes: new Uint8Array(await res.arrayBuffer()), mime: res.headers.get("content-type") ?? "" };
  }
  const timeout = AbortSignal.timeout(ctx.settings.timeoutSeconds * 1000);
  const signal = AbortSignal.any([ctx.signal, timeout]);
  const res = await fetch(url, { credentials: "include", signal, cache: "default", redirect: "follow" });
  if (!res.ok) {
    const retryAfterHeader = res.headers.get("retry-after");
    const retryAfter = retryAfterHeader ? (Number.isFinite(+retryAfterHeader) ? +retryAfterHeader * 1000 : Math.max(0, Date.parse(retryAfterHeader) - Date.now())) : 0;
    throw new HttpError(res.status, retryAfter);
  }
  const mime = res.headers.get("content-type") ?? "";
  if (!res.body) return { bytes: new Uint8Array(await res.arrayBuffer()), mime };

  // Stream so the speed meter updates while large pages download.
  const reader = res.body.getReader();
  const chunks: Uint8Array[] = [];
  let total = 0;
  for (;;) {
    const { done, value } = await reader.read();
    if (done) break;
    chunks.push(value);
    total += value.length;
    ctx.meter.add(value.length);
    ctx.onBytes?.(value.length);
  }
  const bytes = new Uint8Array(total);
  let offset = 0;
  for (const c of chunks) {
    bytes.set(c, offset);
    offset += c.length;
  }
  return { bytes, mime };
}

export async function convertImage(data: Uint8Array | ArrayBuffer, targetMime: "image/jpeg" | "image/png", quality: number): Promise<{ data: ArrayBuffer; width: number; height: number }> {
  const blob = new Blob([data instanceof Uint8Array ? data.slice().buffer : data]);
  const bitmap = await createImageBitmap(blob);
  try {
    const canvas = new OffscreenCanvas(bitmap.width, bitmap.height);
    const g = canvas.getContext("2d")!;
    if (targetMime === "image/jpeg") {
      g.fillStyle = "#fff";
      g.fillRect(0, 0, bitmap.width, bitmap.height);
    }
    g.drawImage(bitmap, 0, 0);
    const out = await canvas.convertToBlob({ type: targetMime, quality: quality / 100 });
    return { data: await out.arrayBuffer(), width: bitmap.width, height: bitmap.height };
  } finally {
    bitmap.close();
  }
}

export async function fetchPage(url: string, referer: string | undefined, ctx: FetchContext): Promise<FetchedImage> {
  const host = hostOf(url) || "data";
  await ensureReferer(url, referer);
  let lastError: unknown = null;
  let triedInPage = false;

  for (let attempt = 0; attempt <= ctx.settings.retries; attempt++) {
    if (ctx.signal.aborted) throw new DOMException("Aborted", "AbortError");
    const release = await ctx.limiter.acquire(host, ctx.signal);
    let result: { bytes: Uint8Array; mime: string } | null = null;
    try {
      result = await fetchBytes(url, ctx);
      ctx.limiter.recover(host);
    } catch (err) {
      lastError = err;
      if (err instanceof HttpError && (err.status === 429 || err.status === 503)) {
        ctx.limiter.throttle(host, err.retryAfter || 2000 * (attempt + 1));
      }
    } finally {
      release();
    }

    if (result && (looksLikeText(result.bytes) || !sniffImage(result.bytes))) {
      lastError = new Error("The server sent a web page instead of the image (hotlink protection).");
      result = null;
    }

    // Hotlink-protected or cookie-bound images: let the reader tab fetch them as the page would.
    const blocked = !result && (lastError instanceof HttpError ? [401, 403, 404, 410].includes(lastError.status) : true);
    if (!result && blocked && ctx.tabId !== undefined && !triedInPage && !(lastError instanceof DOMException && lastError.name === "AbortError")) {
      triedInPage = true;
      const res = await send("off-page-fetch", { tabId: ctx.tabId, url }).catch(() => ({ error: "tab closed" }) as { error: string });
      if ("base64" in res && res.base64) {
        const bytes = base64ToUint8(res.base64);
        ctx.meter.add(bytes.length);
        ctx.onBytes?.(bytes.length);
        if (sniffImage(bytes)) result = { bytes, mime: res.mime ?? "" };
      }
    }

    if (result) return finalize(result.bytes, ctx.settings);
    if (ctx.signal.aborted) throw new DOMException("Aborted", "AbortError");
    if (lastError instanceof HttpError && [400, 401, 403, 404, 410].includes(lastError.status) && attempt >= 1) break;
    if (attempt < ctx.settings.retries) await sleep(400 * 2 ** attempt + Math.random() * 300, ctx.signal);
  }
  throw lastError instanceof Error ? lastError : new Error("Download failed");
}

async function finalize(bytes: Uint8Array, settings: Settings): Promise<FetchedImage> {
  const info = sniffImage(bytes)!;
  if (KOMIK_IMAGE_EXTENSIONS.includes(info.ext) || !settings.convertUnsupported) {
    return { data: bytes.slice().buffer, mime: info.mime, ext: info.ext, width: info.width, height: info.height, converted: false };
  }
  // AVIF / JXL / HEIC pages: Komik can't open them, so re-encode as JPEG (or PNG when decoding fails as JPEG).
  try {
    const out = await convertImage(bytes, "image/jpeg", settings.jpegQuality);
    return { data: out.data, mime: "image/jpeg", ext: "jpg", width: out.width, height: out.height, converted: true };
  } catch {
    return { data: bytes.slice().buffer, mime: info.mime, ext: info.ext, width: info.width, height: info.height, converted: false };
  }
}
