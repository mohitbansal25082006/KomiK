"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { AnimatePresence, LayoutGroup, motion, useInView } from "framer-motion";
import { ArrowLeftRight, Bookmark, BookOpen, Cpu, Moon, Search, ScrollText, Sparkles } from "lucide-react";
import SectionHeading from "@/components/fx/SectionHeading";
import { ComicPage } from "@/components/comic/pages";
import { gsap, ScrollTrigger, useGSAP } from "@/lib/gsap";
import { PRESETS, cssFilter } from "@/components/reader/readerModel";

/** True while the demo is on (or near) the screen, so offscreen loops stop working. */
function useActive<T extends Element>() {
  const ref = useRef<T>(null);
  const active = useInView(ref, { margin: "200px 0px" });
  return [ref, active] as const;
}

/* ----------------------------- Demo 1 ----------------------------- */

function SpreadDemo() {
  const [rtl, setRtl] = useState(false);
  const [auto, setAuto] = useState(true);
  const [ref, active] = useActive<HTMLDivElement>();
  useEffect(() => {
    if (!auto || !active) return;
    const t = setInterval(() => setRtl((v) => !v), 2800);
    return () => clearInterval(t);
  }, [auto, active]);
  const pairs = [[1], [2, 3], [4, 5]];
  return (
    <div ref={ref}>
      <div className="mb-3 flex flex-wrap items-center gap-2">
        {(["ltr", "rtl"] as const).map((d) => (
          <button
            key={d}
            type="button"
            onClick={() => {
              setAuto(false);
              setRtl(d === "rtl");
            }}
            className={`border-2 border-black px-2.5 py-1 font-mono text-[11px] font-black uppercase shadow-[2px_2px_0_#000] ${(d === "rtl") === rtl ? "bg-amber text-black" : "bg-panel-card text-newsprint"}`}
          >
            {d === "ltr" ? "Western LTR" : "Manga RTL"}
          </button>
        ))}
      </div>
      <LayoutGroup>
        <div className="flex flex-wrap items-end gap-3 sm:gap-4">
          {pairs.map((pair, pi) => {
            const row: (number | null)[] = pair.length === 1 ? [null, pair[0]] : [...pair];
            const ordered = rtl ? [...row].reverse() : row;
            return (
              <div key={pi} className="flex flex-col items-center gap-1">
                <div className="flex gap-0.5 border-[3px] border-black bg-black p-1 shadow-[4px_4px_0_#000]">
                  {ordered.map((n, i) =>
                    n === null ? (
                      <motion.div layout key={`blank-${pi}`} className="h-[84px] w-[56px] bg-panel-card sm:h-[108px] sm:w-[72px] xl:h-[150px] xl:w-[100px]" />
                    ) : (
                      <motion.div layout key={n} transition={{ type: "spring", stiffness: 300, damping: 26 }} className="relative h-[84px] w-[56px] overflow-hidden bg-white sm:h-[108px] sm:w-[72px] xl:h-[150px] xl:w-[100px]">
                        <ComicPage n={n} />
                        <span className="absolute bottom-0.5 left-0.5 bg-black px-1 font-mono text-[9px] font-bold text-amber">{n}</span>
                        {i === (rtl ? ordered.length - 1 : 0) && pair.length === 2 && (
                          <span className="absolute right-0.5 top-0.5 bg-magenta px-1 font-mono text-[8px] font-bold text-white">1st</span>
                        )}
                      </motion.div>
                    ),
                  )}
                </div>
                <span className="font-mono text-[10px] font-bold text-muted">{pair.length === 1 ? "cover, isolated" : `spread ${pair.join("–")}`}</span>
              </div>
            );
          })}
        </div>
      </LayoutGroup>
    </div>
  );
}

/* ----------------------------- Demo 2 ----------------------------- */

function WebtoonDemo() {
  const [tick, setTick] = useState(0);
  const [ref, active] = useActive<HTMLDivElement>();
  useEffect(() => {
    if (!active) return;
    const t = setInterval(() => setTick((v) => v + 1), 900);
    return () => clearInterval(t);
  }, [active]);
  const center = 5 + (tick % 4);
  const order = [0, 1, -1, 2, -2, 3];
  return (
    <div ref={ref} className="grid grid-cols-[110px_1fr] gap-4 sm:grid-cols-[140px_1fr] xl:grid-cols-[190px_1fr]">
      <div className="relative h-[240px] overflow-hidden xl:h-[380px] border-[3px] border-black bg-black shadow-[4px_4px_0_#000]">
        <motion.div
          className="absolute inset-x-0 top-0 flex flex-col gap-1 p-1"
          style={{ willChange: "transform" }}
          animate={active ? { y: ["0%", "-50%"] } : undefined}
          transition={{ duration: 14, repeat: Infinity, ease: "linear" }}
        >
          {[2, 3, 4, 5, 6, 7, 8, 9, 2, 3, 4, 5, 6, 7, 8, 9].map((n, i) => (
            <div key={i} className="aspect-[2/3] w-full shrink-0 overflow-hidden bg-white">
              <ComicPage n={n} />
            </div>
          ))}
        </motion.div>
        <div className="pointer-events-none absolute inset-x-0 top-1/3 h-1/3 border-y-2 border-dashed border-amber/80 bg-amber/10" />
      </div>
      <div className="space-y-1.5 font-mono text-[11px]">
        {order.map((off, w) => {
          const p = center + off;
          return (
            <div key={w} className="flex items-center gap-2">
              <span className="w-16 shrink-0 font-bold text-muted">worker {w + 1}</span>
              <div className="relative h-5 flex-1 overflow-hidden border-2 border-black bg-panel-card">
                <motion.div
                  key={`${tick}-${w}`}
                  initial={{ width: "0%" }}
                  animate={{ width: "100%" }}
                  transition={{ duration: 0.5 + w * 0.08, ease: "easeOut", delay: w * 0.04 }}
                  className={`h-full ${w === 0 ? "bg-amber" : w < 3 ? "bg-cyan" : "bg-magenta"}`}
                />
                <span className="absolute inset-0 flex items-center px-1.5 font-bold text-black mix-blend-normal">
                  <span className="rounded-sm bg-white/80 px-1">p.{p}</span>
                </span>
              </div>
            </div>
          );
        })}
        <p className="pt-2 leading-snug text-newsprint/70">
          <span className="text-amber">SemaphoreSlim(6)</span> decodes outward from the viewport, off the UI thread.
        </p>
      </div>
    </div>
  );
}

/* ----------------------------- Demo 3 ----------------------------- */

function LutDemo() {
  const [pos, setPos] = useState(52);
  const [preset, setPreset] = useState<(typeof PRESETS)[number]["id"]>("night");
  const p = PRESETS.find((x) => x.id === preset)!;
  const filter = cssFilter({ preset: p.id, brightness: p.brightness, contrast: p.contrast, warmth: p.warmth });
  const boxRef = useRef<HTMLDivElement>(null);
  const drag = (clientX: number) => {
    const r = boxRef.current!.getBoundingClientRect();
    setPos(Math.max(0, Math.min(100, ((clientX - r.left) / r.width) * 100)));
  };
  return (
    <div className="grid gap-4 sm:grid-cols-[1fr_150px]">
      <div
        ref={boxRef}
        data-no-burst
        className="relative aspect-[3/2] cursor-ew-resize touch-none select-none overflow-hidden border-[3px] border-black bg-black shadow-[4px_4px_0_#000]"
        onPointerDown={(e) => {
          e.currentTarget.setPointerCapture(e.pointerId);
          drag(e.clientX);
        }}
        onPointerMove={(e) => e.buttons && drag(e.clientX)}
      >
        <div className="absolute inset-x-0 top-[-38%] aspect-[2/3]">
          <ComicPage n={9} />
        </div>
        <div className="absolute inset-0" style={{ clipPath: `inset(0 0 0 ${pos}%)` }}>
          <div className="absolute inset-x-0 top-[-38%] aspect-[2/3]" style={{ filter }}>
            <ComicPage n={9} />
          </div>
        </div>
        <div className="absolute inset-y-0 w-1 -translate-x-1/2 bg-amber" style={{ left: `${pos}%` }}>
          <div className="absolute top-1/2 flex h-9 w-9 -translate-x-[40%] -translate-y-1/2 items-center justify-center rounded-full border-[3px] border-black bg-amber text-black">
            <ArrowLeftRight className="h-4 w-4" />
          </div>
        </div>
        <span className="absolute left-2 top-2 border-2 border-black bg-white px-1.5 font-mono text-[10px] font-black text-black">ORIGINAL</span>
        <span className="absolute right-2 top-2 border-2 border-black bg-amber px-1.5 font-mono text-[10px] font-black uppercase text-black">{p.label}</span>
        <input aria-label="Before and after comparison" type="range" min={0} max={100} value={pos} onChange={(e) => setPos(+e.target.value)} className="sr-only" />
      </div>
      <div className="grid grid-cols-3 gap-1.5 sm:grid-cols-2">
        {PRESETS.filter((x) => x.id !== "original").map((x) => (
          <button
            key={x.id}
            type="button"
            onClick={() => setPreset(x.id)}
            className={`flex items-center gap-1.5 border-2 border-black px-1.5 py-1.5 text-[11px] font-bold shadow-[2px_2px_0_#000] ${preset === x.id ? "bg-amber text-black" : "bg-panel-card text-newsprint"}`}
          >
            <span className="h-3.5 w-3.5 shrink-0 rounded-full border border-black" style={{ background: x.swatch }} />
            {x.label}
          </button>
        ))}
      </div>
    </div>
  );
}

/* ----------------------------- Demo 4 ----------------------------- */

const LIBRARY = [
  { t: "Cyberpunk Chronicles #01", c: "#FF1F6D", s: "in-progress" },
  { t: "Cyberpunk Chronicles #02", c: "#B3124F", s: "unread" },
  { t: "Neon Ronin Vol. 1", c: "#00C2FF", s: "done" },
  { t: "Neon Ronin Vol. 2", c: "#0089B5", s: "in-progress" },
  { t: "Paper Moon Detective", c: "#FFD700", s: "unread" },
  { t: "Quiet Robots", c: "#2FD17A", s: "done" },
  { t: "Ink & Thunder", c: "#FF7A00", s: "unread" },
  { t: "Starlight Courier", c: "#A78BFA", s: "in-progress" },
];
const QUERIES = ["chronicles", "neon", "vol", "robots", ""];

function LibraryDemo() {
  const [qi, setQi] = useState(0);
  const [typed, setTyped] = useState("");
  const [ref, active] = useActive<HTMLDivElement>();
  useEffect(() => {
    if (!active) return;
    const target = QUERIES[qi];
    let i = 0;
    const type = setInterval(() => {
      i++;
      setTyped(target.slice(0, i));
      if (i >= target.length) clearInterval(type);
    }, 110);
    const next = setTimeout(() => setQi((v) => (v + 1) % QUERIES.length), target.length * 110 + 1700);
    return () => {
      clearInterval(type);
      clearTimeout(next);
    };
  }, [qi, active]);
  const results = useMemo(() => LIBRARY.filter((b) => b.t.toLowerCase().includes(typed.toLowerCase())), [typed]);
  return (
    <div ref={ref}>
      <div className="mb-3 flex items-center gap-2 border-[3px] border-black bg-black px-3 py-2 font-mono text-[12px] shadow-[3px_3px_0_#000]">
        <Search className="h-4 w-4 text-amber" />
        <span className="text-newsprint">
          {typed}
          <span className="animate-pulse text-amber">▌</span>
        </span>
        <span className="ml-auto text-[10px] font-bold text-amber">
          {results.length} match{results.length === 1 ? "" : "es"} · SQLite
        </span>
      </div>
      <motion.div layout className="grid grid-cols-4 gap-2">
        <AnimatePresence mode="popLayout">
          {results.map((b) => (
            <motion.div
              layout
              key={b.t}
              initial={{ opacity: 0, scale: 0.6 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.6 }}
              transition={{ type: "spring", stiffness: 400, damping: 30 }}
              className="relative aspect-[2/3] overflow-hidden border-2 border-black p-1 shadow-[2px_2px_0_#000]"
              style={{ background: b.c }}
            >
              <div className="bg-halftone-paper absolute inset-0 opacity-50" />
              <div className="relative font-bangers text-[11px] leading-none text-black sm:text-[13px]">{b.t}</div>
              <div
                className={`absolute inset-x-0 bottom-0 h-1.5 border-t border-black ${b.s === "done" ? "bg-[#2FD17A]" : b.s === "in-progress" ? "bg-white" : "bg-black/40"}`}
                style={b.s === "in-progress" ? { background: "linear-gradient(90deg,#fff 55%,#0005 55%)" } : undefined}
              />
            </motion.div>
          ))}
        </AnimatePresence>
      </motion.div>
    </div>
  );
}

/* ----------------------------- Demo 5 ----------------------------- */

function ResumeDemo() {
  const [cycle, setCycle] = useState(0);
  const [ref, active] = useActive<HTMLDivElement>();
  useEffect(() => {
    if (!active) return;
    const t = setInterval(() => setCycle((c) => c + 1), 3200);
    return () => clearInterval(t);
  }, [active]);
  return (
    <div ref={ref} className="relative flex h-[240px] items-center justify-center overflow-hidden border-[3px] xl:h-[380px] border-black bg-[#0f0f12] shadow-[4px_4px_0_#000]">
      <div className="bg-halftone absolute inset-0 opacity-20" />
      <div className="relative flex gap-1">
        {[6, 7].map((n) => (
          <div key={n} className="relative h-[170px] w-[113px] overflow-hidden bg-white shadow-lg xl:h-[270px] xl:w-[180px]">
            <ComicPage n={n} />
            {n === 7 && (
              <motion.div key={cycle} initial={{ y: -60 }} animate={{ y: 0 }} transition={{ type: "spring", stiffness: 300, damping: 14, delay: 0.9 }} className="absolute right-2 top-0">
                <Bookmark className="h-8 w-8 fill-[#F59E0B] text-black" />
              </motion.div>
            )}
          </div>
        ))}
      </div>
      <AnimatePresence mode="wait">
        <motion.div
          key={cycle}
          initial={{ y: 40, opacity: 0 }}
          animate={{ y: 0, opacity: 1 }}
          exit={{ y: 20, opacity: 0 }}
          transition={{ type: "spring", stiffness: 300, damping: 24 }}
          className="absolute bottom-3 flex items-center gap-2 rounded-full border-2 border-black bg-paper px-3 py-1.5 text-[12px] font-bold text-black shadow-[3px_3px_0_#000]"
        >
          <span className="h-2 w-2 rounded-full bg-amber ring-2 ring-black" /> Resumed at page 6
        </motion.div>
      </AnimatePresence>
      <motion.div
        key={`note-${cycle}`}
        initial={{ scale: 0, rotate: -20 }}
        animate={{ scale: 1, rotate: 6 }}
        transition={{ delay: 1.4, type: "spring", stiffness: 400, damping: 12 }}
        className="balloon absolute right-3 top-3 px-3 py-1 text-[11px]"
      >
        note: best glide ever!
      </motion.div>
    </div>
  );
}

/* ----------------------------- Panels ----------------------------- */

const PANELS = [
  {
    no: "3.1",
    icon: BookOpen,
    tone: "bg-amber",
    title: "Spreads that respect the book",
    text: "Page 1 stays an isolated cover, then interior pages pair 2–3, 4–5 exactly like a printed comic. Reading manga? Ctrl+R flips page order, pairing and arrow keys to right-to-left.",
    chips: ["Cover isolation", "Manga RTL", "Fit W / H / 1:1", "Zoom & pan"],
    Demo: SpreadDemo,
  },
  {
    no: "3.2",
    icon: ScrollText,
    tone: "bg-cyan",
    title: "A webtoon engine with six hands",
    text: "Press V for continuous vertical reading. Six parallel workers decode high-resolution pages outward from wherever you are, so fast scrolling never hits a blank page.",
    chips: ["6-worker pool", "Outward priority", "Smooth wheel & keys", "Animated page jumps"],
    Demo: WebtoonDemo,
  },
  {
    no: "3.3",
    icon: Moon,
    tone: "bg-magenta text-white",
    title: "Night Mode & six LUT presets",
    text: "A 256-entry lookup table re-colors pages on the fly: Night, Sepia, High Contrast, Grayscale and Invert, plus brightness, contrast and warmth sliders. Drag the divider to compare.",
    chips: ["Non-destructive", "Brightness ±100", "Contrast 0.5–2.0", "Warmth ±100"],
    Demo: LutDemo,
  },
  {
    no: "3.4",
    icon: Search,
    tone: "bg-paper",
    title: "A library that files itself",
    text: "Point Komik at your comic folders. It indexes them recursively into local SQLite, caches covers on disk, and searches and filters in real time. Removing an entry never touches your files.",
    chips: ["Watched folders", "Grid & list views", "Favorites · In-progress · Unread", "Thumbnail cache"],
    Demo: LibraryDemo,
  },
  {
    no: "3.5",
    icon: Bookmark,
    tone: "bg-amber",
    title: "Right where you left off",
    text: "Every page turn is saved locally. Reopen a comic and a quiet toast tells you where you resumed. Bookmark favorite scenes with notes using Ctrl+D.",
    chips: ["Auto progress", "Resume toast", "Bookmark notes", "Scrubber previews"],
    Demo: ResumeDemo,
  },
];

export default function ReadingEngine() {
  const pinRef = useRef<HTMLDivElement>(null);
  const trackRef = useRef<HTMLDivElement>(null);
  const barRef = useRef<HTMLDivElement>(null);

  useGSAP(
    () => {
      const mm = gsap.matchMedia();
      mm.add("(min-width: 1024px) and (prefers-reduced-motion: no-preference)", () => {
        const track = trackRef.current!;
        const distance = () => track.scrollWidth - window.innerWidth + 48;
        const tween = gsap.to(track, {
          x: () => -distance(),
          ease: "none",
          scrollTrigger: {
            trigger: pinRef.current,
            start: "top top",
            end: () => `+=${distance()}`,
            pin: true,
            scrub: 0.45,
            invalidateOnRefresh: true,
            anticipatePin: 1,
            onUpdate: (self) => {
              if (barRef.current) barRef.current.style.transform = `scaleX(${self.progress})`;
            },
          },
        });
        // panels tilt slightly as they travel for a hand-held comic feel
        gsap.utils.toArray<HTMLElement>("[data-strip-panel]").forEach((el, i) => {
          gsap.fromTo(
            el,
            { rotate: i % 2 ? 2.5 : -2.5, y: 30 },
            {
              rotate: 0,
              y: 0,
              ease: "none",
              scrollTrigger: { trigger: el, containerAnimation: tween, start: "left right", end: "center center", scrub: true },
            },
          );
        });
        return () => ScrollTrigger.refresh();
      });
      return () => mm.revert();
    },
    { scope: pinRef },
  );

  return (
    <section id="reading-engine" className="relative scroll-mt-16 border-b-[3px] border-black bg-ink">
      <div className="bg-halftone pointer-events-none absolute inset-0 opacity-20" />
      <div className="relative mx-auto max-w-7xl px-4 pb-6 pt-20 sm:px-6 sm:pt-28 lg:px-8">
        <SectionHeading
          caption="Chapter 03 · The reading engine"
          title="Engineered for the page."
          accent="Not the algorithm."
          tone="yellow"
          sub="Every interaction is tuned to feel like holding a printed graphic novel, with instant native responsiveness. Scroll on and play with each panel."
        />
      </div>

      <div ref={pinRef} className="relative overflow-hidden lg:flex lg:h-screen lg:flex-col lg:justify-center">
        <div ref={trackRef} className="flex flex-col gap-8 px-4 pb-20 pt-8 sm:px-6 lg:w-max lg:flex-row lg:gap-10 lg:px-[max(2rem,calc((100vw-80rem)/2+2rem))] lg:pb-12">
          {PANELS.map((p, i) => (
            <article
              key={p.no}
              data-strip-panel
              className="relative flex flex-col border-[3px] border-black bg-panel shadow-[8px_8px_0_#000] lg:h-[min(640px,78vh)] lg:w-[min(880px,78vw)] lg:flex-row"
            >
              <div className={`${p.tone} relative flex flex-col justify-between border-b-[3px] border-black p-5 text-black sm:p-7 lg:w-[42%] lg:border-b-0 lg:border-r-[3px]`}>
                <div className="bg-halftone-paper pointer-events-none absolute inset-0 opacity-50" />
                <div className="relative">
                  <div className="flex items-center justify-between">
                    <span className="inline-flex items-center gap-1.5 border-2 border-black bg-black px-2 py-0.5 font-mono text-[11px] font-black text-white">
                      <p.icon className="h-3.5 w-3.5" /> PANEL {p.no}
                    </span>
                    <span className="font-bangers text-4xl leading-none opacity-30">{String(i + 1).padStart(2, "0")}</span>
                  </div>
                  <h3 className="mt-4 font-bangers text-4xl leading-[0.95] tracking-wide sm:text-5xl">{p.title}</h3>
                  <p className="mt-3 text-sm font-semibold leading-relaxed opacity-90 sm:text-[15px] xl:text-base">{p.text}</p>
                </div>
                <div className="relative mt-5 flex flex-wrap gap-1.5">
                  {p.chips.map((c) => (
                    <span key={c} className="border-2 border-black bg-white px-1.5 py-0.5 font-mono text-[10px] font-bold text-black">
                      {c}
                    </span>
                  ))}
                </div>
              </div>
              <div className="relative flex flex-1 flex-col justify-center p-5 sm:p-7">
                <div className="absolute right-3 top-3 hidden items-center gap-1 font-mono text-[10px] font-bold text-muted lg:flex">
                  <Sparkles className="h-3 w-3 text-amber" /> LIVE DEMO
                </div>
                <p.Demo />
              </div>
            </article>
          ))}
          <div className="hidden w-[min(420px,40vw)] shrink-0 flex-col items-start justify-center lg:flex">
            <div className="text-comic-outline font-bangers text-7xl leading-none text-amber">To be continued…</div>
            <p className="mt-3 max-w-xs text-newsprint/75">…in Issue 1.1: series, stats, duplicates, OCR and backups. Keep scrolling!</p>
            <Cpu className="mt-4 h-10 w-10 text-cyan" />
          </div>
        </div>
        <div className="absolute inset-x-0 bottom-0 hidden h-2 border-t-2 border-black bg-black lg:block">
          <div ref={barRef} className="h-full origin-left scale-x-0 bg-[linear-gradient(90deg,#FFD700,#00C2FF,#FF1F6D)]" />
        </div>
      </div>
    </section>
  );
}
