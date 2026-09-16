// Turns the genre/tag links scraped from a site into clean Komik tags.

const JUNK = new Set([
  "home", "homepage", "read", "read now", "read online", "read more", "start reading", "continue reading", "latest",
  "latest chapter", "latest update", "latest updates", "popular", "new", "hot", "trending", "online", "free", "all",
  "more", "next", "prev", "previous", "genre", "genres", "tag", "tags", "category", "categories", "login", "sign in",
  "register", "bookmark", "bookmarks", "follow", "subscribe", "share", "comment", "comments", "download", "search",
  "menu", "list", "manga list", "comic list", "series", "chapter", "chapters", "episode", "episodes", "view all",
  "show more", "show less", "see all", "details", "info", "discord", "facebook", "twitter", "x", "reddit",
  "instagram", "patreon", "ko-fi", "privacy", "privacy policy", "terms", "dmca", "contact", "about", "faq",
  "random", "a-z", "az list", "top", "ranking", "completed list", "ongoing list", "unknown", "n/a", "none", "other"
]);

const KNOWN_CASE: Record<string, string> = {
  "sci-fi": "Sci-Fi", "scifi": "Sci-Fi", "bl": "BL", "gl": "GL", "yaoi": "Yaoi", "yuri": "Yuri", "shounen": "Shounen",
  "shoujo": "Shoujo", "seinen": "Seinen", "josei": "Josei", "isekai": "Isekai", "manhwa": "Manhwa", "manhua": "Manhua",
  "webtoon": "Webtoon", "4-koma": "4-Koma", "slice of life": "Slice of Life", "rpg": "RPG", "ai": "AI", "lgbtq": "LGBTQ",
  "lgbtq+": "LGBTQ+", "3d": "3D", "gore": "Gore", "one shot": "Oneshot", "oneshot": "Oneshot"
};

const SMALL_WORDS = new Set(["of", "and", "the", "in", "on", "a", "an", "to", "for", "with", "at", "by", "or"]);

export function titleCaseTag(tag: string): string {
  const lower = tag.toLowerCase();
  if (KNOWN_CASE[lower]) return KNOWN_CASE[lower];
  // Keep deliberate mixed case ("iPhone", "McDonald") as written.
  if (tag !== lower && tag !== tag.toUpperCase()) return tag;
  return lower
    .split(" ")
    .map((w, i) => (i > 0 && SMALL_WORDS.has(w) ? w : w.replace(/(^|[-/])(\p{L})/gu, (_m, sep: string, ch: string) => sep + ch.toUpperCase())))
    .join(" ");
}

export interface CleanTagOptions {
  clean: boolean;
  max: number;
  /** Words that are really the comic's own title or site name, never tags. */
  exclude?: string[];
}

/**
 * Gallery sites print how many comics a tag or artist has right after the name ("hana hook 48",
 * "romance 12.4K"). That count is not part of the name, so it is dropped. A number that is clearly part
 * of the name ("Iron Orchard 2", "Volume 3") is kept: only counts of two digits or more, or ones with a
 * K/M suffix, are treated as counts.
 */
export function stripTrailingCount(value: string): string {
  const trimmed = (value ?? "").trim();
  const withSuffix = /^(.*\S)\s+\d[\d,.]*\s*[km]\b\.?$/i.exec(trimmed);
  if (withSuffix) return withSuffix[1].trim();
  const plain = /^(.*\S)\s+(\d[\d,]*)$/.exec(trimmed);
  if (plain && plain[2].replace(/,/g, "").length >= 2) return plain[1].trim();
  return trimmed;
}

export function cleanTags(raw: string[], options: CleanTagOptions): string[] {
  const exclude = new Set((options.exclude ?? []).map((e) => e.trim().toLowerCase()).filter(Boolean));
  const out: string[] = [];
  const seen = new Set<string>();

  for (const item of raw) {
    // Counts go first: "Action (1,234)" is dropped, and "8,102" loses its comma so the list split below
    // doesn't tear the number in half.
    const item2 = (item ?? "").replace(/\s*\(\s*\d[\d,.]*\s*\)/g, "").replace(/(\d),(?=\d{3}\b)/g, "$1");
    for (const part of item2.split(/\s*[,;|\n]\s*/)) {
      let tag = part.replace(/[​-‍﻿]/g, "").replace(/\s+/g, " ").trim();
      if (!options.clean) {
        if (tag && tag.length <= 64 && !seen.has(tag.toLowerCase())) {
          seen.add(tag.toLowerCase());
          out.push(tag);
        }
        continue;
      }
      tag = stripTrailingCount(
        tag
          .replace(/^#+/, "")
          .replace(/\s*\(\s*\d[\d,.]*\s*\)\s*$/, "") // "Action (1,234)" counts
          .replace(/\s*[×x]\s*\d+$/, "")
          .replace(/^[\s\-–•·:]+|[\s\-–•·:,.]+$/g, "")
          .trim()
      );
      const key = tag.toLowerCase();
      if (tag.length < 2 || tag.length > 40) continue;
      if (JUNK.has(key) || exclude.has(key)) continue;
      if (/^\d+$/.test(tag) || /^https?:/i.test(tag) || /[<>{}]/.test(tag)) continue;
      if (/^(?:chapter|ch\.?|episode|ep\.?|vol\.?|volume)\s*\d/i.test(tag)) continue;
      if (tag.split(" ").length > 5) continue;

      const pretty = titleCaseTag(tag);
      const norm = key.replace(/[^\p{L}\p{N}]+/gu, "");
      const singular = norm.endsWith("s") ? norm.slice(0, -1) : norm;
      if (seen.has(norm) || seen.has(singular) || seen.has(`${norm}s`)) continue;
      seen.add(norm);
      out.push(pretty);
    }
  }
  return options.max > 0 ? out.slice(0, options.max) : out;
}

/** Splits "Jane Doe, John Roe & Kim" style credit lines into people. */
export function splitPeople(raw: string | string[]): string[] {
  const list = Array.isArray(raw) ? raw : [raw];
  const out: string[] = [];
  const seen = new Set<string>();
  for (const item of list) {
    // "hana hook 1,204" keeps its count in one piece while the line is split into people.
    for (const part of (item ?? "").replace(/(\d),(?=\d{3}\b)/g, "$1").split(/\s*(?:[,;|/\n]|\s&\s|\sand\s)\s*/i)) {
      // Artist links on gallery sites carry a count ("hana hook 48"), which is not part of the name.
      const name = stripTrailingCount(
        part.replace(/\s*\((?:story|art|author|artist|writer|illustrator)\)\s*$/i, "").replace(/\s*\(\s*\d[\d,.]*\s*\)\s*$/, "").replace(/\s+/g, " ").trim()
      );
      if (name.length < 2 || name.length > 60 || /^(?:updating|unknown|n\/a|none|-+)$/i.test(name)) continue;
      const key = name.toLowerCase();
      if (seen.has(key)) continue;
      seen.add(key);
      out.push(name);
    }
  }
  return out;
}
