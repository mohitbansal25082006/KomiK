// The download engine: resolves chapter pages, downloads pages in parallel, packs the comic with its
// ComicInfo.xml and saves it. Jobs and fetched pages live in IndexedDB, so work resumes after restarts.
import { scanDocument } from "@/engine/scan";
import { deleteJob, deletePages, getJob, getJobs, getPage, putHistory, putJob, putPage } from "@/shared/db";
import { broadcast, listen, send, type JobAction } from "@/shared/messages";
import { pageFileName, planSave } from "@/shared/naming";
import type { DetectResult, Job, JobsSnapshot, Settings } from "@/shared/types";
import { uid } from "@/shared/util";
import { fetchPage } from "./fetcher";
import { ConnectionLimiter, SpeedMeter } from "./limiter";
import { buildPdf, buildZip, comicInfoFor, coverThumbnail, repackArchive, retagPdf, type PackPage } from "./pack";
import { saveFile, saveFolder } from "./save";

const ACTIVE = new Set<Job["status"]>(["resolving", "downloading", "packing", "saving"]);

const jobs = new Map<string, Job>();
const controllers = new Map<string, AbortController>();
const meters = new Map<string, SpeedMeter>();
const globalMeter = new SpeedMeter();
let settings: Settings | null = null;
let limiter: ConnectionLimiter | null = null;
let pumping = false;

async function loadEngineSettings(): Promise<Settings> {
  settings = await send("off-settings", {});
  if (!limiter) limiter = new ConnectionLimiter(settings.perHostConnections, settings.globalConnections);
  else limiter.configure(settings.perHostConnections, settings.globalConnections);
  return settings;
}

// ── Persistence & broadcasting ───────────────────────────────────────────────────────
const dirty = new Set<string>();
let flushTimer: ReturnType<typeof setTimeout> | null = null;
let broadcastTimer: ReturnType<typeof setTimeout> | null = null;

function touch(job: Job, immediate = false) {
  job.updatedAt = Date.now();
  dirty.add(job.id);
  if (immediate) void flush();
  else if (!flushTimer) flushTimer = setTimeout(() => void flush(), 900);
  if (!broadcastTimer) {
    broadcastTimer = setTimeout(() => {
      broadcastTimer = null;
      broadcast("jobs", snapshot());
    }, immediate ? 30 : 350);
  }
}

async function flush() {
  if (flushTimer) clearTimeout(flushTimer);
  flushTimer = null;
  const ids = Array.from(dirty);
  dirty.clear();
  await Promise.all(ids.map((id) => (jobs.has(id) ? putJob(jobs.get(id)!) : Promise.resolve())));
}

function snapshot(): JobsSnapshot {
  const list = Array.from(jobs.values()).sort((a, b) => a.createdAt - b.createdAt);
  for (const job of list) if (ACTIVE.has(job.status)) job.speedBps = meters.get(job.id)?.bps ?? 0;
  return {
    jobs: list,
    active: list.filter((j) => ACTIVE.has(j.status)).length,
    queued: list.filter((j) => j.status === "queued").length,
    speedBps: globalMeter.bps
  };
}

// ── Scheduling ───────────────────────────────────────────────────────────────────────
async function reload() {
  for (const job of await getJobs()) {
    const live = jobs.get(job.id);
    if (live && (ACTIVE.has(live.status) || controllers.has(job.id))) continue;
    // Work interrupted by a browser restart continues from the cached pages.
    if (ACTIVE.has(job.status) && !controllers.has(job.id)) job.status = "queued";
    jobs.set(job.id, job);
  }
}

async function pump() {
  if (pumping) return;
  pumping = true;
  try {
    const s = settings ?? (await loadEngineSettings());
    for (;;) {
      const running = Array.from(jobs.values()).filter((j) => controllers.has(j.id)).length;
      if (running >= s.parallelJobs) break;
      const next = Array.from(jobs.values())
        .filter((j) => j.status === "queued" && !controllers.has(j.id))
        .sort((a, b) => a.createdAt - b.createdAt)[0];
      if (!next) break;
      const controller = new AbortController();
      controllers.set(next.id, controller);
      void runJob(next, controller.signal).finally(() => {
        controllers.delete(next.id);
        meters.delete(next.id);
        void pump();
      });
    }
  } finally {
    pumping = false;
  }
  broadcast("jobs", snapshot());
}

// ── Job pipeline ─────────────────────────────────────────────────────────────────────
async function runJob(job: Job, signal: AbortSignal) {
  const s = await loadEngineSettings();
  const meter = new SpeedMeter();
  meters.set(job.id, meter);
  job.error = undefined;
  try {
    if (job.file) await runFileJob(job, s, meter, signal);
    else await runPagesJob(job, s, meter, signal);
  } catch (err) {
    if (signal.aborted) return; // paused or cancelled: the action already set the status
    job.status = "error";
    job.error = err instanceof Error ? err.message : String(err);
    touch(job, true);
  }
}

async function resolveChapter(job: Job, s: Settings, signal: AbortSignal) {
  if (job.pages.length || !job.chapterUrl) return;
  job.status = "resolving";
  touch(job, true);

  let result: DetectResult | null = null;
  try {
    const res = await fetch(job.chapterUrl, { credentials: "include", signal });
    if (res.ok) {
      const html = await res.text();
      const doc = new DOMParser().parseFromString(html, "text/html");
      result = scanDocument(doc, { url: res.url || job.chapterUrl, live: false, rules: s.siteRules, minPageWidth: 0 });
      // "One page per URL" readers: visit each reader page and take its main image.
      if (result.pages.length <= 2 && result.pagination.length >= 3) {
        const pages = await Promise.all(
          result.pagination.map(async (pageUrl) => {
            const r = await fetch(pageUrl, { credentials: "include", signal });
            const d = new DOMParser().parseFromString(await r.text(), "text/html");
            return scanDocument(d, { url: pageUrl, live: false, rules: s.siteRules, minPageWidth: 0 }).pages[0];
          })
        );
        result.pages = pages.filter(Boolean);
      }
    }
  } catch (err) {
    if (signal.aborted) throw err;
  }

  // Script-rendered readers: let a hidden tab run the page, then scan it.
  if (!result || result.pages.length < 2) {
    const viaTab = await send("off-resolve-chapter", { url: job.chapterUrl });
    if ("error" in viaTab) {
      if (!result?.pages.length) throw new Error(`No pages found in this chapter (${viaTab.error}).`);
    } else if (viaTab.pages.length > (result?.pages.length ?? 0)) {
      result = viaTab;
    }
  }
  if (!result || !result.pages.length) throw new Error("No pages found in this chapter.");

  job.pages = result.pages.map((p, index) => ({ ...p, referer: p.referer ?? job.chapterUrl, index, done: false }));
  if (!job.meta.chapterTitle && result.meta.chapterTitle) job.meta.chapterTitle = result.meta.chapterTitle;
  if (!job.meta.number && result.meta.number) job.meta.number = result.meta.number;
}

async function runPagesJob(job: Job, s: Settings, meter: SpeedMeter, signal: AbortSignal) {
  await resolveChapter(job, s, signal);
  job.status = "downloading";
  job.warnings = job.warnings.filter((w) => !w.startsWith("Converted"));
  touch(job, true);

  // Pages cached by an earlier, interrupted run count as done.
  for (const page of job.pages) {
    if (page.done && !(await getPage(job.id, page.index))) page.done = false;
  }
  job.doneBytes = job.pages.reduce((sum, p) => sum + (p.done ? p.bytes ?? 0 : 0), 0);

  let converted = 0;
  const failures: string[] = [];
  const todo = job.pages.filter((p) => !p.done);
  await Promise.all(
    todo.map(async (page) => {
      try {
        const img = await fetchPage(page.url, page.referer ?? job.sourceUrl, {
          settings: s,
          limiter: limiter!,
          meter,
          tabId: job.tabId,
          signal,
          onBytes: (n) => globalMeter.add(n)
        });
        await putPage({ key: `${job.id}:${page.index}`, jobId: job.id, index: page.index, data: img.data, mime: img.mime, ext: img.ext, width: img.width, height: img.height });
        page.done = true;
        page.error = undefined;
        page.bytes = img.data.byteLength;
        page.mime = img.mime;
        page.width = img.width ?? page.width;
        page.height = img.height ?? page.height;
        if (img.converted) converted++;
        job.doneBytes += img.data.byteLength;
        touch(job);
      } catch (err) {
        if (signal.aborted) throw err;
        page.error = err instanceof Error ? err.message : String(err);
        failures.push(`Page ${page.index + 1}: ${page.error}`);
        touch(job);
      }
    })
  );
  if (signal.aborted) throw new DOMException("Aborted", "AbortError");
  if (converted) job.warnings.push(`Converted ${converted} page${converted === 1 ? "" : "s"} to JPEG so Komik can open them.`);
  if (failures.length) {
    const done = job.pages.filter((p) => p.done).length;
    throw new Error(`${failures.length} of ${job.pages.length} pages failed (${done} saved for retry). ${failures[0]}`);
  }

  // ── Pack ──
  job.status = "packing";
  touch(job, true);
  const packPages: PackPage[] = [];
  const order = job.pages.map((p) => p.index);
  if (s.coverFirst && job.coverIndex > 0) order.unshift(...order.splice(job.coverIndex, 1));
  for (const [position, index] of order.entries()) {
    const stored = await getPage(job.id, index);
    if (!stored) throw new Error(`Page ${index + 1} is missing from the cache. Retry to download it again.`);
    packPages.push({ name: pageFileName(position, order.length, stored.ext, s.padPages), data: stored.data, mime: stored.mime, width: stored.width, height: stored.height });
  }
  const coverPosition = s.coverFirst ? 0 : job.coverIndex;
  job.meta.web = job.meta.web || job.chapterUrl || job.sourceUrl;
  const xml = comicInfoFor(job.meta, packPages, coverPosition, job.chapterUrl || job.sourceUrl);
  const plan = planSave(job.meta, job.format, s, packPages.length);

  job.status = "saving";
  touch(job, true);
  let saved;
  if (job.format === "folder") {
    const files = packPages.map((p) => ({ name: p.name, blob: new Blob([p.data], { type: p.mime }) }));
    files.push({ name: "ComicInfo.xml", blob: new Blob([xml], { type: "application/xml" }) });
    saved = await saveFolder(plan.folder, plan.fileName, files, s);
  } else {
    const blob = job.format === "pdf" ? await buildPdf(packPages, job.meta, s.jpegQuality) : await buildZip(packPages, xml);
    job.totalBytes = blob.size;
    saved = await saveFile(plan.folder, plan.fileName, blob, s);
  }
  await complete(job, saved, packPages[coverPosition]?.data, packPages.length);
}

async function runFileJob(job: Job, s: Settings, meter: SpeedMeter, signal: AbortSignal) {
  const file = job.file!;
  job.status = "downloading";
  touch(job, true);

  const res = await fetch(file.url, { credentials: "include", signal });
  if (!res.ok) throw new Error(`The site answered HTTP ${res.status} for ${file.name}.`);
  const total = Number.parseInt(res.headers.get("content-length") ?? "0", 10);
  job.totalBytes = total;
  const reader = res.body!.getReader();
  const chunks: Uint8Array[] = [];
  for (;;) {
    const { done, value } = await reader.read();
    if (done) break;
    chunks.push(value);
    job.doneBytes += value.length;
    meter.add(value.length);
    globalMeter.add(value.length);
    touch(job);
  }
  const blobIn = new Blob(chunks as BlobPart[]);
  const head = new Uint8Array(await blobIn.slice(0, 8).arrayBuffer());
  if (head[0] === 0x3c) throw new Error("The link returned a web page instead of the file. Download it from the page once, then try again.");

  job.status = "packing";
  touch(job, true);
  const ext = file.ext.toLowerCase();
  let blob: Blob = blobIn;
  let pages = 0;
  let outExt = ext;
  const isZip = head[0] === 0x50 && head[1] === 0x4b;
  if ((ext === "cbz" || ext === "zip") && isZip) {
    try {
      const repacked = await repackArchive(await blobIn.arrayBuffer(), job.meta, job.sourceUrl);
      blob = repacked.blob;
      pages = repacked.pages;
      outExt = job.format === "zip" ? "zip" : "cbz";
    } catch (err) {
      job.warnings.push(`Saved as-is: the archive could not be opened to add metadata (${err instanceof Error ? err.message : err}).`);
    }
  } else if (ext === "pdf") {
    try {
      blob = await retagPdf(await blobIn.arrayBuffer(), job.meta);
    } catch {
      job.warnings.push("Saved as-is: this PDF's details could not be updated.");
    }
  } else {
    job.warnings.push(`${ext.toUpperCase()} files can't be rewritten in the browser, so the details are only kept in KomiK history.`);
  }

  job.status = "saving";
  touch(job, true);
  const plan = planSave(job.meta, "cbz", s);
  const fileName = plan.fileName.replace(/\.cbz$/i, `.${outExt}`);
  const saved = await saveFile(plan.folder, fileName, blob, s);
  job.totalBytes = blob.size;
  await complete(job, saved, undefined, pages);
}

async function complete(job: Job, saved: { path: string; downloadId?: number; warning?: string }, coverData: ArrayBuffer | undefined, pageCount: number) {
  if (saved.warning) job.warnings.push(saved.warning);
  job.status = "done";
  job.savedPath = saved.path;
  job.downloadId = saved.downloadId;
  job.doneBytes = Math.max(job.doneBytes, job.totalBytes);
  touch(job, true);
  await flush();

  await putHistory({
    id: uid("hist-"),
    title: job.meta.title,
    series: job.meta.series,
    format: job.file ? ((job.file.ext as Job["format"]) ?? "cbz") : job.format,
    pages: pageCount,
    bytes: job.totalBytes || job.doneBytes,
    sourceUrl: job.chapterUrl || job.sourceUrl,
    savedPath: saved.path,
    downloadId: saved.downloadId,
    coverThumb: coverData ? await coverThumbnail(coverData) : undefined,
    tags: [...job.meta.genres, ...job.meta.tags],
    finishedAt: Date.now(),
    meta: job.meta
  });
  broadcast("history");
  await deletePages(job.id).catch(() => undefined);

  const batchLeft = job.batchId ? Array.from(jobs.values()).filter((j) => j.batchId === job.batchId && j.status !== "done").length : 0;
  if (!job.batchId || batchLeft === 0) {
    const label = job.batchId ? job.batchLabel ?? job.meta.series : job.meta.title;
    await send("off-notify", {
      title: job.batchId ? "Batch downloaded!" : "Comic downloaded!",
      message: `${label}${pageCount ? ` · ${pageCount} pages` : ""} → ${saved.path.split(/[\\/]/).slice(-2).join("/")}`,
      jobId: job.id
    }).catch(() => undefined);
  }
}

// ── Actions ──────────────────────────────────────────────────────────────────────────
async function action(id: string, act: JobAction): Promise<boolean> {
  const job = jobs.get(id) ?? (await getJob(id));
  if (!job) return false;
  jobs.set(id, job);
  const controller = controllers.get(id);
  switch (act) {
    case "pause":
      if (job.status === "done") return false;
      controller?.abort();
      job.status = "paused";
      break;
    case "resume":
    case "retry":
      if (job.status === "done" && act === "retry") {
        job.pages.forEach((p) => (p.done = false));
        job.doneBytes = 0;
      }
      job.status = "queued";
      job.error = undefined;
      job.pages.forEach((p) => (p.error = undefined));
      break;
    case "cancel":
      controller?.abort();
      job.status = "cancelled";
      await deletePages(id).catch(() => undefined);
      break;
    case "remove":
      controller?.abort();
      jobs.delete(id);
      await deleteJob(id);
      broadcast("jobs", snapshot());
      return true;
  }
  touch(job, true);
  await flush();
  void pump();
  return true;
}

listen("offscreen", {
  "engine-kick": async () => {
    await loadEngineSettings();
    await reload();
    void pump();
    return { ok: true };
  },
  "engine-action": async ({ id, action: act }) => ({ ok: await action(id, act) }),
  "engine-snapshot": async () => {
    if (!jobs.size) await reload();
    return snapshot();
  },
  "engine-clear-finished": async () => {
    for (const job of Array.from(jobs.values())) {
      if (job.status === "done" || job.status === "cancelled") {
        jobs.delete(job.id);
        await deleteJob(job.id);
      }
    }
    broadcast("jobs", snapshot());
    return { ok: true };
  }
});

// Boot: pick up anything left from the last session.
void (async () => {
  try {
    const s = await loadEngineSettings();
    await reload();
    if (s.autoResume) void pump();
    else {
      for (const job of jobs.values()) if (job.status === "queued" && job.updatedAt < Date.now() - 5000) job.status = "paused";
      broadcast("jobs", snapshot());
    }
  } catch {
    /* the background will kick us when needed */
  }
})();
