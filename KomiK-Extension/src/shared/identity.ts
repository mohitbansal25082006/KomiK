// A TypeScript port of the number-reading rules in Komik's Helpers/ComicIdentityParser.cs, so the names
// the extension writes are read back by the app exactly as intended (series + volume + chapter/issue).

import { stripDates } from "./dates";
import { cleanText } from "./util";

export type NumberKind = "chapter" | "issue" | "episode" | "volume" | "none";

export interface ParsedIdentity {
  series: string;
  volume: number | null;
  number: number | null;
  kind: NumberKind;
  year: number | null;
  /** Text after the number ("Ch. 12 - The Fight" → "The Fight"). */
  subtitle: string;
}

const N = String.raw`(\d+(?:\.\d+)?)`;
const VOLUME_CHAPTER = new RegExp(String.raw`\b(?:vol(?:ume)?|v)\.?\s*${N}\b[\s\-–_:,.]*(?:ch(?:ap(?:ter)?)?|c|episode|ep)\.?\s*${N}\b`, "i");
const VOLUME_ISSUE = new RegExp(String.raw`\b(?:vol(?:ume)?|v)\.?\s*${N}\b[\s\-–_:,.]*(?:#|no\.?\s*|issue\s*)\s*${N}`, "i");
const CHAPTER = new RegExp(String.raw`(?:\b(?:chapter|chap|ch)\.?\s*|\bc(?=\d{2,4}\b))${N}\b`, "i");
const EPISODE = new RegExp(String.raw`\b(?:episode|ep)\.?\s*${N}\b`, "i");
const ISSUE = new RegExp(String.raw`(?:#|\bno\.\s*|\bissue\s*)\s*${N}`, "i");
const VOLUME = new RegExp(String.raw`\b(?:vol(?:ume)?|tome|book|v)\.?\s*${N}\b`, "i");
const TRAILING = new RegExp(String.raw`^(.*?\S)[\s\-–_]+(\d{1,4}(?:\.\d+)?)\s*$`);
const YEAR = /\((19|20)\d{2}\)|\b(19|20)\d{2}\b(?=\s*$)/;
const JAPANESE = /第?\s*(\d+)\s*(巻|話)/;

const num = (s: string | undefined) => (s === undefined ? null : Number.parseFloat(s));

export function parseIdentity(raw: string): ParsedIdentity {
  let text = cleanTitleNoise((raw ?? "").replace(/_/g, " "));
  let year: number | null = null;
  const y = YEAR.exec(text);
  if (y) {
    year = Number.parseInt(y[0].replace(/[()]/g, ""), 10);
    text = text.replace(y[0], " ").trim();
  }

  const tryMatch = (re: RegExp) => {
    const m = re.exec(text);
    return m && m.index !== undefined ? m : null;
  };

  let series = text;
  let volume: number | null = null;
  let number: number | null = null;
  let kind: NumberKind = "none";
  let subtitle = "";

  const finish = (m: RegExpExecArray) => {
    series = text.slice(0, m.index);
    subtitle = text.slice(m.index + m[0].length);
  };

  let m: RegExpExecArray | null;
  if ((m = tryMatch(VOLUME_CHAPTER))) {
    volume = num(m[1]); number = num(m[2]); kind = "chapter"; finish(m);
  } else if ((m = tryMatch(VOLUME_ISSUE))) {
    volume = num(m[1]); number = num(m[2]); kind = "issue"; finish(m);
  } else if ((m = tryMatch(CHAPTER))) {
    number = num(m[1]); kind = "chapter"; finish(m);
    const v = VOLUME.exec(series);
    if (v) { volume = num(v[1]); series = series.slice(0, v.index); }
  } else if ((m = tryMatch(EPISODE))) {
    number = num(m[1]); kind = "episode"; finish(m);
  } else if ((m = tryMatch(ISSUE))) {
    number = num(m[1]); kind = "issue"; finish(m);
    const v = VOLUME.exec(series);
    if (v) { volume = num(v[1]); series = series.slice(0, v.index); }
  } else if ((m = tryMatch(VOLUME))) {
    volume = num(m[1]); kind = "volume"; finish(m);
  } else if ((m = tryMatch(JAPANESE))) {
    number = num(m[1]); kind = m[2] === "巻" ? "volume" : "chapter";
    if (kind === "volume") { volume = number; number = null; }
    finish(m);
  } else if ((m = TRAILING.exec(text))) {
    series = m[1]; number = num(m[2]); kind = "issue";
  }

  return {
    series: tidySeries(series),
    volume,
    number,
    kind,
    year,
    subtitle: subtitle.replace(/^[\s:\-–—_.,|]+/, "").replace(/[\s\-–—_|]+$/, "").trim()
  };
}

/** Removes release dates and reader-site noise: "Read …", "… Online", "… Manga Free", "| SiteName". */
export function cleanTitleNoise(raw: string, siteName = ""): string {
  let t = stripDates(cleanText(raw));
  if (siteName) {
    const escaped = siteName.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    t = t.replace(new RegExp(`\\s*[-–—|:•·»]\\s*${escaped}\\s*$`, "i"), "").replace(new RegExp(`^${escaped}\\s*[-–—|:•·»]\\s*`, "i"), "");
  }
  t = t
    .replace(/^\s*read\s+(?=\S)/i, "")
    .replace(/\s+(?:manga|manhwa|manhua|comic|webtoon)?\s*(?:online|free|for free|online free|online for free|in english|english)\s*(?:free)?\s*$/i, "")
    .replace(/\s*[-–—|]\s*(?:read\s+)?(?:manga|comics?|webtoons?)\s+online.*$/i, "")
    .replace(/\s*[|•·»]\s*[^|•·»]{2,40}$/, (s) => (/\d/.test(s) ? s : ""));
  return t.replace(/\s+/g, " ").trim();
}

function tidySeries(value: string): string {
  return value
    .replace(/[_]+/g, " ")
    .replace(/\s*[-–—:,|#(\[]+\s*$/, "")
    .replace(/\s+/g, " ")
    .trim();
}

export function formatNumber(value: number | null | undefined): string {
  if (value === null || value === undefined || !Number.isFinite(value)) return "";
  return String(Math.round(value * 1000) / 1000);
}

/**
 * The display title Komik understands: "Series Vol. 2 Ch. 15", "Series #7", "Series Episode 3",
 * "Series Vol. 4". An optional chapter title is appended after " - ".
 */
export function composeTitle(parts: { series: string; volume?: string; number?: string; kind?: NumberKind; chapterTitle?: string; includeChapterTitle?: boolean }): string {
  const series = (parts.series ?? "").trim();
  const bits: string[] = [];
  if (series) bits.push(series);
  if (parts.volume) bits.push(`Vol. ${parts.volume}`);
  if (parts.number) {
    switch (parts.kind) {
      case "issue": bits.push(`#${parts.number}`); break;
      case "episode": bits.push(`Episode ${parts.number}`); break;
      case "volume": if (!parts.volume) bits.push(`Vol. ${parts.number}`); break;
      default: bits.push(`Ch. ${parts.number}`);
    }
  }
  let title = bits.join(" ");
  const sub = (parts.chapterTitle ?? "").trim();
  if (parts.includeChapterTitle && sub && !title.toLowerCase().includes(sub.toLowerCase())) {
    title = title ? `${title} - ${sub}` : sub;
  }
  return title.trim();
}
