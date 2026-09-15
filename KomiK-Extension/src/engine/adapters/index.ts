// Adapters read pages, chapters and details straight from well-known reader layouts, which is more
// precise than the universal scanner. They are generic: each one matches a page *structure* used by
// many sites (common WordPress reader themes, JSON-in-page readers), never one specific website.
import type { ChapterRef, PageRef, SiteRule } from "@/shared/types";
import { absoluteUrl, cleanText, hostOf, uniqueBy } from "@/shared/util";
import { chapterFromLink, sortChapters } from "../detect/chapters";
import { imageUrlFromElement } from "../detect/harvest";

export interface AdapterResult {
  name: string;
  pages: PageRef[];
  chapters: ChapterRef[];
  series?: string;
  title?: string;
  genres?: string[];
  tags?: string[];
  writers?: string[];
  artists?: string[];
  summary?: string;
  cover?: string;
  isSeriesPage?: boolean;
}

interface Adapter {
  name: string;
  run(doc: Document, url: string): AdapterResult | null;
}

const texts = (doc: ParentNode, selector: string) => Array.from(doc.querySelectorAll(selector)).map((e) => cleanText(e.textContent)).filter(Boolean);

function imagesFrom(doc: ParentNode, selector: string, base: string, source: PageRef["source"] = "adapter"): PageRef[] {
  const pages: PageRef[] = [];
  doc.querySelectorAll(selector).forEach((img) => {
    const { url } = imageUrlFromElement(img, base);
    if (url && !url.startsWith("data:")) pages.push({ url, referer: base, source });
  });
  return uniqueBy(pages, (p) => p.url);
}

function chaptersFrom(doc: ParentNode, selector: string, base: string): ChapterRef[] {
  const list: ChapterRef[] = [];
  doc.querySelectorAll(selector).forEach((a) => {
    const ref = chapterFromLink(a, base);
    if (ref) list.push(ref);
  });
  return sortChapters(list);
}

/** Pulls a JSON array of image URLs out of a script call like `reader.run({ … "images": [ … ] })`. */
function imagesFromScriptJson(doc: Document, base: string, key = "images"): string[] {
  for (const s of Array.from(doc.querySelectorAll("script:not([src])"))) {
    const text = s.textContent ?? "";
    const idx = text.indexOf(`"${key}"`);
    if (idx < 0) continue;
    const start = text.indexOf("[", idx);
    if (start < 0) continue;
    let depth = 0;
    for (let i = start; i < text.length && i < start + 500_000; i++) {
      if (text[i] === "[") depth++;
      else if (text[i] === "]" && --depth === 0) {
        try {
          const arr = JSON.parse(text.slice(start, i + 1)) as unknown[];
          const urls = arr.filter((u): u is string => typeof u === "string").map((u) => absoluteUrl(u, base)).filter((u): u is string => !!u);
          if (urls.length) return urls;
        } catch {
          /* not JSON */
        }
        break;
      }
    }
  }
  return [];
}

const ADAPTERS: Adapter[] = [
  {
    // WordPress "manga reader" theme family: .reading-content with .wp-manga-chapter-img pages.
    name: "WP Manga reader theme",
    run(doc, url) {
      const reader = doc.querySelector(".reading-content, .read-container .reading-content");
      const chapterList = doc.querySelector(".wp-manga-chapter, .listing-chapters_wrap, .version-chap");
      if (!reader && !chapterList) return null;
      const pages = reader ? imagesFrom(reader, "img.wp-manga-chapter-img, .page-break img, img", url) : [];
      return {
        name: this.name,
        pages,
        chapters: chaptersFrom(doc, ".wp-manga-chapter a, li.a-h a, .version-chap li a", url),
        series: cleanText(doc.querySelector(".post-title h1, .post-title h3, #chapter-heading, .breadcrumb li:nth-last-child(2) a")?.textContent)?.replace(/\s*[-–]\s*(?:chapter|ch\.?)\s*\d.*$/i, ""),
        genres: texts(doc, ".genres-content a, .genres a"),
        tags: texts(doc, ".tags-content a"),
        writers: texts(doc, ".author-content a"),
        artists: texts(doc, ".artist-content a"),
        summary: cleanText(doc.querySelector(".summary__content, .description-summary")?.textContent),
        cover: absoluteUrl(doc.querySelector(".summary_image img")?.getAttribute("data-src") ?? doc.querySelector(".summary_image img")?.getAttribute("src"), url) ?? undefined,
        isSeriesPage: !reader && !!chapterList
      };
    }
  },
  {
    // "ts_reader" theme family: #readerarea plus a ts_reader.run({ sources: [{ images: [...] }] }) call.
    name: "TS reader theme",
    run(doc, url) {
      const area = doc.querySelector("#readerarea");
      const chapterList = doc.querySelector("#chapterlist, .eplister");
      if (!area && !chapterList) return null;
      let pages = area ? imagesFrom(area, "img", url) : [];
      const scripted = imagesFromScriptJson(doc, url);
      if (scripted.length > pages.length) pages = scripted.map((u) => ({ url: u, referer: url, source: "adapter" as const }));
      return {
        name: this.name,
        pages,
        chapters: chaptersFrom(doc, "#chapterlist li a, .eplister li a", url),
        series: cleanText(doc.querySelector(".entry-title, .allc a, .headpost .allc a")?.textContent)?.replace(/\s*(?:chapter|ch\.?)\s*\d.*$/i, ""),
        genres: texts(doc, ".mgen a, .seriestugenre a, .genxed a"),
        writers: texts(doc, ".fmed:has(b) i, .tsinfo .imptdt:nth-child(3) i").slice(0, 3),
        summary: cleanText(doc.querySelector('.entry-content[itemprop="description"], .synp .entry-content')?.textContent),
        cover: absoluteUrl(doc.querySelector(".thumb img")?.getAttribute("src"), url) ?? undefined,
        isSeriesPage: !area && !!chapterList
      };
    }
  },
  {
    // Next.js / Nuxt readers that ship the page list in __NEXT_DATA__ / __NUXT__ JSON.
    name: "App data reader",
    run(doc, url) {
      const data = doc.querySelector("script#__NEXT_DATA__, script#__NUXT_DATA__")?.textContent;
      if (!data || data.length > 5_000_000) return null;
      const urls: string[] = [];
      const re = /"(?:url|src|image|imageUrl|img)"\s*:\s*"((?:https?:)?\\?\/\\?\/[^"]+?\.(?:jpe?g|png|webp|avif|gif)(?:\?[^"]*)?)"/gi;
      for (const m of data.matchAll(re)) {
        const u = absoluteUrl(m[1].replace(/\\\//g, "/").replace(/^\/\//, "https://"), url);
        if (u) urls.push(u);
      }
      const unique = uniqueBy(urls, (u) => u);
      if (unique.length < 3) return null;
      return { name: this.name, pages: unique.map((u) => ({ url: u, referer: url, source: "adapter" as const })), chapters: [] };
    }
  }
];

function ruleMatches(rule: SiteRule, url: string): boolean {
  const host = hostOf(url);
  const want = rule.host.replace(/^www\./, "").toLowerCase();
  return rule.enabled && !!want && (host === want || host.endsWith(`.${want}`));
}

export function runSiteRule(doc: Document, url: string, rules: SiteRule[]): AdapterResult | null {
  const rule = rules.find((r) => ruleMatches(r, url));
  if (!rule) return null;
  const safe = (sel: string) => {
    try {
      return sel ? doc.querySelectorAll(sel) : null;
    } catch {
      return null;
    }
  };
  const pages: PageRef[] = [];
  safe(rule.pageSelector)?.forEach((el) => {
    const { url: u } = imageUrlFromElement(el, url);
    if (u) pages.push({ url: u, referer: url, source: "adapter" });
  });
  const chapters: ChapterRef[] = [];
  safe(rule.chapterSelector)?.forEach((a) => {
    const ref = chapterFromLink(a, url);
    if (ref) chapters.push(ref);
  });
  const first = (sel: string) => (sel ? cleanText(safe(sel)?.[0]?.textContent) : "");
  return {
    name: `Site rule · ${rule.host}`,
    pages: uniqueBy(pages, (p) => p.url),
    chapters: sortChapters(chapters),
    title: first(rule.titleSelector) || undefined,
    series: first(rule.seriesSelector) || undefined,
    tags: rule.tagSelector ? Array.from(safe(rule.tagSelector) ?? []).map((e) => cleanText(e.textContent)).filter(Boolean) : undefined
  };
}

export function runAdapters(doc: Document, url: string, rules: SiteRule[]): AdapterResult | null {
  const custom = runSiteRule(doc, url, rules);
  if (custom && (custom.pages.length || custom.chapters.length)) return custom;
  for (const adapter of ADAPTERS) {
    try {
      const result = adapter.run(doc, url);
      if (result && (result.pages.length >= 1 || result.chapters.length >= 1)) return result;
    } catch {
      /* an adapter never breaks the scan */
    }
  }
  return null;
}
