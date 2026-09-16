// Many comic sites show small previews and keep the full-size page somewhere close by: another folder,
// another host, or the same name without a size suffix. These rules turn a preview URL into an ordered
// list of candidates, best quality first; the downloader tries them in order and keeps the first that works.

const SIZE_QUERY = /^(?:w|width|h|height|q|quality|size|resize|fit|thumb|thumbnail|scale|dpr|format|auto)$/i;

/** Folder names that mean "small copy", with what they usually become. */
const FOLDER_SWAPS: Array<[RegExp, string[]]> = [
  [/\/(?:thumbs?|thumbnails?|thumbnail)\//i, ["/images/", "/image/", "/original/", "/originals/", "/full/", "/"]],
  [/\/(?:small|mini|preview|previews|tn|sm)\//i, ["/original/", "/originals/", "/images/", "/large/", "/full/", "/"]],
  [/\/(?:150|200|250|300|320|360)x\d*\//i, ["/original/", "/full/", "/"]],
  [/\/c\/\d+x\d+\//i, ["/"]],
  [/\/resize\/[^/]+\//i, ["/"]],
  [/\/cover\//i, ["/images/", "/"]]
];

/** File-name endings that mean "small copy" ("page12t.jpg", "page12_thumb.jpg", "12-small.webp"). */
const NAME_SUFFIXES = [/([-_.])(?:thumb|thumbnail|small|mini|preview|tn|sm|s|t|m|b)$/i, /(\d+)t$/i, /(\d+)s$/i];

/** Extensions a full-size page may use when the preview is a webp/avif copy. */
const SWAP_EXTENSIONS: Record<string, string[]> = {
  webp: ["jpg", "png", "jpeg", "webp"],
  avif: ["jpg", "png", "jpeg", "avif"],
  jpg: ["jpg", "png", "jpeg", "webp"],
  jpeg: ["jpeg", "jpg", "png", "webp"],
  png: ["png", "jpg", "jpeg", "webp"],
  gif: ["gif", "jpg", "png"]
};

/** Hosts are often split by role: t1.example → i1.example / i.example. */
function hostSwaps(url: URL): string[] {
  const out: string[] = [];
  const host = url.hostname;
  const first = host.split(".")[0];
  const m = /^(t|th|thumb|thumbs|tn|s|small|preview)(\d*)$/i.exec(first);
  if (!m) return out;
  for (const prefix of ["i", "img", "image"]) {
    const next = new URL(url.href);
    next.hostname = host.replace(/^[^.]+/, `${prefix}${m[2] ?? ""}`);
    out.push(next.href);
  }
  return out;
}

export function looksLikePreview(rawUrl: string): boolean {
  try {
    const url = new URL(rawUrl);
    if (FOLDER_SWAPS.some(([re]) => re.test(url.pathname))) return true;
    if (hostSwaps(url).length > 0) return true;
    const stem = url.pathname.replace(/\.[a-z0-9]+$/i, "").split("/").pop() ?? "";
    if (NAME_SUFFIXES.some((re) => re.test(stem))) return true;
    for (const key of url.searchParams.keys()) if (SIZE_QUERY.test(key)) return true;
    return false;
  } catch {
    return false;
  }
}

/**
 * Full-size candidates for a preview URL, best first. The original URL is always last, so a page still
 * downloads when none of the guesses exist.
 */
export function fullSizeCandidates(rawUrl: string, limit = 14): string[] {
  let url: URL;
  try {
    url = new URL(rawUrl);
  } catch {
    return [rawUrl];
  }

  const out: string[] = [];
  const add = (value: string) => {
    if (value && value !== rawUrl && !out.includes(value)) out.push(value);
  };

  // 1. Drop resizing query parameters ("?w=200&q=60").
  if (Array.from(url.searchParams.keys()).some((k) => SIZE_QUERY.test(k))) {
    const clean = new URL(url.href);
    for (const key of Array.from(clean.searchParams.keys())) if (SIZE_QUERY.test(key)) clean.searchParams.delete(key);
    add(clean.href);
  }

  const base = new URL(url.href);
  base.search = url.search;
  const path = base.pathname;
  const dir = path.replace(/[^/]*$/, "");
  const file = path.split("/").pop() ?? "";
  const dot = file.lastIndexOf(".");
  const stem = dot > 0 ? file.slice(0, dot) : file;
  const ext = (dot > 0 ? file.slice(dot + 1) : "").toLowerCase();

  // 2. Folders that mean "small copy" ("/thumbs/" → "/images/").
  const swappedDirs: string[] = [];
  for (const [re, replacements] of FOLDER_SWAPS) {
    if (!re.test(path)) continue;
    for (const replacement of replacements) swappedDirs.push(path.replace(re, replacement).replace(/[^/]*$/, ""));
  }

  // 3. Size suffixes on the file name ("12t.jpg" → "12.jpg").
  const shortStems: string[] = [];
  for (const re of NAME_SUFFIXES) {
    const trimmed = stem.replace(re, (_m, keep: string) => (/\d/.test(keep ?? "") ? keep : ""));
    if (trimmed && trimmed !== stem && !shortStems.includes(trimmed)) shortStems.push(trimmed);
  }

  // 4. Full-size hosts ("t3.example" → "i3.example"), which is the strongest signal when it applies.
  const hosts = [...new Set([...hostSwaps(base).map((u) => new URL(u).hostname), base.hostname])];

  // 5. Try every host / folder / name combination, the changed ones first, keeping the original file
  //    extension for the whole first pass (a preview is usually the same format as the page).
  const extensions = SWAP_EXTENSIONS[ext] ?? (ext ? [ext] : ["jpg", "png", "webp"]);
  const primary = ext || extensions[0];
  const dirs = [...swappedDirs, dir];
  const stems = [...shortStems, stem];
  // Per name shape, walk file types then hosts then folders: a preview is often a .webp copy of a .jpg
  // page, and the full-size copy often lives on a sister host.
  const ordered = [primary, ...extensions.filter((e) => e !== primary)];
  const byStem = stems.map((s) => {
    const list: string[] = [];
    for (const e of ordered) {
      for (const host of hosts) {
        for (const d of dirs) {
          const target = new URL(base.href);
          target.hostname = host;
          target.pathname = `${d}${s}.${e}`;
          if (target.href !== rawUrl && !list.includes(target.href)) list.push(target.href);
        }
      }
    }
    return list;
  });

  // The trimmed name takes most of the budget, but the untrimmed one keeps a few slots of its own.
  const quotas = [Math.max(4, limit - 4), 4, 2];
  byStem.forEach((list, i) => list.slice(0, quotas[i] ?? 2).forEach(add));

  // The preview itself is always the last resort, so a page is never lost when every guess is wrong.
  const list = out.slice(0, limit);
  list.push(rawUrl);
  return list;
}

/** How one site turns a preview URL into its full-size page, learned from a page that already worked. */
export interface PreviewRule {
  fromHost: string;
  toHost: string;
  fromDir: string;
  toDir: string;
  /** Characters to drop from the end of the file name ("1t" → "1"). */
  suffix: string;
  fromExt: string;
  toExt: string;
}

function parts(rawUrl: string) {
  const url = new URL(rawUrl);
  const file = url.pathname.split("/").pop() ?? "";
  const dot = file.lastIndexOf(".");
  return {
    url,
    dir: url.pathname.replace(/[^/]*$/, ""),
    stem: dot > 0 ? file.slice(0, dot) : file,
    ext: dot > 0 ? file.slice(dot + 1) : ""
  };
}

/**
 * Works out the rule connecting one preview to its full-size page, so the rest of the gallery can be
 * built without opening every page. Returns null when the two names aren't the same page.
 */
export function derivePreviewRule(previewUrl: string, fullUrl: string): PreviewRule | null {
  try {
    const preview = parts(previewUrl);
    const full = parts(fullUrl);
    if (preview.stem !== full.stem && !preview.stem.startsWith(full.stem)) return null;
    const suffix = preview.stem.slice(full.stem.length);
    // A page number must survive: "1t" → "1" is a rule, "cover" → "1" is not.
    if (suffix.length > 3 || !/^\d+$/.test(full.stem.replace(/\D+/g, "") || "x")) return null;
    return {
      fromHost: preview.url.hostname,
      toHost: full.url.hostname,
      fromDir: preview.dir,
      toDir: full.dir,
      suffix,
      fromExt: preview.ext,
      toExt: full.ext
    };
  } catch {
    return null;
  }
}

/** Applies a learned rule to another preview from the same gallery. */
export function applyPreviewRule(rule: PreviewRule, previewUrl: string): string | null {
  try {
    const preview = parts(previewUrl);
    if (preview.url.hostname !== rule.fromHost || preview.dir !== rule.fromDir || preview.ext !== rule.fromExt) return null;
    const stem = rule.suffix && preview.stem.endsWith(rule.suffix) ? preview.stem.slice(0, -rule.suffix.length) : preview.stem;
    if (!stem) return null;
    const out = new URL(preview.url.href);
    out.hostname = rule.toHost;
    out.pathname = `${rule.toDir}${stem}.${rule.toExt}`;
    return out.href;
  } catch {
    return null;
  }
}
