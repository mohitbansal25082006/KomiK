// Finds chapter / episode / issue lists on series pages.
import type { ChapterRef } from "@/shared/types";
import { findDate, stripDates } from "@/shared/dates";
import { absoluteUrl, cleanText, uniqueBy } from "@/shared/util";

const CHAPTER_TEXT = /(?:\b(?:chapter|chap|ch|episode|ep|issue|part|capitulo|capítulo|chapitre|kapitel|глава|bab)\.?\s*#?\s*(\d+(?:\.\d+)?))|(?:第\s*(\d+)\s*[話话章回])|(?:#\s*(\d+(?:\.\d+)?)\b)/i;
const CHAPTER_URL = /(?:chapter|chap|ch|episode|ep|issue|capitulo|chapitre)[-_/.]?(\d+(?:[-_.]\d+)?)(?:[/?#.-]|$)/i;
const VOLUME_TEXT = /\b(?:vol(?:ume)?|v)\.?\s*(\d+)\b/i;

/** Text of an element with a space between child elements, so "Chapter 1" and a date never glue together. */
function spacedText(el: Element): string {
  const parts: string[] = [];
  el.childNodes.forEach((node) => {
    const text = node.nodeType === 3 ? node.textContent : (node as Element).textContent;
    if (text && text.trim()) parts.push(text.trim());
  });
  return cleanText(parts.join(" ") || el.textContent);
}

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
  const raw = cleanText(a.getAttribute("title") || "") || spacedText(a);
  const title = stripDates(raw);
  const number = numberFrom(title, url);
  if (number === null || !Number.isFinite(number)) return null;
  const vol = VOLUME_TEXT.exec(title);
  const row = a.closest("li, tr, .chapter, [class*=chapter], [class*=episode]");
  const dateText = row ? cleanText(row.querySelector("time, .date, [class*=date], [class*=time]")?.textContent ?? "") : "";
  const dateInText = findDate(raw);
  return {
    url,
    title: title.slice(0, 140) || `Chapter ${number}`,
    number,
    volume: vol ? Number.parseInt(vol[1], 10) : null,
    date: dateText || dateInText || undefined
  };
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
