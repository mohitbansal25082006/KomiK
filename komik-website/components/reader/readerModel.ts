import { TOTAL_PAGES, pageText, PAGE_TITLES } from "@/components/comic/pages";

export type FitMode = "height" | "width" | "actual";
export type ViewMode = "paged" | "webtoon";
export type Direction = "ltr" | "rtl";
export type PresetId = "original" | "night" | "sepia" | "contrast" | "grayscale" | "invert";
export type PanelId = "color" | "bookmarks" | "search" | "menu" | null;

export type ColorState = {
  preset: PresetId;
  brightness: number; // -100 … 100
  contrast: number; // 0.5 … 2.0
  warmth: number; // -100 … 100
};

export const PRESETS: { id: PresetId; label: string; brightness: number; contrast: number; warmth: number; swatch: string }[] = [
  { id: "original", label: "Original", brightness: 0, contrast: 1, warmth: 0, swatch: "linear-gradient(135deg,#FF1F6D,#FFD700,#00C2FF)" },
  { id: "night", label: "Night", brightness: -22, contrast: 0.95, warmth: 70, swatch: "linear-gradient(135deg,#3B2400,#FFB547)" },
  { id: "sepia", label: "Sepia", brightness: 4, contrast: 1.05, warmth: 40, swatch: "linear-gradient(135deg,#704214,#F3E0C0)" },
  { id: "contrast", label: "Contrast", brightness: 4, contrast: 1.5, warmth: 0, swatch: "linear-gradient(135deg,#000,#fff)" },
  { id: "grayscale", label: "Grayscale", brightness: 0, contrast: 1.1, warmth: 0, swatch: "linear-gradient(135deg,#222,#bbb)" },
  { id: "invert", label: "Invert", brightness: 0, contrast: 1, warmth: 0, swatch: "linear-gradient(135deg,#fff,#00C2FF 50%,#111)" },
];

export const DEFAULT_COLOR: ColorState = { preset: "original", brightness: 0, contrast: 1, warmth: 0 };

/** Builds the CSS filter chain that stands in for Komik's GPU LUT. */
export function cssFilter(c: ColorState): string {
  const parts: string[] = [];
  if (c.preset === "sepia") parts.push("sepia(0.75)");
  if (c.preset === "grayscale") parts.push("grayscale(1)");
  if (c.preset === "invert") parts.push("invert(1) hue-rotate(180deg)");
  if (c.brightness !== 0) parts.push(`brightness(${(1 + (c.brightness / 100) * 0.6).toFixed(3)})`);
  if (c.contrast !== 1) parts.push(`contrast(${c.contrast.toFixed(2)})`);
  if (c.warmth > 0) parts.push(`sepia(${((c.warmth / 100) * 0.55).toFixed(3)}) saturate(${(1 + (c.warmth / 100) * 0.35).toFixed(3)})`);
  if (c.warmth < 0) parts.push(`hue-rotate(${((-c.warmth / 100) * 18).toFixed(1)}deg) saturate(${(1 - (-c.warmth / 100) * 0.15).toFixed(3)})`);
  return parts.length ? parts.join(" ") : "none";
}

export const clamp = (v: number, lo: number, hi: number) => Math.max(lo, Math.min(hi, v));

/** Physical-book spread pairing: cover alone, then 2-3, 4-5 … and a lone back cover. */
export function spreadOf(page: number, spread: boolean, total = TOTAL_PAGES): number[] {
  if (!spread) return [page];
  if (page <= 1) return [1];
  const first = page % 2 === 0 ? page : page - 1;
  if (first + 1 > total) return [first];
  return [first, first + 1];
}

export type SearchHit = { page: number; text: string; start: number; len: number };

export function searchScript(query: string): SearchHit[] {
  const q = query.trim().toUpperCase();
  if (q.length < 2) return [];
  const hits: SearchHit[] = [];
  for (let p = 1; p <= TOTAL_PAGES; p++) {
    for (const line of pageText(p)) {
      const idx = line.toUpperCase().indexOf(q);
      if (idx >= 0) hits.push({ page: p, text: line, start: idx, len: q.length });
    }
  }
  return hits;
}

export { TOTAL_PAGES, PAGE_TITLES, pageText };
