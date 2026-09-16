// Reads a comic's details from its web page: schema.org JSON-LD, OpenGraph / meta tags, labelled fields
// ("Author:", "Genres:", "Released:"), tag and genre links, breadcrumbs and the page title.
import { cleanTitleNoise, composeTitle, formatNumber, parseIdentity } from "@/shared/identity";
import { splitPeople } from "@/shared/tags";
import type { ComicMeta } from "@/shared/types";
import { absoluteUrl, cleanText, emptyMeta, hostOf } from "@/shared/util";

type Json = Record<string, unknown>;

function asArray<T>(v: T | T[] | undefined | null): T[] {
  return v === undefined || v === null ? [] : Array.isArray(v) ? v : [v];
}

function nameOf(v: unknown): string {
  if (!v) return "";
  if (typeof v === "string") return v;
  if (typeof v === "object") return String((v as Json).name ?? (v as Json)["@value"] ?? "");
  return "";
}

function readJsonLd(doc: Document): Json[] {
  const out: Json[] = [];
  doc.querySelectorAll('script[type="application/ld+json"]').forEach((s) => {
    try {
      const data = JSON.parse(s.textContent ?? "");
      const walk = (node: unknown) => {
        if (!node || typeof node !== "object") return;
        if (Array.isArray(node)) return node.forEach(walk);
        const obj = node as Json;
        out.push(obj);
        if (obj["@graph"]) walk(obj["@graph"]);
      };
      walk(data);
    } catch {
      /* invalid JSON-LD is common; ignore */
    }
  });
  return out;
}

const COMIC_TYPES = /ComicSeries|ComicIssue|ComicStory|Book|BookSeries|CreativeWorkSeries|Periodical|PublicationIssue|Chapter|CreativeWork/i;

function meta(doc: Document, ...names: string[]): string {
  for (const n of names) {
    const el = doc.querySelector(`meta[property="${n}"], meta[name="${n}"], meta[itemprop="${n}"]`);
    const v = cleanText(el?.getAttribute("content"));
    if (v) return v;
  }
  return "";
}

type Field = "writers" | "artists" | "authors" | "genres" | "tags" | "publisher" | "released" | "status" | "summary" | "type" | "series" | "language" | "alt";

const LABELS: Array<[Field, RegExp]> = [
  ["authors", /^(?:author|authors|autor|auteur|mangaka|creator|creators|created by|by)$/i],
  ["writers", /^(?:writer|writers|story|story by|script|written by|scenario|original story)$/i],
  ["artists", /^(?:artist|artists|art|art by|illustrator|illustrators|illustration|drawn by|penciller|pencils)$/i],
  ["genres", /^(?:genre|genres|género|géneros|genre\(s\)|categories|category|kategori)$/i],
  ["tags", /^(?:tags?|themes?|keywords?|demographic)$/i],
  ["publisher", /^(?:publisher|publishers|serialization|serialized in|magazine|imprint|studio)$/i],
  ["released", /^(?:released|release|release date|year|published|publication|date|first published|start date|posted on|updated on)$/i],
  ["status", /^(?:status|publication status|scan status|comic status)$/i],
  ["type", /^(?:type|format|comic type)$/i],
  ["alt", /^(?:alternative|alternative titles?|alt(?:ernate)? names?|other names?|synonyms?|associated names?)$/i],
  ["language", /^(?:language|lang|original language)$/i]
];

function labelField(text: string): Field | null {
  const t = cleanText(text).replace(/[:：]\s*$/, "").replace(/\s*\(s\)$/, "").trim();
  if (!t || t.length > 30) return null;
  for (const [field, re] of LABELS) if (re.test(t)) return field;
  return null;
}

interface LabelValue {
  text: string;
  links: string[];
}

/** Finds "Label: value" pairs in definition lists, tables, info boxes and inline <b>Label:</b> text. */
function readLabelledFields(root: ParentNode): Map<Field, LabelValue> {
  const found = new Map<Field, LabelValue>();
  const set = (field: Field, value: LabelValue) => {
    if (!value.text && !value.links.length) return;
    if (!found.has(field)) found.set(field, value);
  };
  const valueOf = (el: Element | null | undefined, labelText = ""): LabelValue => {
    if (!el) return { text: "", links: [] };
    const links = Array.from(el.querySelectorAll("a")).map((a) => cleanText(a.textContent)).filter(Boolean);
    let text = cleanText(el.textContent);
    if (labelText && text.toLowerCase().startsWith(labelText.toLowerCase())) text = text.slice(labelText.length);
    return { text: text.replace(/^[\s:：\-–]+/, "").trim(), links };
  };

  root.querySelectorAll("dt, th").forEach((label) => {
    const field = labelField(label.textContent ?? "");
    if (!field) return;
    const value = label.tagName === "DT" ? label.nextElementSibling : label.nextElementSibling ?? label.parentElement?.querySelector("td");
    set(field, valueOf(value));
  });

  // Innermost elements first, so "<p><b>Status:</b> Ongoing</p>" wins over the whole info box around it.
  Array.from(root.querySelectorAll("b, strong, h4, h5, h6, span, label, div, p, li")).reverse().forEach((el) => {
    if (el.children.length > 4) return;
    const own = cleanText(Array.from(el.childNodes).filter((n) => n.nodeType === 3).map((n) => n.textContent).join(" ")) || cleanText(el.firstElementChild?.textContent ?? "");
    const whole = cleanText(el.textContent);
    // "Author: Jane Doe" inside one element
    const inline = /^([^:：]{2,28})[:：]\s*(.+)$/.exec(whole);
    if (inline && el.children.length <= 4 && whole.length < 400) {
      const field = labelField(inline[1]);
      if (field) {
        const links = Array.from(el.querySelectorAll("a")).map((a) => cleanText(a.textContent)).filter(Boolean);
        set(field, { text: inline[2], links });
        return;
      }
    }
    const field = labelField(own) ?? (el.children.length === 0 ? labelField(whole) : null);
    if (!field) return;
    // An inline label ("<p><b>Genres:</b> <a>Action</a>, <a>Drama</a></p>") owns the rest of its line;
    // a block label (heading, div) is followed by its value in the next element.
    const parent = el.parentElement;
    const inlineLabel = /^(B|STRONG|SPAN|LABEL|EM|I)$/.test(el.tagName) && parent && cleanText(parent.textContent).length > whole.length;
    const sibling = el.nextElementSibling;
    if (inlineLabel) set(field, valueOf(parent, whole));
    else if (sibling && cleanText(sibling.textContent)) set(field, valueOf(sibling));
    else if (parent) set(field, valueOf(parent, whole));
  });
  return found;
}

function tagLinks(doc: Document): string[] {
  const out: string[] = [];
  doc.querySelectorAll('a[rel~="tag"], a[href*="/genre/"], a[href*="/genres/"], a[href*="/genre?"], a[href*="genre="], a[href*="/tag/"], a[href*="/tags/"], a[href*="/category/"], a[href*="/theme/"]').forEach((a) => {
    if (a.closest("header, footer, nav, aside, [class*=menu], [class*=sidebar], [class*=widget], [id*=menu]")) return;
    const text = cleanText(a.textContent);
    if (text && text.length <= 40) out.push(text);
  });
  return out;
}

function dateParts(value: string): { year: string; month: string; day: string } {
  const iso = /((?:19|20)\d{2})-(\d{1,2})(?:-(\d{1,2}))?/.exec(value);
  if (iso) return { year: iso[1], month: String(+iso[2]), day: iso[3] ? String(+iso[3]) : "" };
  const year = /\b((?:19|20)\d{2})\b/.exec(value);
  if (!year) return { year: "", month: "", day: "" };
  const parsed = Date.parse(value);
  if (!Number.isNaN(parsed) && /[a-z]{3}/i.test(value)) {
    const d = new Date(parsed);
    return { year: String(d.getFullYear()), month: String(d.getMonth() + 1), day: /\b\d{1,2}\b/.test(value.replace(year[1], "")) ? String(d.getDate()) : "" };
  }
  return { year: year[1], month: "", day: "" };
}

function breadcrumbSeries(doc: Document, jsonLd: Json[]): string {
  const list = jsonLd.find((j) => /BreadcrumbList/i.test(String(j["@type"])));
  if (list) {
    const items = asArray(list.itemListElement as Json[]).map((i) => nameOf(i.name ? i : (i.item as Json)));
    if (items.length >= 3) return cleanText(items[items.length - 2]);
  }
  const crumbs = Array.from(doc.querySelectorAll('[class*=breadcrumb] a, nav[aria-label*=readcrumb] a, [itemtype*=BreadcrumbList] a')).map((a) => cleanText(a.textContent)).filter(Boolean);
  return crumbs.length >= 2 ? crumbs[crumbs.length - 1] : "";
}

export interface ScrapeOptions {
  url: string;
  /** Chapter number read from the reader's URL, when the page itself doesn't say. */
  isSeriesPage?: boolean;
}

export function scrapeMetadata(doc: Document, options: ScrapeOptions): ComicMeta {
  const m = emptyMeta();
  const url = options.url;
  const jsonLd = readJsonLd(doc);
  const work = jsonLd.find((j) => COMIC_TYPES.test(asArray(j["@type"] as string | string[]).join(" "))) ?? {};
  const partOf = (work.isPartOf ?? work.partOfSeries) as Json | undefined;

  const siteName = meta(doc, "og:site_name", "application-name") || hostOf(url);
  m.site = siteName;
  m.web = absoluteUrl(doc.querySelector('link[rel="canonical"]')?.getAttribute("href"), url) ?? url;

  // ── Title & series ──
  const h1 = cleanText(doc.querySelector("h1")?.textContent);
  const rawTitle = cleanText(nameOf(work.name) || work.headline as string) || meta(doc, "og:title", "twitter:title") || h1 || cleanText(doc.title);
  const pageTitle = cleanTitleNoise(rawTitle, siteName);
  const labelled = readLabelledFields(doc.querySelector("main, article, [class*=info], [class*=detail], [class*=summary], [id*=info], body") ?? doc);

  let series = cleanText(nameOf(partOf)) || breadcrumbSeries(doc, jsonLd);
  const id = parseIdentity(pageTitle);
  const urlId = parseIdentity(decodeURIComponent(new URL(url).pathname.replace(/[-_/]+/g, " ")));

  if (!series || series.toLowerCase() === siteName.toLowerCase()) series = id.series || pageTitle;
  series = cleanTitleNoise(series, siteName);
  // Series pages: the title is the series itself.
  if (options.isSeriesPage) series = cleanTitleNoise(h1 || pageTitle, siteName) || series;

  const number = id.number ?? (options.isSeriesPage ? null : urlId.kind === "chapter" || urlId.kind === "episode" ? urlId.number : null);
  const volume = id.volume ?? (options.isSeriesPage ? null : urlId.volume);
  m.series = series.replace(/\s+(?:chapter|ch\.?|episode|ep\.?)\s*$/i, "").trim();
  m.number = formatNumber(number);
  m.volume = formatNumber(volume);
  m.numberKind = number !== null ? (id.kind === "none" ? urlId.kind : id.kind) : volume !== null ? "volume" : "none";
  if (m.numberKind === "volume" && number !== null) m.numberKind = "chapter";
  m.chapterTitle = id.subtitle && id.subtitle.toLowerCase() !== m.series.toLowerCase() ? id.subtitle : "";
  m.title = options.isSeriesPage ? m.series : composeTitle({ series: m.series, volume: m.volume, number: m.number, kind: m.numberKind }) || pageTitle;

  // ── Summary ──
  m.summary =
    cleanText(work.description as string) ||
    labelled.get("summary")?.text ||
    cleanText(doc.querySelector('[class*=summary] p, [class*=synopsis], [class*=description] p, [itemprop=description], #summary, .summary__content, .entry-content[itemprop=description]')?.textContent) ||
    meta(doc, "og:description", "description", "twitter:description");
  if (m.summary.length > 4000) m.summary = `${m.summary.slice(0, 3990)}…`;

  // ── Credits ──
  const ldAuthors = asArray(work.author as unknown[]).map(nameOf).concat(asArray(work.creator as unknown[]).map(nameOf));
  const ldArtists = asArray(work.illustrator as unknown[]).map(nameOf).concat(asArray(work.artist as unknown[]).map(nameOf), asArray(work.penciler as unknown[]).map(nameOf));
  const pick = (f: Field) => {
    const v = labelled.get(f);
    return v ? (v.links.length ? v.links : [v.text]) : [];
  };
  m.writers = splitPeople([...ldAuthors, ...pick("writers"), ...pick("authors")]);
  m.artists = splitPeople([...ldArtists, ...pick("artists")]);
  if (!m.writers.length) m.writers = splitPeople(meta(doc, "author", "book:author", "article:author").replace(/^https?:.*$/, ""));

  m.publisher = cleanText(nameOf(work.publisher)) || labelled.get("publisher")?.links[0] || labelled.get("publisher")?.text || "";
  if (m.publisher.length > 80) m.publisher = "";

  // ── Dates ──
  const released = String(work.datePublished ?? work.dateCreated ?? "") || labelled.get("released")?.text || meta(doc, "article:published_time", "og:published_time", "datePublished");
  Object.assign(m, dateParts(released));

  // ── Tags & genres ──
  const ldGenres = asArray(work.genre as unknown[]).map(nameOf);
  const ldKeywords = typeof work.keywords === "string" ? (work.keywords as string).split(",") : asArray(work.keywords as unknown[]).map(nameOf);
  m.genres = [...ldGenres, ...pick("genres")].flatMap((g) => g.split(/\s*,\s*/)).map(cleanText).filter(Boolean);
  const keywordMeta = meta(doc, "keywords");
  const keywordList = keywordMeta && keywordMeta.split(",").length <= 15 ? keywordMeta.split(",") : [];
  m.tags = [...ldKeywords, ...pick("tags"), ...tagLinks(doc), ...keywordList].map(cleanText).filter(Boolean);

  m.status = labelled.get("status")?.text.slice(0, 40) ?? "";
  m.language = (doc.documentElement.getAttribute("lang") || meta(doc, "og:locale") || String(work.inLanguage ?? "")).split(/[-_]/)[0].toLowerCase().slice(0, 3);
  m.coverUrl = absoluteUrl(nameOf(work.image) || (asArray(work.image as unknown[])[0] as string) || meta(doc, "og:image", "twitter:image"), url) ?? "";
  m.ageRating = String(work.contentRating ?? "").slice(0, 30);

  // ── Manga reading direction ──
  const typeText = `${labelled.get("type")?.text ?? ""} ${m.genres.join(" ")} ${m.tags.join(" ")} ${siteName} ${hostOf(url)}`.toLowerCase();
  if (/manhwa|webtoon|manhua|long strip/.test(typeText)) m.manga = "Yes";
  else if (/\bmanga\b|mangaka|shounen|shonen|shoujo|seinen|josei/.test(typeText) || m.language === "ja") m.manga = "YesAndRightToLeft";

  return m;
}
