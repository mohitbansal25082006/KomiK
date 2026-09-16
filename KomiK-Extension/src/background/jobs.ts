// Turns download requests into persisted jobs and wakes the offscreen engine.
import { putJob } from "@/shared/db";
import { applySeriesMemory, finalizeMeta, metaForChapter, rememberSeries } from "@/shared/enrich";
import type { JobRequest } from "@/shared/messages";
import { loadSettings } from "@/shared/settings";
import type { ChapterRef, ComicMeta, DetectResult, Job, OutputFormat } from "@/shared/types";
import { uid } from "@/shared/util";
import { toEngine } from "./offscreen";

export async function queueJobs(requests: JobRequest[]): Promise<string[]> {
  const settings = await loadSettings();
  const ids: string[] = [];
  const now = Date.now();
  for (const [i, req] of requests.entries()) {
    const meta = finalizeMeta(req.meta, settings);
    const job: Job = {
      id: uid("job-"),
      createdAt: now + i,
      updatedAt: now + i,
      status: "queued",
      format: req.format,
      meta,
      chapterUrl: req.chapterUrl,
      sourceUrl: req.sourceUrl,
      tabId: req.tabId,
      pages: req.pages.map((p, index) => ({ ...p, index, done: false })),
      coverIndex: Math.min(Math.max(0, req.coverIndex ?? 0), Math.max(0, req.pages.length - 1)),
      totalBytes: 0,
      doneBytes: 0,
      speedBps: 0,
      batchId: req.batchId,
      batchLabel: req.batchLabel,
      warnings: []
    };
    await putJob(job);
    ids.push(job.id);
  }
  await toEngine("engine-kick", {});
  return ids;
}

export async function queueChapters(base: DetectResult, editedMeta: ComicMeta, chapters: ChapterRef[], format: OutputFormat, tabId?: number): Promise<string[]> {
  const settings = await loadSettings();
  if (settings.rememberSeriesEdits) await rememberSeries(editedMeta, base.meta).catch(() => undefined);
  const batchId = uid("batch-");
  const label = `${editedMeta.series || base.meta.series} · ${chapters.length} chapter${chapters.length === 1 ? "" : "s"}`;
  return queueJobs(
    chapters.map((chapter) => ({
      meta: metaForChapter(editedMeta, chapter, settings),
      format,
      pages: [],
      sourceUrl: base.url,
      chapterUrl: chapter.url,
      tabId,
      batchId,
      batchLabel: label
    }))
  );
}

/** One-click download with default settings (keyboard shortcut, context menu). */
export async function quickDownload(result: DetectResult, tabId?: number): Promise<{ ids: string[]; kind: "pages" | "chapters" | "none" }> {
  const settings = await loadSettings();
  const meta = await applySeriesMemory(result.meta);
  if (result.pages.length >= 1) {
    const ids = await queueJobs([{ meta, format: settings.defaultFormat, pages: result.pages, sourceUrl: result.url, tabId }]);
    return { ids, kind: "pages" };
  }
  return { ids: [], kind: "none" };
}
