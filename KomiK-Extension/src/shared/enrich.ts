import { getSeriesMemory, putSeriesMemory, seriesKey } from "./db";
import { composeTitle } from "./identity";
import { cleanTags, splitPeople } from "./tags";
import type { ChapterRef, ComicMeta, Settings } from "./types";
import { cleanText } from "./util";

/** Final clean-up right before a comic is queued: tidy tags, credits and title so Komik reads them well. */
export function finalizeMeta(input: ComicMeta, settings: Pick<Settings, "cleanTags" | "maxTags" | "addSiteTag" | "mangaDirection" | "includeChapterTitle">): ComicMeta {
  const meta: ComicMeta = {
    ...input,
    writers: splitPeople(input.writers),
    artists: splitPeople(input.artists),
    series: cleanText(input.series),
    title: cleanText(input.title),
    summary: (input.summary ?? "").trim()
  };
  const exclude = [meta.series, meta.title, meta.site, meta.site.replace(/\.[a-z]{2,}$/i, "")];
  meta.genres = cleanTags(input.genres, { clean: settings.cleanTags, max: settings.maxTags, exclude });
  const genreKeys = new Set(meta.genres.map((g) => g.toLowerCase()));
  const extraTags = settings.addSiteTag && meta.site ? [meta.site] : [];
  const tags = cleanTags([...input.tags, ...extraTags], { clean: settings.cleanTags, max: 0, exclude: settings.addSiteTag ? exclude.slice(0, 2) : exclude })
    .filter((t) => !genreKeys.has(t.toLowerCase()));
  // maxTags covers genres and tags together; 0 means no limit.
  meta.tags = settings.maxTags > 0 ? tags.slice(0, Math.max(0, settings.maxTags - meta.genres.length)) : tags;

  if (settings.mangaDirection === "rtl") meta.manga = "YesAndRightToLeft";
  else if (settings.mangaDirection === "ltr" && meta.manga === "YesAndRightToLeft") meta.manga = "Yes";

  const composed = composeTitle({
    series: meta.series,
    volume: meta.volume,
    number: meta.number,
    kind: meta.numberKind,
    chapterTitle: meta.chapterTitle,
    includeChapterTitle: settings.includeChapterTitle
  });
  if (!meta.title) meta.title = composed || meta.series || "Untitled Comic";
  if (!meta.series) meta.series = meta.title;
  return meta;
}

/** Details for one chapter of a series, starting from the series page's details. */
export function metaForChapter(base: ComicMeta, chapter: ChapterRef, settings: Pick<Settings, "includeChapterTitle">): ComicMeta {
  const number = chapter.number !== null ? String(Math.round(chapter.number * 1000) / 1000) : "";
  const volume = chapter.volume !== null ? String(chapter.volume) : "";
  const subtitle = chapter.title
    .replace(/^.*?(?:chapter|chap|ch|episode|ep|issue)\.?\s*#?\s*\d+(?:\.\d+)?/i, "")
    .replace(/^[\s:\-–—.|]+/, "")
    .trim();
  const kind = /episode|\bep\b/i.test(chapter.title) ? "episode" : /issue|#/i.test(chapter.title) ? "issue" : "chapter";
  const meta: ComicMeta = { ...base, number, volume, numberKind: number ? kind : "none", chapterTitle: subtitle, web: chapter.url, title: "" };
  meta.title = composeTitle({ series: base.series, volume, number, kind: meta.numberKind, chapterTitle: subtitle, includeChapterTitle: settings.includeChapterTitle });
  return meta;
}

const REMEMBERED: Array<keyof ComicMeta> = ["series", "writers", "artists", "publisher", "genres", "tags", "summary", "language", "manga", "ageRating"];

/** Fills empty fields from the user's earlier edits to the same series on the same site. */
export async function applySeriesMemory(meta: ComicMeta): Promise<ComicMeta> {
  if (!meta.series) return meta;
  try {
    const memory = await getSeriesMemory(seriesKey(meta.series, meta.site));
    if (!memory) return meta;
    const out = { ...meta } as Record<string, unknown>;
    for (const key of REMEMBERED) {
      const remembered = memory.meta[key];
      if (remembered === undefined) continue;
      const current = out[key];
      const empty = Array.isArray(current) ? current.length === 0 : !current;
      // Tags and genres the user curated replace the site's raw lists.
      if (empty || key === "tags" || key === "genres") out[key] = remembered;
    }
    return out as unknown as ComicMeta;
  } catch {
    return meta;
  }
}

export async function rememberSeries(meta: ComicMeta, original: ComicMeta | null): Promise<void> {
  if (!meta.series) return;
  const changed: Partial<ComicMeta> = {};
  for (const key of REMEMBERED) {
    const now = JSON.stringify(meta[key]);
    if (!original || now !== JSON.stringify(original[key])) (changed as Record<string, unknown>)[key] = meta[key];
  }
  if (!Object.keys(changed).length) return;
  const key = seriesKey(meta.series, meta.site);
  const existing = await getSeriesMemory(key);
  await putSeriesMemory({ key, meta: { ...(existing?.meta ?? {}), ...changed }, updatedAt: Date.now() });
}
