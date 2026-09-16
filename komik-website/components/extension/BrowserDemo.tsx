"use client";

import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import { AnimatePresence, motion, useInView, useReducedMotion } from "framer-motion";
import { Check, ChevronLeft, ChevronRight, FileArchive, FolderOpen, Lock, MousePointerClick, Puzzle, RotateCcw, Star, X, Zap } from "lucide-react";
import KomikLogo from "@/components/KomikLogo";
import ComicBurst from "@/components/fx/ComicBurst";
import { ComicPage, COMIC_TITLE } from "@/components/comic/pages";

/* -------------------------------------------------------------------------- */
/*  A working miniature of KomiK Downloader: a comic site in a browser tab,    */
/*  the extension popup scanning it, the download and the CBZ landing on a    */
/*  Komik shelf with every tag inside.                                         */
/* -------------------------------------------------------------------------- */

type Stage = "idle" | "scanning" | "ready" | "downloading" | "saved";

const PAGES = 12;
const SITE_PAGES = [1, 2, 3, 4];
const THUMBS = [1, 2, 3, 4, 5, 6, 7, 8];

/** Tag links as the site prints them: counts, menu words and duplicates included. */
const RAW_TAGS = [
  { raw: "Home", clean: null },
  { raw: "cyberpunk 8,102", clean: "Cyberpunk" },
  { raw: "sci-fi (12.4K)", clean: "Sci-Fi" },
  { raw: "Read now", clean: null },
  { raw: "action 1,234", clean: "Action" },
  { raw: "#drones", clean: "Drones" },
  { raw: "neon city 48", clean: "Neon City" },
  { raw: "found family", clean: "Found Family" },
  { raw: "Actions", clean: null },
  { raw: "offline heist", clean: "Offline Heist" },
] as const;
const CLEAN_TAGS = RAW_TAGS.map((t) => t.clean).filter(Boolean) as string[];

const SCAN_LINES = ["Reading the panels…", "Scrolling lazy pages in…", "Checking the chapter list…", "Lifting titles & tags…"];
const STEPS: { id: Stage[]; label: string }[] = [
  { id: ["idle"], label: "Open a chapter" },
  { id: ["scanning"], label: "KomiK scans it" },
  { id: ["ready", "downloading"], label: "Grab every page" },
  { id: ["saved"], label: "Read it in Komik" },
];

const XML_LINES = [
  `<Title>${COMIC_TITLE}</Title>`,
  "<Series>Cyberpunk Chronicles</Series>",
  "<Number>1</Number>",
  "<Writer>Mira Sol</Writer>",
  "<Penciller>Dex Halloway</Penciller>",
  "<Year>2024</Year>",
  `<Tags>${CLEAN_TAGS.join(", ")}</Tags>`,
  `<PageCount>${PAGES}</PageCount>`,
];

const SHELF = [
  { t: "Neon Ronin", v: "Vol. 1", c: "#FF1F6D" },
  { t: "Paper Moon", v: "Ep. 42", c: "#00C2FF" },
  { t: "Iron Orchard", v: "#7", c: "#2FD17A" },
];

function PageThumb({ n, className = "" }: { n: number; className?: string }) {
  return (
    <div className={`overflow-hidden bg-paper [contain:paint] ${className}`}>
      <ComicPage n={n} />
    </div>
  );
}

export default function BrowserDemo() {
  const rootRef = useRef<HTMLDivElement>(null);
  const popupFileRef = useRef<HTMLDivElement>(null);
  const shelfSlotRef = useRef<HTMLDivElement>(null);
  const inView = useInView(rootRef, { margin: "-20% 0px" });
  const reduce = useReducedMotion();

  const [stage, setStage] = useState<Stage>("idle");
  const [popupOpen, setPopupOpen] = useState(false);
  const [scanLine, setScanLine] = useState(0);
  const [found, setFound] = useState(0);
  const [done, setDone] = useState(0);
  const [tab, setTab] = useState<"pages" | "details">("pages");
  const [flight, setFlight] = useState<{ from: DOMRect; to: DOMRect; root: DOMRect } | null>(null);
  const [landed, setLanded] = useState(false);
  const [xmlShown, setXmlShown] = useState(0);
  const [touched, setTouched] = useState(false);
  const timers = useRef<number[]>([]);

  const later = useCallback((ms: number, fn: () => void) => {
    timers.current.push(window.setTimeout(fn, reduce ? Math.min(ms, 60) : ms));
  }, [reduce]);
  const clearTimers = () => {
    timers.current.forEach((t) => window.clearTimeout(t));
    timers.current = [];
  };
  useEffect(() => clearTimers, []);

  const reset = useCallback(() => {
    clearTimers();
    setStage("idle");
    setPopupOpen(false);
    setFound(0);
    setDone(0);
    setTab("pages");
    setFlight(null);
    setLanded(false);
    setXmlShown(0);
  }, []);

  const startScan = useCallback(() => {
    clearTimers();
    setFlight(null);
    setLanded(false);
    setXmlShown(0);
    setDone(0);
    setFound(0);
    setTab("pages");
    setPopupOpen(true);
    setStage("scanning");
    for (let i = 1; i <= PAGES; i++) later(250 + i * 150, () => setFound(i));
    later(2350, () => setStage("ready"));
  }, [later]);

  const startDownload = useCallback(() => {
    clearTimers();
    setStage("downloading");
    setTab("pages");
    for (let i = 1; i <= PAGES; i++) later(i * 170, () => setDone(i));
    later(PAGES * 170 + 350, () => {
      const root = rootRef.current?.getBoundingClientRect();
      const from = popupFileRef.current?.getBoundingClientRect();
      const to = shelfSlotRef.current?.getBoundingClientRect();
      setStage("saved");
      if (root && from && to && !reduce) setFlight({ from, to, root });
      else setLanded(true);
    });
  }, [later, reduce]);

  // Plays the whole story once when it first scrolls into view, unless the visitor is already driving it.
  useEffect(() => {
    if (!inView || touched || stage !== "idle") return;
    const t = window.setTimeout(() => startScan(), reduce ? 0 : 900);
    return () => window.clearTimeout(t);
  }, [inView, touched, stage, startScan, reduce]);
  useEffect(() => {
    if (touched || stage !== "ready") return;
    const t = window.setTimeout(() => startDownload(), reduce ? 0 : 1700);
    return () => window.clearTimeout(t);
  }, [touched, stage, startDownload, reduce]);

  // Rotating status lines while scanning.
  useEffect(() => {
    if (stage !== "scanning") return;
    const t = window.setInterval(() => setScanLine((l) => (l + 1) % SCAN_LINES.length), 560);
    return () => window.clearInterval(t);
  }, [stage]);

  // ComicInfo.xml types itself out once the comic lands.
  useEffect(() => {
    if (!landed) return;
    if (xmlShown >= XML_LINES.length) return;
    const t = window.setTimeout(() => setXmlShown((n) => n + 1), reduce ? 0 : 140);
    return () => window.clearTimeout(t);
  }, [landed, xmlShown, reduce]);

  // Keep the flight path right if the layout moves while the CBZ is in the air.
  useLayoutEffect(() => {
    if (!flight) return;
    const onResize = () => {
      setFlight(null);
      setLanded(true);
    };
    window.addEventListener("resize", onResize);
    return () => window.removeEventListener("resize", onResize);
  }, [flight]);

  const crestClick = () => {
    setTouched(true);
    if (stage === "idle" || stage === "saved") startScan();
    else setPopupOpen((o) => !o);
  };
  const scanning = stage === "scanning";
  const hasResult = stage === "ready" || stage === "downloading" || stage === "saved";
  const stepIndex = STEPS.findIndex((s) => s.id.includes(stage));

  return (
    <div ref={rootRef} data-no-burst className="relative">
      {/* ----------------------------- steps ----------------------------- */}
      <ol className="mb-5 grid grid-cols-2 gap-2 sm:grid-cols-4">
        {STEPS.map((s, i) => {
          const on = i === stepIndex;
          const past = i < stepIndex;
          return (
            <li
              key={s.label}
              className={`relative flex items-center gap-2 border-[3px] border-black px-2.5 py-2 font-bangers text-lg leading-none tracking-wide shadow-[3px_3px_0_#000] transition-colors duration-300 sm:text-xl ${
                on ? "bg-amber text-black" : past ? "bg-[#2FD17A] text-black" : "bg-panel-card text-newsprint/70"
              }`}
            >
              <span className={`flex h-6 w-6 shrink-0 items-center justify-center rounded-full border-2 border-black font-mono text-[11px] font-black ${on || past ? "bg-black text-amber" : "bg-ink text-newsprint"}`}>
                {past ? <Check className="h-3.5 w-3.5 stroke-[3]" /> : i + 1}
              </span>
              {s.label}
              {on && <motion.span layoutId="ext-step" className="absolute -bottom-[7px] left-1/2 h-2 w-8 -translate-x-1/2 bg-black" />}
            </li>
          );
        })}
      </ol>

      <div className="grid gap-6 lg:grid-cols-12">
        {/* ============================ browser ============================ */}
        <div className="relative flex flex-col overflow-hidden border-[3px] border-black bg-[#e8e4dc] shadow-[9px_9px_0_#000] lg:col-span-8">
          {/* tab strip */}
          <div className="flex items-end gap-2 border-b-2 border-black bg-[#1b1b21] px-3 pt-2">
            <div className="mb-2 flex gap-1.5" aria-hidden>
              <span className="h-3 w-3 rounded-full border border-black bg-magenta" />
              <span className="h-3 w-3 rounded-full border border-black bg-amber" />
              <span className="h-3 w-3 rounded-full border border-black bg-[#2FD17A]" />
            </div>
            <div className="flex min-w-0 max-w-[260px] items-center gap-2 rounded-t-md border-2 border-b-0 border-black bg-[#e8e4dc] px-3 py-1.5 text-[11px] font-bold text-black">
              <span className="h-3 w-3 shrink-0 rotate-45 border-2 border-black bg-amber" />
              <span className="truncate">Cyberpunk Chronicles #01 · Panel House</span>
              <X className="h-3 w-3 shrink-0 opacity-60" />
            </div>
          </div>
          {/* address bar + toolbar */}
          <div className="relative z-20 flex items-center gap-2 border-b-2 border-black bg-[#f4f1ea] px-2 py-1.5 sm:px-3">
            <ChevronLeft className="hidden h-4 w-4 text-black/50 sm:block" />
            <ChevronRight className="hidden h-4 w-4 text-black/30 sm:block" />
            <div className="flex min-w-0 flex-1 items-center gap-1.5 rounded-full border-2 border-black bg-white px-3 py-1 font-mono text-[10.5px] text-black/80 sm:text-[11px]">
              <Lock className="h-3 w-3 shrink-0 text-[#1a9c56]" />
              <span className="truncate">
                panelhouse.example/<span className="text-black">read/cyberpunk-chronicles/01</span>
              </span>
            </div>
            <Puzzle className="hidden h-4 w-4 text-black/50 sm:block" />
            <motion.button
              type="button"
              onClick={crestClick}
              whileHover={{ scale: 1.1, rotate: -6 }}
              whileTap={{ scale: 0.9 }}
              aria-label="Open KomiK Downloader"
              aria-expanded={popupOpen}
              className={`relative flex h-9 w-9 shrink-0 items-center justify-center rounded-md border-2 border-black ${popupOpen ? "bg-amber" : "bg-white"}`}
            >
              {stage === "idle" && !reduce && <span className="absolute inset-0 animate-ping rounded-md bg-amber/70" />}
              <KomikLogo size={24} className="relative" />
              <span className="absolute -right-2 -top-2 flex h-[18px] min-w-[18px] items-center justify-center rounded-full border-2 border-black bg-amber px-1 font-mono text-[9px] font-black text-black">
                {PAGES}
              </span>
            </motion.button>
          </div>

          {/* page viewport */}
          <div className="relative h-[430px] shrink-0 overflow-hidden sm:h-[520px] lg:h-[560px]">
            <motion.div
              animate={{ y: scanning ? -560 : hasResult ? -120 : 0 }}
              transition={reduce ? { duration: 0 } : { duration: scanning ? 2.1 : 0.9, ease: [0.45, 0, 0.2, 1] }}
              className="px-4 pb-10 pt-4 sm:px-8"
            >
              <div className="flex items-center justify-between border-b-2 border-black pb-2">
                <span className="font-bangers text-2xl tracking-wide text-black">PANEL HOUSE</span>
                <span className="hidden gap-3 font-mono text-[10px] font-bold uppercase text-black/60 sm:flex">
                  <span className="relative">
                    Home
                    {hasResult && <motion.span initial={{ scaleX: 0 }} animate={{ scaleX: 1 }} className="absolute left-0 top-1/2 h-0.5 w-full origin-left bg-magenta" />}
                  </span>
                  <span>Latest</span>
                  <span>Genres</span>
                </span>
              </div>
              <h4 className="mt-3 font-bangers text-3xl leading-none text-black sm:text-4xl">Cyberpunk Chronicles · Chapter 1</h4>
              <div className="mt-2 grid grid-cols-[78px_1fr] gap-x-3 gap-y-1 text-[11px] text-black/80 sm:text-xs">
                <span className="font-bold">Story</span>
                <span className="font-bold text-[#0077aa]">Mira Sol</span>
                <span className="font-bold">Art</span>
                <span className="font-bold text-[#0077aa]">Dex Halloway</span>
                <span className="font-bold">Uploaded</span>
                <span>2 years 3 months ago</span>
                <span className="font-bold">Tags</span>
                <span className="flex flex-wrap gap-1">
                  {RAW_TAGS.map((t, i) => {
                    const lit = hasResult && t.clean;
                    const junk = hasResult && !t.clean;
                    return (
                      <motion.span
                        key={t.raw}
                        animate={lit ? { backgroundColor: "#FFD700", scale: [1, 1.15, 1] } : { backgroundColor: "#ffffff", scale: 1 }}
                        transition={{ delay: lit ? i * 0.05 : 0, duration: 0.35 }}
                        className={`rounded-sm border border-black/40 px-1.5 py-px font-mono text-[10px] text-black ${junk ? "line-through opacity-40" : ""}`}
                      >
                        {t.raw}
                      </motion.span>
                    );
                  })}
                </span>
              </div>

              <div className="relative mx-auto mt-5 max-w-[340px] space-y-2">
                {SITE_PAGES.map((n, i) => (
                  <div key={n} className="relative aspect-[2/3] border-2 border-black bg-white">
                    <PageThumb n={n} className="h-full w-full" />
                    {/* lazy placeholder that "loads" as KomiK scrolls past */}
                    <motion.div
                      animate={{ opacity: found > i * 3 || hasResult ? 0 : 1 }}
                      transition={{ duration: 0.4 }}
                      className="absolute inset-0 flex items-center justify-center bg-[repeating-linear-gradient(45deg,#ddd8cc_0_10px,#e9e4d9_10px_20px)]"
                    >
                      <span className="border-2 border-black bg-white px-2 py-0.5 font-mono text-[10px] font-black text-black">lazy-loading…</span>
                    </motion.div>
                    <AnimatePresence>
                      {(found > i * 3 || hasResult) && stage !== "idle" && (
                        <motion.span
                          initial={{ scale: 0, rotate: -30 }}
                          animate={{ scale: 1, rotate: -8 }}
                          exit={{ scale: 0 }}
                          className="absolute -left-2 -top-2 border-2 border-black bg-amber px-1.5 font-bangers text-lg leading-tight text-black shadow-[2px_2px_0_#000]"
                        >
                          #{i * 3 + 1}
                        </motion.span>
                      )}
                    </AnimatePresence>
                  </div>
                ))}
              </div>
            </motion.div>

            {/* scan beam */}
            <AnimatePresence>
              {scanning && !reduce && (
                <motion.div
                  initial={{ y: "-8%", opacity: 0 }}
                  animate={{ y: ["-8%", "92%", "-8%"], opacity: 1 }}
                  exit={{ opacity: 0 }}
                  transition={{ duration: 1.6, repeat: Infinity, ease: "easeInOut" }}
                  className="pointer-events-none absolute inset-0 will-change-transform"
                >
                  <div className="absolute inset-x-0 top-0 h-16 bg-[linear-gradient(180deg,transparent,rgba(255,215,0,0.45),transparent)]">
                    <div className="absolute inset-x-0 top-1/2 h-[3px] bg-amber shadow-[0_0_14px_#FFD700]" />
                  </div>
                </motion.div>
              )}
            </AnimatePresence>

            {/* floating crest on the page */}
            <motion.button
              type="button"
              onClick={crestClick}
              whileHover={{ scale: 1.08, rotate: 8 }}
              whileTap={{ scale: 0.92 }}
              className="absolute bottom-4 right-4 z-10 flex h-14 w-14 items-center justify-center rounded-full border-[3px] border-black bg-amber shadow-[4px_4px_0_#000]"
              aria-label="KomiK floating button"
            >
              <KomikLogo size={34} />
              <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full border-2 border-black bg-magenta px-1 font-mono text-[10px] font-black text-white">
                {PAGES}
              </span>
            </motion.button>

            <AnimatePresence>
              {stage === "idle" && (
                <motion.div
                  initial={{ opacity: 0, y: 10, scale: 0.9 }}
                  animate={{ opacity: 1, y: 0, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.8 }}
                  className="balloon pointer-events-none absolute bottom-24 right-4 z-10 px-3 py-1.5 text-[11px]"
                >
                  <MousePointerClick className="mr-1 inline h-3.5 w-3.5" /> Click the crest!
                </motion.div>
              )}
            </AnimatePresence>

            {/* saved toast */}
            <AnimatePresence>
              {stage === "saved" && (
                <motion.div
                  initial={{ y: 80, opacity: 0 }}
                  animate={{ y: 0, opacity: 1 }}
                  exit={{ y: 80, opacity: 0 }}
                  transition={{ type: "spring", stiffness: 320, damping: 24, delay: 0.3 }}
                  className="absolute bottom-4 left-4 right-24 z-10 flex items-center gap-2 border-[3px] border-black bg-black px-3 py-2 text-white shadow-[4px_4px_0_#FFD700] sm:right-auto sm:max-w-sm"
                >
                  <FolderOpen className="h-5 w-5 shrink-0 text-amber" />
                  <div className="min-w-0">
                    <div className="font-bangers text-lg leading-none tracking-wide text-amber">SAVED!</div>
                    <div className="truncate font-mono text-[10px]">Downloads/KomiK/Cyberpunk Chronicles/{COMIC_TITLE}.cbz</div>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>

            {/* ============================ popup ============================ */}
            <AnimatePresence>
              {popupOpen && (
                <motion.div
                  initial={{ opacity: 0, scale: 0.6, y: -20 }}
                  animate={{ opacity: 1, scale: 1, y: 0 }}
                  exit={{ opacity: 0, scale: 0.7, y: -16 }}
                  transition={{ type: "spring", stiffness: 380, damping: 28 }}
                  style={{ transformOrigin: "calc(100% - 22px) 0" }}
                  className="absolute inset-x-2 top-2 z-30 flex max-h-[calc(100%-16px)] flex-col overflow-hidden border-[3px] border-black bg-[#fff8e7] text-black shadow-[7px_7px_0_#000] sm:inset-x-auto sm:right-3 sm:w-[320px]"
                >
                  <div className="relative flex items-center justify-between border-b-[3px] border-black bg-white px-3 py-2">
                    <div className="bg-speedlines pointer-events-none absolute inset-0 opacity-60" />
                    <div className="relative flex items-center gap-1.5">
                      <KomikLogo size={24} />
                      <div className="leading-none">
                        <div className="font-bangers text-lg tracking-wide text-amber [-webkit-text-stroke:1px_#000] [paint-order:stroke_fill]">KOMIK</div>
                        <div className="font-mono text-[7px] font-bold uppercase tracking-[0.25em] text-black/60">Downloader</div>
                      </div>
                    </div>
                    <button type="button" onClick={() => { setTouched(true); setPopupOpen(false); }} className="relative flex h-6 w-6 items-center justify-center border-2 border-black bg-white" aria-label="Close popup">
                      <X className="h-3.5 w-3.5" />
                    </button>
                  </div>

                  <div className="min-h-0 flex-1 overflow-hidden p-2.5">
                    <AnimatePresence mode="wait">
                      {scanning ? (
                        <motion.div key="scan" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0, scale: 0.9 }} className="flex flex-col items-center py-6">
                          <motion.div animate={reduce ? undefined : { rotate: 360 }} transition={{ duration: 6, repeat: Infinity, ease: "linear" }}>
                            <ComicBurst size={118} fill="#FFD700" spikes={16} seed={4}>
                              <motion.span
                                animate={reduce ? undefined : { scale: [1, 1.15, 1] }}
                                transition={{ repeat: Infinity, duration: 0.8 }}
                                className="block font-bangers text-3xl leading-none text-white [-webkit-text-stroke:2px_#000] [paint-order:stroke_fill]"
                              >
                                SCAN!
                              </motion.span>
                            </ComicBurst>
                          </motion.div>
                          <AnimatePresence mode="wait">
                            <motion.p key={scanLine} initial={{ y: 8, opacity: 0 }} animate={{ y: 0, opacity: 1 }} exit={{ y: -8, opacity: 0 }} className="mt-4 font-comic text-sm font-bold">
                              {SCAN_LINES[scanLine]}
                            </motion.p>
                          </AnimatePresence>
                          <div className="mt-2 font-mono text-[11px] font-black">
                            <span className="text-magenta">{found}</span> / {PAGES} pages found
                          </div>
                        </motion.div>
                      ) : hasResult ? (
                        <motion.div key="result" initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} className="flex h-full flex-col">
                          {/* hero */}
                          <div className="relative flex gap-2.5 border-[3px] border-black bg-white p-2 shadow-[3px_3px_0_#000]">
                            <div className="bg-halftone-paper pointer-events-none absolute inset-0 opacity-40" />
                            <motion.div initial={{ rotate: -12, scale: 0.6 }} animate={{ rotate: -3, scale: 1 }} transition={{ type: "spring", stiffness: 300, damping: 14 }} className="relative w-[52px] shrink-0 border-2 border-black shadow-[2px_2px_0_#000]">
                              <PageThumb n={1} className="aspect-[2/3]" />
                            </motion.div>
                            <div className="relative min-w-0 flex-1">
                              <motion.span initial={{ scale: 0 }} animate={{ scale: 1, rotate: -2 }} transition={{ type: "spring", stiffness: 500, damping: 14, delay: 0.15 }} className="inline-block rounded-full border-2 border-black bg-[#2FD17A] px-1.5 font-bangers text-[11px] leading-[16px] tracking-wide">
                                PERFECT MATCH!
                              </motion.span>
                              <div className="mt-0.5 truncate font-bangers text-lg leading-none tracking-wide">{COMIC_TITLE}</div>
                              <div className="truncate text-[10px] font-semibold text-black/60">panelhouse.example · Mira Sol</div>
                              <div className="mt-1 flex flex-wrap gap-0.5">
                                {CLEAN_TAGS.slice(0, 4).map((t, i) => (
                                  <motion.span
                                    key={t}
                                    initial={{ y: -30, opacity: 0, rotate: -20 }}
                                    animate={{ y: 0, opacity: 1, rotate: 0 }}
                                    transition={{ type: "spring", stiffness: 420, damping: 16, delay: 0.25 + i * 0.07 }}
                                    className="rounded-full border-2 border-black bg-[#fff8e7] px-1 text-[9px] font-bold leading-[13px]"
                                  >
                                    {t}
                                  </motion.span>
                                ))}
                              </div>
                            </div>
                          </div>

                          {/* tabs */}
                          <div className="mt-2.5 flex gap-1">
                            {(["pages", "details"] as const).map((id) => (
                              <button
                                key={id}
                                type="button"
                                onClick={() => { setTouched(true); setTab(id); }}
                                className={`flex-1 border-2 border-black px-2 py-1 font-bangers text-sm tracking-wide shadow-[2px_2px_0_#000] ${tab === id ? "bg-amber" : "bg-white hover:bg-cyan/30"}`}
                              >
                                {id === "pages" ? `Pages · ${PAGES}` : `Details · ${CLEAN_TAGS.length} tags`}
                              </button>
                            ))}
                          </div>

                          <div className="relative mt-2 min-h-0 flex-1 overflow-hidden border-2 border-black bg-white p-2">
                            <AnimatePresence mode="wait">
                              {tab === "pages" ? (
                                <motion.div key="pages" initial={{ opacity: 0, x: 12 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -12 }} className="relative grid grid-cols-4 gap-1.5">
                                  <AnimatePresence>
                                    {stage === "saved" && (
                                      <motion.div
                                        initial={{ scale: 0, rotate: -20 }}
                                        animate={{ scale: 1, rotate: -6 }}
                                        transition={{ type: "spring", stiffness: 380, damping: 14, delay: 0.45 }}
                                        className="absolute inset-0 z-10 flex flex-col items-center justify-center"
                                      >
                                        <ComicBurst size={130} fill="#2FD17A" spikes={14} seed={11}>
                                          <span className="block font-bangers text-2xl leading-none text-black">
                                            PACKED!
                                            <br />
                                            <span className="text-sm">{PAGES} pages + XML</span>
                                          </span>
                                        </ComicBurst>
                                      </motion.div>
                                    )}
                                  </AnimatePresence>
                                  {THUMBS.map((n, i) => {
                                    const packed = stage === "saved";
                                    return (
                                      <motion.div
                                        key={n}
                                        initial={{ scale: 0.4, opacity: 0 }}
                                        animate={packed && !reduce ? { scale: 0.2, opacity: 0, y: 120, x: (1.5 - (i % 4)) * 40 } : { scale: 1, opacity: 1, y: 0, x: 0 }}
                                        transition={packed ? { duration: 0.5, delay: i * 0.03, ease: "backIn" } : { type: "spring", stiffness: 420, damping: 22, delay: i * 0.035 }}
                                        className="relative border-2 border-black"
                                      >
                                        <PageThumb n={n} className="aspect-[2/3]" />
                                        <span className="absolute bottom-0 left-0 border-r-2 border-t-2 border-black bg-amber px-0.5 font-mono text-[8px] font-black">{n}</span>
                                        {done >= n && (
                                          <motion.span initial={{ scale: 0 }} animate={{ scale: 1 }} className="absolute right-0.5 top-0.5 flex h-4 w-4 items-center justify-center rounded-full border-2 border-black bg-[#2FD17A]">
                                            <Check className="h-2.5 w-2.5 stroke-[4]" />
                                          </motion.span>
                                        )}
                                      </motion.div>
                                    );
                                  })}
                                </motion.div>
                              ) : (
                                <motion.div key="details" initial={{ opacity: 0, x: 12 }} animate={{ opacity: 1, x: 0 }} exit={{ opacity: 0, x: -12 }} className="space-y-1.5 text-[11px]">
                                  <div className="border-2 border-black bg-[#e6f8ff] p-1.5">
                                    <div className="font-bangers text-sm tracking-wide">KOMIK WILL READ IT AS</div>
                                    <div className="font-bold">{COMIC_TITLE}</div>
                                    <div className="text-black/60">by Mira Sol · art Dex Halloway · 2024</div>
                                  </div>
                                  <div className="font-mono text-[9px] font-black uppercase text-black/60">Tags (cleaned)</div>
                                  <div className="flex flex-wrap gap-1">
                                    {CLEAN_TAGS.map((t, i) => (
                                      <motion.span key={t} initial={{ scale: 0 }} animate={{ scale: 1 }} transition={{ delay: i * 0.05, type: "spring", stiffness: 500, damping: 18 }} className="rounded-full border-2 border-black bg-amber px-1.5 font-bold">
                                        {t}
                                      </motion.span>
                                    ))}
                                  </div>
                                </motion.div>
                              )}
                            </AnimatePresence>
                          </div>

                          {/* download bar */}
                          <div className="mt-2.5 flex items-stretch gap-1.5">
                            <div ref={popupFileRef} className="flex items-center gap-1 border-2 border-black bg-cyan px-2 font-bangers text-sm tracking-wide shadow-[2px_2px_0_#000]">
                              <FileArchive className="h-4 w-4" /> CBZ
                            </div>
                            <motion.button
                              type="button"
                              disabled={stage !== "ready"}
                              onClick={() => { setTouched(true); startDownload(); }}
                              whileHover={stage === "ready" ? { scale: 1.04, rotate: -1 } : undefined}
                              whileTap={stage === "ready" ? { scale: 0.95 } : undefined}
                              className="relative flex-1 overflow-hidden border-[3px] border-black bg-magenta px-2 py-1.5 font-bangers text-lg tracking-wider text-white shadow-[3px_3px_0_#000] [-webkit-text-stroke:1px_#000] [paint-order:stroke_fill] disabled:cursor-default"
                            >
                              {stage === "downloading" && (
                                <motion.span className="absolute inset-0 origin-left bg-[#2FD17A]" initial={{ scaleX: 0 }} animate={{ scaleX: done / PAGES }} transition={{ duration: 0.15 }} />
                              )}
                              <span className="relative">
                                {stage === "ready" ? `GET ${PAGES} PAGES` : stage === "downloading" ? `PAGE ${done}/${PAGES} · ${(2.4 + done * 0.17).toFixed(1)} MB/S` : "DONE!"}
                              </span>
                            </motion.button>
                          </div>
                        </motion.div>
                      ) : null}
                    </AnimatePresence>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        </div>

        {/* ============================= shelf ============================= */}
        <div className="relative flex flex-col border-[3px] border-black bg-[#121216] text-newsprint shadow-[9px_9px_0_#000] lg:col-span-4">
          <div className="flex items-center justify-between border-b-[3px] border-black bg-[#1b1b21] px-3 py-2">
            <span className="flex items-center gap-2">
              <KomikLogo size={20} />
              <span className="font-bangers text-lg tracking-wide text-white">Komik · Library</span>
            </span>
            <span className="border-2 border-black bg-amber px-1.5 font-mono text-[9px] font-black uppercase text-black">v1.2.0</span>
          </div>
          <div className="flex items-center gap-2 border-b-2 border-black/60 bg-black/40 px-3 py-1.5 font-mono text-[10px] text-white/70">
            <span className="inline-block h-2.5 w-2.5 animate-spin rounded-full border-2 border-cyan border-t-transparent [animation-duration:2s]" />
            Watching Downloads/KomiK…
          </div>

          <div className="grid grid-cols-4 content-start gap-2 p-3">
            {SHELF.map((c) => (
              <div key={c.t} className="relative flex aspect-[2/3] flex-col justify-between border-2 border-black p-1 shadow-[2px_2px_0_#000]" style={{ background: c.c }}>
                <div className="bg-halftone-paper pointer-events-none absolute inset-0 opacity-50" />
                <span className="relative font-bangers text-[13px] leading-none text-black">{c.t}</span>
                <span className="relative self-end border border-black bg-white px-0.5 font-mono text-[8px] font-black text-black">{c.v}</span>
              </div>
            ))}

            {/* the slot the new comic lands in */}
            <div ref={shelfSlotRef} className="relative aspect-[2/3]">
              <AnimatePresence>
                {!landed ? (
                  <motion.div key="empty" exit={{ opacity: 0 }} className="flex h-full items-center justify-center border-2 border-dashed border-white/30 text-center font-mono text-[8.5px] leading-tight text-white/40">
                    next
                    <br />
                    comic
                  </motion.div>
                ) : (
                  <motion.div
                    key="new"
                    initial={{ scale: 1.6, rotate: -16, opacity: 0 }}
                    animate={{ scale: 1, rotate: -2, opacity: 1 }}
                    transition={{ type: "spring", stiffness: 360, damping: 15 }}
                    className="relative h-full border-2 border-black shadow-[3px_3px_0_#FFD700]"
                  >
                    <PageThumb n={1} className="h-full w-full" />
                    <motion.span initial={{ scale: 0, rotate: 30 }} animate={{ scale: 1, rotate: 12 }} transition={{ delay: 0.25, type: "spring", stiffness: 500, damping: 12 }} className="absolute -right-2.5 -top-3 z-10 flex h-8 w-8 items-center justify-center rounded-full border-2 border-black bg-magenta font-bangers text-[11px] leading-none text-white">
                      NEW!
                    </motion.span>

                  </motion.div>
                )}
              </AnimatePresence>
            </div>
          </div>

          <div className="mx-3 mb-3 flex min-h-[30px] items-center">
            <AnimatePresence mode="wait">
              {landed ? (
                <motion.div key="added" initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} className="flex w-full items-center gap-2 border-2 border-black bg-amber px-2 py-1 font-mono text-[10px] font-black text-black">
                  <Star className="h-3 w-3 shrink-0 fill-black" /> Added: {COMIC_TITLE} · {PAGES} pages · {CLEAN_TAGS.length} tags
                </motion.div>
              ) : (
                <motion.div key="wait" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="font-mono text-[10px] text-white/45">
                  New comics appear here with their details and tags.
                </motion.div>
              )}
            </AnimatePresence>
          </div>

          {/* ComicInfo.xml receipt */}
          <div className="relative mx-3 mb-3 min-h-[170px] flex-1 overflow-hidden border-[3px] border-black bg-paper-grain p-3 font-mono text-[10px] leading-[1.6] text-black shadow-[4px_4px_0_#000] sm:text-[11.5px]">
            <div className="mb-1 flex items-center justify-between border-b-2 border-dashed border-black/40 pb-1 font-sans text-[10px] font-black uppercase">
              <span>ComicInfo.xml</span>
              <span className="text-black/50">inside the .cbz</span>
            </div>
            {landed ? (
              XML_LINES.slice(0, xmlShown).map((l, i) => (
                <motion.div key={i} initial={{ opacity: 0, x: -8 }} animate={{ opacity: 1, x: 0 }} className="break-words">
                  <span className="text-[#b0006a]">{l.match(/^<\w+>/)?.[0]}</span>
                  <span>{l.replace(/^<\w+>|<\/\w+>$/g, "")}</span>
                  <span className="text-[#b0006a]">{l.match(/<\/\w+>$/)?.[0]}</span>
                </motion.div>
              ))
            ) : (
              <p className="pt-6 text-center font-sans text-[11px] font-bold text-black/50">Title, credits and every tag get written in here the moment the comic is saved.</p>
            )}
          </div>

          <div className="flex gap-2 border-t-[3px] border-black p-3">
            <button
              type="button"
              onClick={() => { setTouched(true); reset(); later(80, startScan); }}
              className="btn-comic-primary inline-flex flex-1 items-center justify-center gap-2 px-3 py-2 text-xs uppercase"
            >
              {stage === "idle" ? <Zap className="h-4 w-4" /> : <RotateCcw className="h-4 w-4" />}
              {stage === "idle" ? "Run the demo" : "Replay"}
            </button>
          </div>
        </div>
      </div>

      {/* flying CBZ */}
      <AnimatePresence>
        {flight && (
          <motion.div
            key="flight"
            className="pointer-events-none absolute z-40 flex items-center gap-1 border-[3px] border-black bg-cyan px-2 py-1 font-bangers text-lg tracking-wide text-black shadow-[4px_4px_0_#000]"
            initial={{ left: flight.from.left - flight.root.left, top: flight.from.top - flight.root.top, rotate: 0, scale: 1 }}
            animate={{
              left: [flight.from.left - flight.root.left, (flight.from.left + flight.to.left) / 2 - flight.root.left, flight.to.left - flight.root.left + flight.to.width / 2 - 40],
              top: [flight.from.top - flight.root.top, Math.min(flight.from.top, flight.to.top) - flight.root.top - 90, flight.to.top - flight.root.top + flight.to.height / 2 - 18],
              rotate: [0, -25, 360],
              scale: [1, 1.5, 0.6],
            }}
            exit={{ opacity: 0, scale: 0 }}
            transition={{ duration: 1.05, ease: "easeInOut", times: [0, 0.45, 1] }}
            onAnimationComplete={() => {
              setFlight(null);
              setLanded(true);
            }}
          >
            <FileArchive className="h-5 w-5" /> .CBZ
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
