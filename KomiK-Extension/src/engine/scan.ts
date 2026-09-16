// Runs every detector on a document and merges the results into one DetectResult.
import { composeTitle } from "@/shared/identity";
import type { Confidence, DetectResult, PageRef, SiteRule } from "@/shared/types";
import { cleanText, hostOf, uniqueBy } from "@/shared/util";
import { runAdapters } from "./adapters";
import { findChapters, findPagination } from "./detect/chapters";
import { clusterPages, fillSequenceGaps } from "./detect/cluster";
import { findGallery } from "./detect/gallery";
import { harvest } from "./detect/harvest";
import { fullSizeCandidates, looksLikePreview } from "./detect/quality";
import { scrapeMetadata } from "./metadata/scrape";

export interface ScanOptions {
  url: string;
  live: boolean;
  rules: SiteRule[];
  minPageWidth: number;
  /** Image URLs the browser loaded in this tab (from the background's network observer). */
  networkImages?: Array<{ url: string; size?: number }>;
}

export function scanDocument(doc: Document, options: ScanOptions): DetectResult {
  const { url } = options;
  const adapter = runAdapters(doc, url, options.rules);
  const candidates = harvest(doc, { live: options.live, baseUrl: url });
  const cluster = clusterPages(candidates, { minWidth: options.minPageWidth });

  let pages: PageRef[] = [];
  let confidence: Confidence = "none";
  // An adapter that only found the chapter list still names the layout; pages then come from the scanner.
  let adapterName = adapter?.pages.length || adapter?.chapters.length ? adapter.name : "Universal scanner";

  // Galleries list every page as a small preview linking to its own reader page. That list is exact,
  // so it beats a cluster of thumbnails, and each page keeps full-size guesses plus its reader page.
  const gallery = findGallery(doc, url);

  if (adapter && adapter.pages.length >= 1) {
    pages = adapter.pages;
    confidence = adapter.pages.length >= 2 ? "high" : "medium";
  } else if (gallery && gallery.pages.length >= 3 && gallery.pages.length >= (cluster?.candidates.length ?? 0)) {
    pages = gallery.pages;
    confidence = gallery.pages.length >= 5 ? "high" : "medium";
    adapterName = gallery.fromPreviews ? "Gallery (full size)" : "Gallery";
  } else if (cluster) {
    const urls = fillSequenceGaps(cluster.candidates.map((c) => c.url));
    const byUrl = new Map(cluster.candidates.map((c) => [c.url, c]));
    pages = urls.map((u) => {
      const c = byUrl.get(u);
      return {
        url: u,
        width: c?.width || undefined,
        height: c?.height || undefined,
        referer: url,
        source: c ? (c.source === "background" ? "background" : c.source) : "sequence",
        // Readers that serve resized copies ("?w=800", "/thumbs/") still have the full page nearby.
        candidates: looksLikePreview(u) ? fullSizeCandidates(u) : undefined,
        thumbUrl: looksLikePreview(u) ? u : undefined
      };
    });
    confidence = cluster.confidence;
  }

  // Network-observed images from the same folder as the chosen pages (readers that swap <img> src).
  if (options.networkImages?.length && pages.length >= 2) {
    const folder = (u: string) => u.replace(/[^/]*$/, "");
    const folders = new Set(pages.map((p) => folder(p.url)));
    const have = new Set(pages.map((p) => p.url));
    const extra = options.networkImages.filter((n) => folders.has(folder(n.url)) && !have.has(n.url));
    if (extra.length && extra.length <= pages.length * 2) {
      pages = [...pages, ...extra.map((n) => ({ url: n.url, referer: url, source: "network" as const }))];
    }
  }
  // Gallery pages without a preview have no image URL yet, only their reader page.
  pages = uniqueBy(pages, (p) => p.url || p.pageUrl || "");

  const chapters = adapter?.chapters.length ? adapter.chapters : findChapters(doc, url, url);
  const pagination = pages.length <= 2 ? findPagination(doc, url, url) : [];
  const isSeriesPage = adapter?.isSeriesPage ?? (chapters.length >= 3 && pages.length < 3);

  const meta = scrapeMetadata(doc, { url, isSeriesPage });
  if (adapter) {
    if (adapter.series) meta.series = adapter.series;
    if (adapter.genres?.length) meta.genres = adapter.genres;
    if (adapter.tags?.length) meta.tags = [...adapter.tags, ...meta.tags];
    if (adapter.writers?.length) meta.writers = adapter.writers;
    if (adapter.artists?.length) meta.artists = adapter.artists;
    if (adapter.summary) meta.summary = adapter.summary;
    if (adapter.cover) meta.coverUrl = adapter.cover;
    if (adapter.title) meta.title = adapter.title;
    else if (adapter.series) meta.title = isSeriesPage ? adapter.series : composeTitle({ series: adapter.series, volume: meta.volume, number: meta.number, kind: meta.numberKind }) || meta.title;
  }
  if (!meta.coverUrl && pages[0]) meta.coverUrl = pages[0].url;

  const lazyPending = cluster ? cluster.candidates.filter((c) => c.lazy).length : 0;
  if (!pages.length && chapters.length) confidence = "medium";

  return {
    url,
    pageTitle: cleanText(doc.title),
    site: meta.site || hostOf(url),
    adapter: adapterName,
    confidence,
    pages,
    chapters,
    meta,
    pagination,
    lazyPending,
    scannedAt: Date.now(),
    isSeriesPage
  };
}
