// Parsing a page's HTML must never try to load anything from it. Even though a parsed document is inert
// and its scripts never run, the browser still looks ahead for resources to preload, which shows up as a
// wall of blocked-by-policy errors. Removing the tags that point at other files keeps that quiet, while
// inline scripts stay because readers keep their page lists in them.

const EXTERNAL_SCRIPT = /<script\b[^>]*\bsrc\s*=[^>]*>(?:[\s\S]*?<\/script\s*>)?/gi;
const LINKED_RESOURCE = /<link\b[^>]*>/gi;
const FRAMED_CONTENT = /<(iframe|frame|embed|object|video|audio|source|track)\b[^>]*>(?:[\s\S]*?<\/\1\s*>)?/gi;
const PRELOAD_META = /<meta\b[^>]*http-equiv\s*=\s*["']?(?:refresh|content-security-policy)["']?[^>]*>/gi;

/** Strips everything that would make the browser fetch another file. */
export function stripExternalResources(html: string): string {
  return html
    .replace(EXTERNAL_SCRIPT, "")
    .replace(LINKED_RESOURCE, "")
    .replace(FRAMED_CONTENT, "")
    .replace(PRELOAD_META, "");
}

/**
 * Parses a page fetched from a site, without the browser trying to load anything it mentions.
 * `<base href>` is rewritten to the page's own address so relative links still resolve.
 */
export function parsePageHtml(html: string, url: string): Document {
  const doc = new DOMParser().parseFromString(stripExternalResources(html), "text/html");
  try {
    const base = doc.querySelector("base") ?? doc.head.appendChild(doc.createElement("base"));
    base.setAttribute("href", new URL(base.getAttribute("href") ?? ".", url).href);
  } catch {
    /* a page without a head still parses fine */
  }
  return doc;
}
