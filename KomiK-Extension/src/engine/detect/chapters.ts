// Finds chapter / episode / issue lists on series pages, and the site's own comic file downloads.
import type { ChapterRef, FileLink } from "@/shared/types";
import { absoluteUrl, cleanText, COMIC_FILE_EXTENSIONS, extOf, uniqueBy } from "@/shared/util";

const CHAPTER_TEXT = /(?:\b(?:chapter|chap|ch|episode|ep|issue|part|capitulo|capítulo|chapitre|kapitel|глава|bab)\.?\s*#?\s*(\d+(?:\.\d+)?))|(?:第\s*(\d+)\s*[話话章回])|(?:#\s*(\d+(?:\.\d+)?)\b)/i;
const CHAPTER_URL = /(?:chapter|chap|ch|episode|ep|issue|capitulo|chapitre)[-_/.]?(\d+(?:[-_.]\d+)?)(?:[/?#.-]|$)/i;
const VOLUME_TEXT = /\b(?:vol(?:ume)?|v)\.?\s*(\d+)\b/i;

function numberFrom(text: string, url: string): number | null {
  const t = CHAPTER_TEXT.exec(text);
  if (t) return Number.parseFloat(t[1] ?? t[2] ?? t[3]);
  const u = CHAPTER_URL.exec(url);
  if (u) return Number.parseFloat(u[1].replace(/[-_]/, "."));
  return null;
}

export function chapterFromLink(a: Element, base: string): ChapterRef | null {
  const url = absoluteUrl(a.getAttribute("href"), base);
  if (!url || url.startsWith("data:")) return null;
  const title = cleanText(a.getAttribute("title") || a.textContent);
  const number = numberFrom(title, url);
  if (number === null || !Number.isFinite(number)) return null;
  const vol = VOLUME_TEXT.exec(title);
  const row = a.closest("li, tr, .chapter, [class*=chapter], [class*=episode]");
  const dateText = row ? cleanText(row.querySelector("time, .date, [class*=date], [class*=time]")?.textContent ?? "") : "";
  return { url, title: title.slice(0, 140) || `Chapter ${number}`, number, volume: vol ? Number.parseInt(vol[1], 10) : null, date: dateText || undefined };
}

/** Groups chapter-like links by their list container and keeps the biggest consistent list. */
export function findChapters(doc: Document, base: string, currentUrl: string): ChapterRef[] {
  const groups = new Map<Element, ChapterRef[]>();
  doc.querySelectorAll("a[href]").forEach((a) => {
    if (a.closest("header, footer, nav, [class*=breadcrumb]")) return;
    const ref = chapterFromLink(a, base);
    if (!ref) return;
    const container = a.closest("ul, ol, table, tbody, [class*=chapter-list], [class*=chapterlist], [class*=episode-list], [id*=chapter], [class*=chapters], [class*=episodes]") ?? a.parentElement?.parentElement ?? a.parentElement;
    if (!container) return;
    const list = groups.get(container) ?? [];
    list.push(ref);
    groups.set(container, list);
  });

  let best: ChapterRef[] = [];
  for (const list of groups.values()) {
    const unique = uniqueBy(list, (c) => c.url.replace(/#.*$/, ""));
    if (unique.length > best.length) best = unique;
  }
  // A lone "next chapter" / "previous chapter" pair on a reader page isn't a chapter list.
  if (best.length < 3) return [];
  best = best.filter((c) => c.url.replace(/#.*$/, "") !== currentUrl.replace(/#.*$/, ""));
  return sortChapters(best);
}

export function sortChapters(chapters: ChapterRef[]): ChapterRef[] {
  return uniqueBy(chapters, (c) => `${c.volume ?? ""}|${c.number}`).sort((a, b) => (a.volume ?? 0) - (b.volume ?? 0) || (a.number ?? 0) - (b.number ?? 0));
}

const DOWNLOAD_WORDS = /download|descargar|télécharger|herunterladen|скачать|baixar|unduh|\.cbz|\.cbr|\.pdf|\.zip/i;

/** Links and buttons that download the comic as a file (the site's own CBZ / CBR / PDF / ZIP). */
export function findFileLinks(doc: Document, base: string): FileLink[] {
  const out: FileLink[] = [];
  doc.querySelectorAll("a[href], [data-href], [data-url], [data-download], button[formaction]").forEach((el) => {
    const raw = el.getAttribute("href") ?? el.getAttribute("data-href") ?? el.getAttribute("data-url") ?? el.getAttribute("data-download") ?? el.getAttribute("formaction");
    const url = absoluteUrl(raw, base);
    if (!url || url.startsWith("data:")) return;
    let ext = extOf(url);
    const label = cleanText(el.textContent || el.getAttribute("title") || el.getAttribute("aria-label") || "");
    const downloadAttr = el.getAttribute("download");
    if (downloadAttr) {
      const e = /\.([a-z0-9]{2,4})$/i.exec(downloadAttr);
      if (e) ext = e[1].toLowerCase();
    }
    if (!COMIC_FILE_EXTENSIONS.includes(ext)) {
      // "Download CBZ" buttons pointing at /download?id=… without an extension.
      const fromLabel = /\b(cbz|cbr|cb7|pdf|zip|epub)\b/i.exec(label);
      if (!(fromLabel && DOWNLOAD_WORDS.test(label))) return;
      ext = fromLabel[1].toLowerCase();
    }
    let name = downloadAttr || "";
    if (!name) {
      try {
        name = decodeURIComponent(new URL(url).pathname.split("/").pop() ?? "");
      } catch {
        name = "";
      }
    }
    const sizeMatch = /(\d+(?:\.\d+)?)\s*(kb|mb|gb)\b/i.exec(el.closest("li, tr, div")?.textContent ?? label);
    const size = sizeMatch ? Math.round(Number.parseFloat(sizeMatch[1]) * 1024 ** ({ kb: 1, mb: 2, gb: 3 }[sizeMatch[2].toLowerCase() as "kb" | "mb" | "gb"])) : undefined;
    out.push({ url, ext, name: name || `${label || "comic"}.${ext}`, label: label.slice(0, 80), size });
  });
  return uniqueBy(out, (f) => f.url);
}

/** "Page 1 of 20" readers: a <select> of page URLs or numbered page links around one big image. */
export function findPagination(doc: Document, base: string, currentUrl: string): string[] {
  const current = currentUrl.replace(/#.*$/, "");
  for (const select of Array.from(doc.querySelectorAll("select"))) {
    const options = Array.from(select.querySelectorAll("option"));
    if (options.length < 3) continue;
    const label = `${select.id} ${select.className} ${select.getAttribute("name") ?? ""}`;
    const urls = options.map((o) => absoluteUrl(o.getAttribute("value"), base)).filter((u): u is string => !!u && /^https?:/.test(u));
    if (urls.length !== options.length) continue;
    const pageLike = /page/i.test(label) || options.every((o) => /^\s*(?:page\s*)?\d+\s*(?:\/\s*\d+)?\s*$/i.test(o.textContent ?? ""));
    if (!pageLike) continue;
    const unique = uniqueBy(urls, (u) => u);
    if (unique.some((u) => u.replace(/#.*$/, "") === current) || unique.length >= 3) return unique;
  }
  return [];
}
