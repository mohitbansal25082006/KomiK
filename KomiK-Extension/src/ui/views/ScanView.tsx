import { AnimatePresence, motion } from "framer-motion";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { applySeriesMemory, finalizeMeta, rememberSeries } from "@/shared/enrich";
import { send } from "@/shared/messages";
import type { ComicMeta, DetectResult, OutputFormat, Settings } from "@/shared/types";
import { Burst, Stamp } from "../components/Brand";
import { Button, Segmented, Sticker, Tabs } from "../components/Controls";
import { Icon } from "../components/Icon";
import { RemoteImage } from "../components/RemoteImage";
import { sfx } from "../sound";
import { MetadataEditor } from "./MetadataEditor";
import { ChapterList, PagesGrid } from "./ScanParts";

type Tab = "pages" | "chapters" | "details";

const SCAN_LINES = ["Reading the panels…", "Hunting lazy-loaded pages…", "Checking chapter lists…", "Lifting titles and tags…", "Sniffing out download links…"];

const CONFIDENCE: Record<DetectResult["confidence"], { text: string; color: string }> = {
  high: { text: "PERFECT MATCH!", color: "#2FD17A" },
  medium: { text: "LOOKS GOOD", color: "#00C2FF" },
  low: { text: "BEST GUESS", color: "#FF7A00" },
  none: { text: "NOTHING YET", color: "#FF1F6D" }
};

export function ScanView({ tab, settings, compact = false, onOpenQueue }: { tab: chrome.tabs.Tab | null; settings: Settings; compact?: boolean; onOpenQueue?: () => void }) {
  const [status, setStatus] = useState<"scanning" | "ready" | "error" | "unsupported">("scanning");
  const [error, setError] = useState("");
  const [line, setLine] = useState(0);
  const [result, setResult] = useState<DetectResult | null>(null);
  const [meta, setMeta] = useState<ComicMeta | null>(null);
  const [excluded, setExcluded] = useState<Set<number>>(new Set());
  const [coverIndex, setCoverIndex] = useState(0);
  const [reversed, setReversed] = useState(false);
  const [chapters, setChapters] = useState<Set<string>>(new Set());
  const [view, setView] = useState<Tab>("pages");
  const [format, setFormat] = useState<OutputFormat>(settings.defaultFormat);
  const [stamp, setStamp] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => setFormat(settings.defaultFormat), [settings.defaultFormat]);
  const settingsRef = useRef(settings);
  settingsRef.current = settings;

  const scan = useCallback(
    async (deep = false, force = false) => {
      if (!tab?.id) return;
      if (!tab.url || !/^https?:/.test(tab.url)) {
        setStatus("unsupported");
        return;
      }
      setStatus("scanning");
      setError("");
      const res = await send("scan-tab", { tabId: tab.id, deep, force }).catch((err: unknown) => ({ error: String(err) }));
      if (!res || "error" in res) {
        setError(res && "error" in res ? res.error : "The page didn't answer.");
        setStatus("error");
        sfx.bonk();
        return;
      }
      // Show exactly what will be saved: cleaned tags and credits, then the user's remembered series edits.
      const current = settingsRef.current;
      const cleaned = finalizeMeta(res.meta, current);
      const remembered = current.rememberSeriesEdits ? await applySeriesMemory(cleaned) : cleaned;
      setResult({ ...res, meta: cleaned });
      setMeta(remembered);
      setExcluded(new Set());
      setCoverIndex(0);
      setReversed(false);
      setChapters(new Set());
      setView(res.pages.length || !res.chapters.length ? "pages" : "chapters");
      setStatus("ready");
      if (res.pages.length || res.chapters.length) sfx.pop();
    },
    [tab?.id, tab?.url]
  );

  useEffect(() => {
    void scan();
  }, [scan]);

  useEffect(() => {
    if (status !== "scanning") return;
    const t = setInterval(() => setLine((l) => (l + 1) % SCAN_LINES.length), 900);
    return () => clearInterval(t);
  }, [status]);

  const pages = useMemo(() => {
    const list = result?.pages ?? [];
    return reversed ? [...list].reverse() : list;
  }, [result, reversed]);

  const selectedPages = pages.filter((_, i) => !excluded.has(i));

  const queued = (label: string) => {
    sfx.whoosh();
    setStamp(label);
  };

  const downloadPages = async () => {
    if (!result || !meta || !selectedPages.length || !tab?.id) return;
    setBusy(true);
    try {
      if (settings.rememberSeriesEdits) await rememberSeries(meta, result.meta).catch(() => undefined);
      const keptCover = pages[coverIndex] && !excluded.has(coverIndex) ? selectedPages.indexOf(pages[coverIndex]) : 0;
      await send("queue-jobs", { jobs: [{ meta, format, pages: selectedPages, coverIndex: Math.max(0, keptCover), sourceUrl: result.url, tabId: tab.id }] });
      queued("QUEUED!");
    } finally {
      setBusy(false);
    }
  };

  const downloadChapters = async () => {
    if (!result || !meta || !chapters.size) return;
    setBusy(true);
    try {
      const list = result.chapters.filter((c) => chapters.has(c.url));
      await send("queue-chapters", { base: result, meta, chapters: list, format, tabId: tab?.id });
      queued(`${list.length} QUEUED!`);
    } finally {
      setBusy(false);
    }
  };

  const pick = async () => {
    if (!tab?.id) return;
    await send("start-picker", { tabId: tab.id });
    if (compact) window.close();
  };

  // ── Primary action ──
  let primary: { label: string; action: () => void; disabled?: boolean } = { label: "DOWNLOAD", action: () => undefined, disabled: true };
  if (status === "ready" && result) {
    if (view === "chapters" && result.chapters.length) {
      primary = chapters.size ? { label: `GET ${chapters.size} CHAPTER${chapters.size === 1 ? "" : "S"}`, action: downloadChapters } : { label: "PICK CHAPTERS", action: () => undefined, disabled: true };
    } else if (selectedPages.length) {
      primary = { label: `GET ${selectedPages.length} PAGE${selectedPages.length === 1 ? "" : "S"}`, action: downloadPages };
    } else if (result.chapters.length) {
      primary = { label: "CHOOSE CHAPTERS", action: () => setView("chapters") };
    }
  }

  if (status === "unsupported") {
    return (
      <EmptyPanel title="NOT A COMIC PAGE" sticker="BROWSER PAGE" text="KomiK works on web pages. Open a comic, manga or webtoon chapter and click the KomiK icon again." />
    );
  }

  if (status === "scanning") {
    return (
      <div className="grid flex-1 place-items-center px-4 py-10">
        <div className="flex flex-col items-center gap-5">
          <Burst className="h-36 w-36" spin fill="#FFD700">
            <motion.span animate={{ scale: [1, 1.12, 1] }} transition={{ repeat: Infinity, duration: 0.9 }} className="letter-outline block text-[30px] leading-none">
              SCAN!
            </motion.span>
          </Burst>
          <AnimatePresence mode="wait">
            <motion.p key={line} initial={{ y: 8, opacity: 0 }} animate={{ y: 0, opacity: 1 }} exit={{ y: -8, opacity: 0 }} className="font-comic text-[15px] font-bold">
              {SCAN_LINES[line]}
            </motion.p>
          </AnimatePresence>
        </div>
      </div>
    );
  }

  if (status === "error" || !result || !meta) {
    return (
      <EmptyPanel title="OOPS!" sticker="SCAN FAILED" text={error || "Something went wrong while reading this page."}>
        <Button tone="yellow" icon="refresh" onClick={() => scan(false, true)}>Try again</Button>
      </EmptyPanel>
    );
  }

  const nothing = !result.pages.length && !result.chapters.length;
  const conf = CONFIDENCE[result.confidence];

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      {stamp && <Stamp text={stamp} onDone={() => setStamp(null)} />}
      <div className="scroll-area min-h-0 flex-1 px-3 pb-3 pt-3">
        {/* Hero */}
        <motion.div initial={{ y: 12, opacity: 0 }} animate={{ y: 0, opacity: 1 }} transition={{ type: "spring", stiffness: 380, damping: 26 }} className="panel relative overflow-hidden">
          <div className="absolute inset-0 bg-halftone" />
          <div className="relative flex gap-3 p-3">
            <div className="relative w-[78px] shrink-0">
              <div className="aspect-[2/3] overflow-hidden rounded-[4px] border-[2.5px] border-gutter bg-panel-card shadow-comic-sm" style={{ transform: "rotate(-3deg)" }}>
                <RemoteImage src={meta.coverUrl || pages[0]?.url} referer={result.url} className="h-full w-full object-cover" fallback={<Icon name="book" size={26} className="text-muted" />} />
              </div>
            </div>
            <div className="min-w-0 flex-1">
              <div className="mb-1 flex flex-wrap items-center gap-1">
                <Sticker color={conf.color} rotate={-2}>{conf.text}</Sticker>
                <span className="truncate text-[11px] font-semibold text-muted" title={result.adapter}>{result.adapter}</span>
              </div>
              <h1 className="letter line-clamp-2 text-[22px] leading-[1.02]" title={meta.title}>{meta.title || result.pageTitle || "Untitled"}</h1>
              <div className="mt-1 truncate text-[12px] font-semibold text-muted">
                <Icon name="globe" size={12} className="mr-1 inline -translate-y-px" />
                {result.site}
                {meta.writers[0] ? ` · ${meta.writers[0]}` : ""}
              </div>
              <div className="mt-2 flex flex-wrap gap-1">
                {[...meta.genres, ...meta.tags].slice(0, compact ? 4 : 8).map((t) => (
                  <span key={t} className="rounded-full border-2 border-gutter bg-panel-card px-1.5 text-[10.5px] font-bold leading-[16px]">{t}</span>
                ))}
              </div>
            </div>
          </div>
          <div className="relative flex border-t-[2.5px] border-gutter bg-panel-card text-center">
            {[
              { n: result.pages.length, l: "pages", c: "#00C2FF" },
              { n: result.chapters.length, l: "chapters", c: "#FF7A00" }
            ].map((s, i) => (
              <div key={s.l} className={`flex-1 py-1 ${i ? "border-l-[2.5px] border-gutter" : ""}`}>
                <span className="letter text-[19px] leading-none" style={{ color: s.n ? s.c : undefined }}>{s.n}</span>
                <span className="ml-1 font-comic text-[11px] font-bold uppercase text-muted">{s.l}</span>
              </div>
            ))}
          </div>
        </motion.div>

        {/* Quick tools */}
        <div className="mt-3 flex gap-1.5">
          <Button size="sm" icon="bolt" onClick={() => scan(true, true)} title="Scroll through the page to load lazy pages, then scan again">Deep scan</Button>
          <Button size="sm" icon="cursor" onClick={pick} title="Click the pages you want on the page itself">Pick pages</Button>
          {onOpenQueue && <Button size="sm" icon="queue" className="ml-auto" onClick={onOpenQueue}>Queue</Button>}
        </div>

        {nothing ? (
          <EmptyPanel title="NO COMIC HERE!" sticker="EMPTY PANEL" text="No pages, chapters or comic files were found. Open a chapter's reader page, try a deep scan, or pick the pages yourself.">
            <div className="flex gap-2">
              <Button tone="yellow" icon="bolt" onClick={() => scan(true, true)}>Deep scan</Button>
              <Button tone="cyan" icon="cursor" onClick={pick}>Pick pages</Button>
            </div>
          </EmptyPanel>
        ) : (
          <div className="mt-3">
            <Tabs<Tab>
              value={view}
              onChange={setView}
              tabs={[
                { id: "pages", label: "Pages", count: result.pages.length, disabled: !result.pages.length },
                { id: "chapters", label: "Chapters", count: result.chapters.length, disabled: !result.chapters.length },
                { id: "details", label: "Details" }
              ]}
            />
            <div className="panel !rounded-tl-none !shadow-comic-sm p-3">
              <AnimatePresence mode="wait">
                <motion.div key={view} initial={{ opacity: 0, x: 10 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -10 }} transition={{ duration: 0.16 }}>
                  {view === "pages" && (
                    <PagesGrid
                      pages={pages}
                      excluded={excluded}
                      coverIndex={coverIndex}
                      columns={compact ? 4 : 5}
                      onToggle={(i) => setExcluded((s) => { const n = new Set(s); if (n.has(i)) n.delete(i); else n.add(i); return n; })}
                      onCover={setCoverIndex}
                      onSelectAll={() => setExcluded(new Set())}
                      onSelectNone={() => setExcluded(new Set(pages.map((_, i) => i)))}
                      onReverse={() => { setReversed((r) => !r); setExcluded(new Set()); setCoverIndex(0); }}
                    />
                  )}
                  {view === "chapters" && <ChapterList chapters={result.chapters} selected={chapters} onChange={setChapters} />}
                  {view === "details" && <MetadataEditor meta={meta} onChange={setMeta} settings={settings} format={format} pages={selectedPages.length} compact={compact} />}
                </motion.div>
              </AnimatePresence>
            </div>
          </div>
        )}
      </div>

      {/* Download bar */}
      {!nothing && (
        <div className="relative border-t-[3px] border-gutter bg-panel px-3 pb-3 pt-2.5">
          <div className="absolute inset-0 bg-halftone opacity-70" />
          <div className="relative flex items-center gap-2">
            <Segmented<OutputFormat>
              className="flex-1"
              value={format}
              onChange={setFormat}
              options={[
                { id: "cbz", label: "CBZ", hint: "Comic archive with details and tags (best for Komik)" },
                { id: "zip", label: "ZIP", hint: "Same as CBZ with a .zip name" },
                { id: "pdf", label: "PDF", hint: "One PDF with title, authors and keywords" },
                { id: "folder", label: "FOLDER", hint: "Image files plus ComicInfo.xml in a folder" }
              ]}
            />
            <motion.button
              whileHover={{ scale: primary.disabled ? 1 : 1.04, rotate: primary.disabled ? 0 : -1.5 }}
              whileTap={{ scale: 0.94 }}
              disabled={primary.disabled || busy}
              onClick={() => primary.action()}
              className="relative h-[46px] shrink-0 overflow-hidden rounded-md border-[3px] border-gutter bg-magenta px-4 font-bangers text-[19px] tracking-wider text-white shadow-comic disabled:cursor-not-allowed disabled:opacity-50"
              style={{ WebkitTextStroke: "1px #08080A", paintOrder: "stroke fill", textShadow: "2px 2px 0 #08080A" }}
            >
              <span className="absolute inset-0 bg-sunburst opacity-60" />
              <span className="absolute inset-y-0 -left-1/2 w-1/3 bg-white/25 animate-shine" />
              <span className="relative flex items-center gap-1.5">
                <Icon name="download" size={18} stroke={3} />
                {busy ? "…" : primary.label}
              </span>
            </motion.button>
          </div>
        </div>
      )}
    </div>
  );
}

function EmptyPanel({ title, sticker, text, children }: { title: string; sticker: string; text: string; children?: React.ReactNode }) {
  return (
    <motion.div initial={{ scale: 0.92, opacity: 0 }} animate={{ scale: 1, opacity: 1 }} className="panel relative m-3 overflow-hidden p-5 text-center">
      <div className="absolute inset-0 bg-speedlines" />
      <div className="relative flex flex-col items-center gap-3">
        <Burst className="h-24 w-24 animate-wobble" fill="#FF1F6D">
          <Icon name="search" size={30} stroke={3} className="text-white drop-shadow-[2px_2px_0_#000]" />
        </Burst>
        <Sticker color="#FFD700" rotate={-3}>{sticker}</Sticker>
        <h2 className="letter text-[26px] leading-none">{title}</h2>
        <p className="max-w-[300px] text-[13px] leading-snug text-muted">{text}</p>
        {children}
      </div>
    </motion.div>
  );
}
