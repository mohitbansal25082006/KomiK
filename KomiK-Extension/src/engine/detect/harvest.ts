// Collects every image a page could be showing as a comic page: <img> (including lazy-load attributes
// and srcset), <picture>, <noscript> fallbacks, CSS backgrounds, links to images and image URLs inside
// inline scripts / JSON. Works on a live page (with layout and natural sizes) and on a parsed document.
import { absoluteUrl, extOf, IMAGE_EXTENSIONS } from "@/shared/util";

export interface Candidate {
  url: string;
  width: number;
  height: number;
  order: number;
  /** Signature of the element's surroundings, used to group pages that sit in the same reader column. */
  container: string;
  source: "img" | "background" | "link" | "script";
  /** Was the <img> still showing a lazy-load placeholder? */
  lazy: boolean;
  element?: Element;
}

const LAZY_ATTRS = [
  "data-src", "data-lazy-src", "data-original", "data-url", "data-lazy", "data-echo", "data-img", "data-image",
  "data-full", "data-full-src", "data-hi-res", "data-high-res", "data-orig", "data-actualsrc", "data-cfsrc", "data-pagespeed-lazy-src",
  "data-srcset", "data-lazy-srcset"
];

const PLACEHOLDER = /(?:^data:image\/(?:gif|svg\+xml|png);base64,.{0,200}$)|blank\.(?:gif|png)|placeholder|loading\.(?:gif|svg|png)|lazy(?:load)?\.(?:gif|png|svg)|spacer\.gif|1x1\.|pixel\.gif|transparent\.(?:gif|png)/i;
export const NOT_A_PAGE = /(?:avatar|gravatar|logo|favicon|icon[-_.]|[-_/]icons?\/|sprite|emoji|smiley|badge|banner|advert|[-_/]ads?[-_/.]|adserver|doubleclick|pixel|tracking|analytics|button|social|share[-_]|facebook|twitter|discord|patreon|kofi|paypal|flag[-_]|rating|star[-_]|comment|profile)/i;

function bestFromSrcset(srcset: string | null, base: string): string | null {
  if (!srcset) return null;
  let best: { url: string; score: number } | null = null;
  for (const part of srcset.split(/,\s+(?=\S)/)) {
    const [rawUrl, descriptor] = part.trim().split(/\s+/);
    const url = absoluteUrl(rawUrl, base);
    if (!url) continue;
    const score = descriptor ? Number.parseFloat(descriptor) * (descriptor.endsWith("x") ? 1000 : 1) : 1;
    if (!best || score > best.score) best = { url, score };
  }
  return best?.url ?? null;
}

/** A short structural signature: tag + stable classes of the three closest ancestors. */
export function containerSignature(el: Element): string {
  const parts: string[] = [];
  let node: Element | null = el.parentElement;
  for (let depth = 0; node && depth < 3; depth++, node = node.parentElement) {
    const cls = Array.from(node.classList)
      .filter((c) => !/\d{3,}|active|lazy|loaded|visible|show|hidden|swiper-slide-(?:next|prev|active)/i.test(c))
      .slice(0, 3)
      .join(".");
    parts.push(`${node.tagName.toLowerCase()}${node.id && !/\d{3,}/.test(node.id) ? `#${node.id}` : ""}${cls ? `.${cls}` : ""}`);
  }
  return parts.join("<");
}

export function imageUrlFromElement(img: HTMLImageElement | Element, base: string): { url: string | null; lazy: boolean } {
  const src = img.getAttribute("src");
  const current = (img as HTMLImageElement).currentSrc || "";
  let lazyUrl: string | null = null;
  for (const attr of LAZY_ATTRS) {
    const value = img.getAttribute(attr);
    if (!value) continue;
    lazyUrl = attr.endsWith("srcset") ? bestFromSrcset(value, base) : absoluteUrl(value, base);
    if (lazyUrl) break;
  }
  const srcsetUrl = bestFromSrcset(img.getAttribute("srcset"), base);
  const srcUrl = absoluteUrl(current || src, base);
  const srcIsPlaceholder = !srcUrl || PLACEHOLDER.test(srcUrl);
  const url = lazyUrl || srcsetUrl || (srcIsPlaceholder ? null : srcUrl);
  return { url, lazy: !!lazyUrl && srcIsPlaceholder };
}

function sizeOf(el: Element, live: boolean): { width: number; height: number } {
  const img = el as HTMLImageElement;
  let width = 0;
  let height = 0;
  if (live && img.naturalWidth) {
    width = img.naturalWidth;
    height = img.naturalHeight;
  }
  if (!width) {
    width = Number.parseInt(el.getAttribute("width") ?? "", 10) || Number.parseInt(el.getAttribute("data-width") ?? "", 10) || 0;
    height = Number.parseInt(el.getAttribute("height") ?? "", 10) || Number.parseInt(el.getAttribute("data-height") ?? "", 10) || 0;
  }
  if (!width && live) {
    const rect = el.getBoundingClientRect();
    width = Math.round(rect.width);
    height = Math.round(rect.height);
  }
  return { width, height };
}

const SCRIPT_IMAGE_URL = new RegExp(String.raw`(?:https?:)?\\?/\\?/(?:[^"'\s<>()\\]|\\/)+?\.(?:${IMAGE_EXTENSIONS.join("|")})(?:\?[^"'\s<>\\]*)?`, "gi");

export function harvest(doc: Document, options: { live: boolean; baseUrl?: string }): Candidate[] {
  const base = options.baseUrl ?? doc.baseURI ?? doc.URL;
  const out: Candidate[] = [];
  let order = 0;
  const seen = new Set<string>();
  const push = (c: Omit<Candidate, "order">) => {
    if (!c.url || c.url.startsWith("blob:") || seen.has(c.url)) return;
    if (c.url.startsWith("data:") && c.url.length < 2000) return;
    seen.add(c.url);
    out.push({ ...c, order: order++ });
  };

  const roots: ParentNode[] = [doc];
  // Readers that render inside open shadow roots.
  if (options.live) {
    doc.querySelectorAll("*").forEach((el) => {
      if ((el as HTMLElement).shadowRoot) roots.push((el as HTMLElement).shadowRoot!);
    });
  }

  for (const root of roots) {
    root.querySelectorAll("img, image, amp-img").forEach((el) => {
      // Site chrome never holds pages; sidebars and comments are left to lose the clustering instead.
      if (el.closest("header, footer, nav, [role=navigation]")) return;
      const tag = el.tagName.toLowerCase();
      let url: string | null;
      let lazy = false;
      if (tag === "image") {
        url = absoluteUrl(el.getAttribute("href") ?? el.getAttribute("xlink:href"), base);
      } else {
        const r = imageUrlFromElement(el, base);
        url = r.url;
        lazy = r.lazy;
        const picture = el.parentElement?.tagName.toLowerCase() === "picture" ? el.parentElement : null;
        if (picture) {
          const sources = Array.from(picture.querySelectorAll("source"));
          const jpg = sources.find((s) => /jpe?g|png|webp/i.test(s.getAttribute("type") ?? "")) ?? sources[0];
          const fromSource = jpg ? bestFromSrcset(jpg.getAttribute("srcset") ?? jpg.getAttribute("data-srcset"), base) : null;
          if (fromSource && !url) url = fromSource;
        }
      }
      if (!url) return;
      const { width, height } = sizeOf(el, options.live);
      push({ url, width, height, container: containerSignature(el), source: "img", lazy, element: el });
    });
  }

  // <noscript><img src=…></noscript> fallbacks used by lazy loaders.
  doc.querySelectorAll("noscript").forEach((ns) => {
    const html = ns.textContent ?? "";
    for (const m of html.matchAll(/<img[^>]+(?:data-src|src)=["']([^"']+)["']/gi)) {
      const url = absoluteUrl(m[1], base);
      if (url) push({ url, width: 0, height: 0, container: containerSignature(ns), source: "img", lazy: false });
    }
  });

  // Links straight to images ("view full size").
  doc.querySelectorAll("a[href]").forEach((a) => {
    const url = absoluteUrl(a.getAttribute("href"), base);
    if (!url || !IMAGE_EXTENSIONS.includes(extOf(url))) return;
    if (a.closest("header, footer, nav")) return;
    push({ url, width: 0, height: 0, container: containerSignature(a), source: "link", lazy: false, element: a });
  });

  // Large CSS backgrounds (canvas-free readers sometimes paint pages as backgrounds).
  if (options.live) {
    const view = doc.defaultView;
    doc.querySelectorAll("div, section, figure, span, a, li").forEach((el) => {
      const rect = (el as HTMLElement).getBoundingClientRect();
      if (rect.width < 250 || rect.height < 250) return;
      const style = view?.getComputedStyle(el);
      const bg = style?.backgroundImage;
      if (!bg || bg === "none") return;
      const m = /url\(["']?([^"')]+)["']?\)/.exec(bg);
      const url = m ? absoluteUrl(m[1], base) : null;
      if (url) push({ url, width: Math.round(rect.width), height: Math.round(rect.height), container: containerSignature(el), source: "background", lazy: false, element: el });
    });
  }

  // Image URLs inside inline scripts and JSON blobs (JS-driven readers).
  doc.querySelectorAll("script:not([src])").forEach((script) => {
    const text = script.textContent ?? "";
    if (text.length < 40 || text.length > 3_000_000) return;
    const type = script.getAttribute("type") ?? "";
    if (/ld\+json/i.test(type)) return;
    for (const m of text.matchAll(SCRIPT_IMAGE_URL)) {
      const raw = m[0].replace(/\\\//g, "/").replace(/\\u002F/gi, "/");
      const url = absoluteUrl(raw.startsWith("//") ? `https:${raw}` : raw, base);
      if (url) push({ url, width: 0, height: 0, container: "script", source: "script", lazy: false });
    }
  });

  return out;
}
