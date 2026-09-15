// Picks the set of images that are the comic's pages out of everything harvested from a page.
import type { Confidence } from "@/shared/types";
import { extOf } from "@/shared/util";
import { NOT_A_PAGE, type Candidate } from "./harvest";

export interface PageCluster {
  candidates: Candidate[];
  score: number;
  confidence: Confidence;
  reason: string;
}

function directoryOf(url: string): string {
  try {
    const u = new URL(url);
    return u.host + u.pathname.replace(/[^/]*$/, "");
  } catch {
    return url;
  }
}

function median(values: number[]): number {
  const v = values.filter((x) => x > 0).sort((a, b) => a - b);
  if (!v.length) return 0;
  const mid = Math.floor(v.length / 2);
  return v.length % 2 ? v[mid] : (v[mid - 1] + v[mid]) / 2;
}

/** Trailing number in the file name ("page-012.jpg" → 12), used to spot numbered page sequences. */
export function trailingNumber(url: string): number | null {
  try {
    const name = decodeURIComponent(new URL(url).pathname.split("/").pop() ?? "");
    const m = /(\d{1,5})(?!.*\d)/.exec(name.replace(/\.[a-z0-9]+$/i, ""));
    return m ? Number.parseInt(m[1], 10) : null;
  } catch {
    return null;
  }
}

function sequentialShare(urls: string[]): number {
  const nums = urls.map(trailingNumber);
  if (nums.some((n) => n === null) || urls.length < 3) return 0;
  let steps = 0;
  for (let i = 1; i < nums.length; i++) if ((nums[i] as number) - (nums[i - 1] as number) === 1) steps++;
  return steps / (nums.length - 1);
}

export function isLikelyPage(c: Candidate, minWidth: number): boolean {
  if (NOT_A_PAGE.test(c.url)) return false;
  const ext = extOf(c.url);
  if (ext === "svg" || ext === "ico") return false;
  if (c.width && c.height) {
    if (c.width < minWidth && c.height < minWidth * 1.2) return false;
    if (c.width < 120 || c.height < 120) return false;
    // Very wide short images are banners.
    if (c.width / c.height > 3.2) return false;
  }
  return true;
}

export function clusterPages(all: Candidate[], options: { minWidth: number }): PageCluster | null {
  const usable = all.filter((c) => isLikelyPage(c, options.minWidth));
  if (!usable.length) return null;

  // Group by DOM surroundings; script URLs group by folder instead.
  const groups = new Map<string, Candidate[]>();
  for (const c of usable) {
    const key = c.source === "script" ? `script:${directoryOf(c.url)}` : `${c.source === "link" ? "link:" : ""}${c.container}`;
    const list = groups.get(key) ?? [];
    list.push(c);
    groups.set(key, list);
  }

  const clusters: PageCluster[] = [];
  for (const [key, list] of groups) {
    const count = list.length;
    const widths = list.map((c) => c.width);
    const heights = list.map((c) => c.height);
    const medW = median(widths);
    const medH = median(heights);
    const known = list.filter((c) => c.width && c.height).length;
    const tall = medW && medH ? medH / medW : 0;
    const dirs = new Set(list.map((c) => directoryOf(c.url)));
    const sameDirShare = Math.max(...Array.from(dirs, (d) => list.filter((c) => directoryOf(c.url) === d).length)) / count;
    const seq = sequentialShare(list.map((c) => c.url));
    const consistentWidth = known >= 2 ? list.filter((c) => c.width && Math.abs(c.width - medW) / medW < 0.08).length / known : 0;

    let score = Math.min(count, 60) * 3;
    score += sameDirShare * 25;
    score += seq * 30;
    score += consistentWidth * 20;
    if (medW >= 600) score += 20;
    else if (medW >= 400) score += 10;
    if (tall >= 1.15) score += 15; // portrait pages or webtoon strips
    if (count === 1) score -= 25;
    if (key.startsWith("script:")) score -= 12; // may include thumbnails or other chapters' covers
    if (key.startsWith("link:")) score -= 6;
    if (/comment|sidebar|related|recommend|widget|popular|similar/i.test(key)) score -= 40;
    if (/read|chapter|page|comic|manga|viewer|reader|content|entry|gallery|strip|webtoon/i.test(key)) score += 12;

    const confidence: Confidence = count >= 5 && score >= 70 ? "high" : count >= 2 && score >= 45 ? "medium" : "low";
    clusters.push({
      candidates: list.sort((a, b) => a.order - b.order),
      score,
      confidence,
      reason: `${count} image${count === 1 ? "" : "s"} in ${key.startsWith("script:") ? "page script" : "reader column"}`
    });
  }

  clusters.sort((a, b) => b.score - a.score);
  const best = clusters[0];
  if (!best) return null;

  // Webtoon readers sometimes split one chapter over sibling containers with the same shape: merge them.
  const signature = best.candidates[0]?.container.split("<").slice(1).join("<");
  if (signature && !best.candidates[0].container.startsWith("script")) {
    for (const other of clusters.slice(1)) {
      const sameParent = other.candidates[0]?.container.split("<").slice(1).join("<") === signature;
      if (sameParent && other.score >= best.score * 0.6) {
        best.candidates = [...best.candidates, ...other.candidates].sort((a, b) => a.order - b.order);
      }
    }
  }
  return best;
}

/**
 * Fills gaps in clearly numbered page URLs ("001.jpg … 004.jpg, 006.jpg" → adds 005.jpg). Only gaps
 * inside the known range are filled, and only when most of the sequence is already present.
 */
export function fillSequenceGaps(urls: string[]): string[] {
  if (urls.length < 4) return urls;
  const parsed = urls.map((url) => {
    const m = /^(.*?)(\d{1,5})(\.[a-z0-9]{2,5}(?:\?.*)?)$/i.exec(url);
    return m ? { prefix: m[1], digits: m[2], suffix: m[3] } : null;
  });
  if (parsed.some((p) => !p)) return urls;
  const first = parsed[0]!;
  if (!parsed.every((p) => p!.prefix === first.prefix && p!.suffix.replace(/\?.*$/, "") === first.suffix.replace(/\?.*$/, ""))) return urls;
  const nums = parsed.map((p) => Number.parseInt(p!.digits, 10));
  const min = Math.min(...nums);
  const max = Math.max(...nums);
  const span = max - min + 1;
  if (span > urls.length * 1.3 || span > 2000 || urls.length / span < 0.7) return urls;
  const pad = first.digits.length > String(max).length || first.digits.startsWith("0") ? first.digits.length : 0;
  const have = new Set(nums);
  const out: string[] = [];
  for (let n = min; n <= max; n++) {
    const idx = nums.indexOf(n);
    if (idx >= 0) out.push(urls[idx]);
    else if (!have.has(n)) out.push(`${first.prefix}${pad ? String(n).padStart(pad, "0") : n}${first.suffix}`);
  }
  return out;
}
