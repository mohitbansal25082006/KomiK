// Gallery layouts: one page showing every page as a small preview, each linking to a reader page
// ("…/7/" or "…?page=7"). The previews give the page order and count; the full-size images come from
// the quality rules, with the reader page as a fallback.
import type { PageRef } from "@/shared/types";
import { absoluteUrl, uniqueBy } from "@/shared/util";
import { imageUrlFromElement } from "./harvest";
import { fullSizeCandidates, looksLikePreview } from "./quality";

const PAGE_NUMBER_PATTERNS = [
  /\/(\d{1,4})\/?$/, // …/gallery/123/7/
  /[?&](?:page|p|pg|i|image)=(\d{1,4})\b/i, // …?page=7
  /[-_/](?:page|p|pg)[-_]?(\d{1,4})(?:\.[a-z]{2,4})?$/i // …/page-7 or …/p_7.html
];

function pageNumberOf(href: string): number | null {
  for (const re of PAGE_NUMBER_PATTERNS) {
    const m = re.exec(href);
    if (m) {
      const n = Number.parseInt(m[1], 10);
      if (n >= 1 && n <= 5000) return n;
    }
  }
  return null;
}

/** Everything before the page number, so links of one gallery group together. */
function groupKey(href: string): string {
  return href
    .replace(/\/(\d{1,4})\/?$/, "/#/")
    .replace(/([?&](?:page|p|pg|i|image)=)\d{1,4}\b/i, "$1#")
    .replace(/([-_/](?:page|p|pg)[-_]?)\d{1,4}(\.[a-z]{2,4})?$/i, "$1#$2");
}

export interface GalleryResult {
  pages: PageRef[];
  /** True when the previews had to be guessed up to full size (the engine verifies each one). */
  fromPreviews: boolean;
}

/**
 * Finds a grid of numbered page links. Works whether or not the thumbnails are images, and keeps the
 * reader URL on each page so the downloader can fall back to reading that page for the real image.
 */
export function findGallery(doc: Document, baseUrl: string): GalleryResult | null {
  const groups = new Map<string, Map<number, { href: string; thumb: string | null }>>();

  doc.querySelectorAll("a[href]").forEach((a) => {
    if (a.closest("header, footer, nav, [role=navigation]")) return;
    const href = absoluteUrl(a.getAttribute("href"), baseUrl);
    if (!href) return;
    const number = pageNumberOf(href);
    if (number === null) return;

    const img = a.querySelector("img") ?? (a.firstElementChild?.tagName === "IMG" ? a.firstElementChild : null);
    const thumb = img ? imageUrlFromElement(img, baseUrl).url : null;
    const key = groupKey(href);
    const group = groups.get(key) ?? new Map();
    // The first link to a page wins (galleries often repeat the first page in a cover block).
    if (!group.has(number)) group.set(number, { href, thumb });
    groups.set(key, group);
  });

  let best: Map<number, { href: string; thumb: string | null }> | null = null;
  for (const group of groups.values()) {
    const withThumbs = Array.from(group.values()).filter((g) => g.thumb).length;
    const bestThumbs = best ? Array.from(best.values()).filter((g) => g.thumb).length : -1;
    if (group.size < 3) continue;
    if (!best || withThumbs > bestThumbs || (withThumbs === bestThumbs && group.size > best.size)) best = group;
  }
  if (!best) return null;

  const numbers = Array.from(best.keys()).sort((a, b) => a - b);
  // A real page list runs 1..n with few gaps; menus and pagers don't.
  const span = numbers[numbers.length - 1] - numbers[0] + 1;
  if (numbers.length < 3 || numbers.length / span < 0.8) return null;

  let fromPreviews = false;
  const pages: PageRef[] = numbers.map((n) => {
    const entry = best!.get(n)!;
    const thumb = entry.thumb;
    if (!thumb) return { url: "", pageUrl: entry.href, referer: baseUrl, source: "pagination" as const };
    const preview = looksLikePreview(thumb);
    if (preview) fromPreviews = true;
    return {
      url: preview ? fullSizeCandidates(thumb)[0] : thumb,
      candidates: preview ? fullSizeCandidates(thumb) : undefined,
      // The grid shows the preview the site already loaded; the guesses are only checked when downloading.
      thumbUrl: thumb,
      pageUrl: entry.href,
      referer: baseUrl,
      source: "gallery" as const
    };
  });

  // Pages without a preview still work: the engine opens their reader page.
  return { pages: uniqueBy(pages, (p) => p.pageUrl ?? p.url), fromPreviews };
}

/**
 * On a reader page showing a single page, the biggest image is the page itself. Used when a gallery
 * link has to be resolved one page at a time.
 */
export function mainImageOf(doc: Document, baseUrl: string): string | null {
  let best: { url: string; area: number } | null = null;
  doc.querySelectorAll("img").forEach((img) => {
    if (img.closest("header, footer, nav")) return;
    const { url } = imageUrlFromElement(img, baseUrl);
    if (!url || url.startsWith("data:")) return;
    const width = Number.parseInt(img.getAttribute("width") ?? "", 10) || 0;
    const height = Number.parseInt(img.getAttribute("height") ?? "", 10) || 0;
    // Prefer an explicit size; otherwise trust the reader container and take the first big image.
    const inReader = !!img.closest("#image-container, .image-container, #img, #page, .page, .reader, [class*=read], [id*=read], main, article, section");
    const area = width * height || (inReader ? 1 : 0);
    if (!best || area > best.area) best = { url, area };
  });
  return best ? (best as { url: string }).url : null;
}
