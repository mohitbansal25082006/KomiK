"use client";

import { useEffect, useRef, useState } from "react";
import { AnimatePresence, motion, useInView, useReducedMotion } from "framer-motion";
import { CalendarClock, Check, ImageUp, Layers3, ListChecks, MousePointer2, Pause, Play, Power, Sparkles, Wand2 } from "lucide-react";
import { ComicPage } from "@/components/comic/pages";

function Shell({
  className = "",
  tone,
  icon: Icon,
  title,
  kicker,
  children,
  delay = 0,
}: {
  className?: string;
  tone: string;
  icon: typeof Sparkles;
  title: string;
  kicker: string;
  children: React.ReactNode;
  delay?: number;
}) {
  return (
    <motion.article
      initial={{ opacity: 0, y: 46, rotate: 1.2 }}
      whileInView={{ opacity: 1, y: 0, rotate: 0 }}
      viewport={{ once: true, margin: "-8% 0px" }}
      transition={{ type: "spring", stiffness: 210, damping: 22, delay }}
      className={`group relative flex flex-col overflow-hidden border-[3px] border-black bg-panel text-newsprint shadow-[7px_7px_0_#000] ${className}`}
    >
      <header className={`${tone} flex items-center justify-between gap-2 border-b-[3px] border-black px-4 py-2 text-black`}>
        <span className="inline-flex items-center gap-2 font-bangers text-2xl leading-none tracking-wide">
          <Icon className="h-5 w-5" /> {title}
        </span>
        <span className="border-2 border-black bg-black px-1.5 py-0.5 font-mono text-[9px] font-black uppercase text-white">{kicker}</span>
      </header>
      <div className="relative flex flex-1 flex-col p-4 sm:p-5">{children}</div>
    </motion.article>
  );
}

/* ------------------------------ tag cleaner ------------------------------ */

const RAW = [
  { raw: "Home", clean: null },
  { raw: "action (1,234)", clean: "Action" },
  { raw: "rio pen 48", clean: "Rio Pen" },
  { raw: "READ NOW", clean: null },
  { raw: "#isekai", clean: "Isekai" },
  { raw: "sci-fi 12.4K", clean: "Sci-Fi" },
  { raw: "slice of life", clean: "Slice of Life" },
  { raw: "Chapter 3", clean: null },
  { raw: "dramas", clean: "Drama" },
  { raw: "Drama", clean: null },
  { raw: "iron orchard 2", clean: "Iron Orchard 2" },
] as const;

function TagCleaner() {
  const [clean, setClean] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-20% 0px" });
  useEffect(() => {
    if (!inView) return;
    const t = setTimeout(() => setClean(true), 1400);
    return () => clearTimeout(t);
  }, [inView]);
  const kept = RAW.filter((t) => t.clean).length;
  return (
    <Shell className="lg:col-span-7" tone="bg-amber" icon={Wand2} title="Every tag, cleaned" kicker="Details">
      <div ref={ref} className="grid flex-1 gap-5 sm:grid-cols-[1fr_190px]">
        <div>
          <p className="text-sm font-medium leading-relaxed text-newsprint/80">
            Sites print tags with view counts, menu words and duplicates. KomiK keeps <strong className="text-newsprint">every real tag, with no limit</strong>, drops the junk, fixes capitals and
            strips counts, but never a number that belongs to the name.
          </p>
          <div className="mt-4 flex min-h-[112px] flex-wrap content-start gap-1.5">
            <AnimatePresence mode="popLayout">
              {RAW.filter((t) => !clean || t.clean).map((t) => (
                <motion.span
                  layout
                  key={t.raw}
                  initial={{ scale: 0, rotate: -10 }}
                  animate={{ scale: 1, rotate: 0 }}
                  exit={{ scale: 0, rotate: 40, opacity: 0, transition: { duration: 0.25 } }}
                  transition={{ type: "spring", stiffness: 500, damping: 26 }}
                  className={`border-2 border-black px-2 py-0.5 text-xs font-bold shadow-[2px_2px_0_#000] ${
                    clean ? "rounded-full bg-amber font-bangers text-base tracking-wide text-black" : t.clean ? "bg-white font-mono text-black" : "bg-[#3a3a44] font-mono text-white/70"
                  }`}
                >
                  {clean ? t.clean : t.raw}
                </motion.span>
              ))}
            </AnimatePresence>
          </div>
          <button type="button" onClick={() => setClean((c) => !c)} className={`${clean ? "btn-comic-secondary" : "btn-comic-primary"} mt-4 inline-flex items-center gap-2 px-3 py-1.5 text-xs uppercase`}>
            <Sparkles className="h-3.5 w-3.5" /> {clean ? "Show the raw page" : "Clean them up!"}
          </button>
        </div>
        <div className="flex flex-col gap-3">
          <div className="border-[3px] border-black bg-black p-3 font-mono text-[11px]">
            <div className="text-white/50">artist link on the page</div>
            <div className={`mt-1 font-bold ${clean ? "text-white/40 line-through" : "text-white"}`}>rio pen 48</div>
            <div className="mt-2 text-white/50">saved as Penciller</div>
            <motion.div key={String(clean)} initial={{ scale: 0.6, opacity: 0 }} animate={{ scale: 1, opacity: 1 }} className="mt-1 font-bold text-amber">
              {clean ? "Rio Pen" : "…"}
            </motion.div>
          </div>
          <div className="border-[3px] border-black bg-paper-grain p-3 text-black">
            <div className="flex items-center gap-1.5 font-mono text-[10px] font-black uppercase text-black/60">
              <CalendarClock className="h-3.5 w-3.5" /> "Uploaded 5 years 6 months ago"
            </div>
            <div className="mt-1 font-bangers text-2xl leading-none">{clean ? "Year 2021 · Month 3" : "Year ?"}</div>
          </div>
          <div className="text-center font-bangers text-3xl leading-none text-amber">
            {clean ? kept : RAW.length} <span className="text-base text-newsprint/70">{clean ? "real tags kept" : "things on the page"}</span>
          </div>
        </div>
      </div>
    </Shell>
  );
}

/* ------------------------------- lazy pages ------------------------------- */

function LazyPages() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { margin: "-15% 0px" });
  const reduce = useReducedMotion();
  const [loaded, setLoaded] = useState(0);
  useEffect(() => {
    if (!inView) return;
    if (reduce) {
      setLoaded(6);
      return;
    }
    setLoaded(0);
    const t = setInterval(() => setLoaded((n) => (n >= 8 ? 0 : n + 1)), 520);
    return () => clearInterval(t);
  }, [inView, reduce]);
  return (
    <Shell className="lg:col-span-5" tone="bg-cyan" icon={ImageUp} title="Lazy pages? Handled." kicker="Auto-scroll" delay={0.05}>
      <div ref={ref} className="flex flex-1 flex-col">
        <p className="text-sm font-medium leading-relaxed text-newsprint/80">
          Readers that only load images as you scroll get scrolled for you, then your place on the page is put back. Webtoon strips, one-page-per-URL readers and script-built
          readers all work.
        </p>
        <div className="relative mt-4 grid flex-1 grid-cols-6 gap-1.5">
          {[1, 2, 3, 4, 5, 6].map((n, i) => (
            <div key={n} className="relative aspect-[2/3] border-2 border-black bg-[repeating-linear-gradient(45deg,#2a2a33_0_6px,#23232b_6px_12px)]">
              <motion.div animate={{ opacity: loaded > i ? 1 : 0, scale: loaded > i ? 1 : 0.9 }} transition={{ duration: 0.3 }} className="absolute inset-0 overflow-hidden [contain:paint] [will-change:opacity,transform]">
                <ComicPage n={n + 3} />
              </motion.div>
              {loaded > i && (
                <motion.span initial={{ scale: 0 }} animate={{ scale: 1 }} className="absolute -right-1.5 -top-1.5 flex h-4 w-4 items-center justify-center rounded-full border-2 border-black bg-[#2FD17A]">
                  <Check className="h-2.5 w-2.5 stroke-[4] text-black" />
                </motion.span>
              )}
            </div>
          ))}
        </div>
        <div className="mt-3 h-3 border-2 border-black bg-black">
          <motion.div className="h-full origin-left bg-cyan" initial={{ scaleX: 0 }} animate={{ scaleX: Math.min(loaded, 6) / 6 }} transition={{ duration: 0.3 }} />
        </div>
        <div className="mt-1 font-mono text-[11px] font-bold text-newsprint/70">{Math.min(loaded, 6)} / 6 pages loaded and found</div>
      </div>
    </Shell>
  );
}

/* ---------------------------- full-size gallery ---------------------------- */

function FullSize() {
  const [learned, setLearned] = useState(false);
  const [busy, setBusy] = useState(false);
  const learn = () => {
    if (learned) return setLearned(false);
    setBusy(true);
    setTimeout(() => {
      setBusy(false);
      setLearned(true);
    }, 900);
  };
  return (
    <Shell className="lg:col-span-5" tone="bg-magenta" icon={Layers3} title="Full-size galleries" kicker="Highest quality" delay={0.05}>
      <p className="text-sm font-medium leading-relaxed text-newsprint/80">
        Gallery sites only show small previews. KomiK opens <strong className="text-newsprint">one</strong> page, learns how the site names its full-size images, and gets every page at full
        size. Guesses and each page&apos;s own reader page back it up.
      </p>
      <div className="mt-4 border-[3px] border-black bg-black p-2.5 font-mono text-[10.5px] text-white">
        <div className="text-white/50">preview → full size</div>
        <div className="mt-1 truncate">
          cdn.example/<span className="text-magenta">t</span>/1042/12<span className="text-magenta">t</span>.jpg
        </div>
        <motion.div animate={{ opacity: learned ? 1 : 0.25 }} className="truncate text-amber">
          cdn.example/<span className="font-black">i</span>/1042/12.jpg {learned && "✓ rule learned"}
        </motion.div>
      </div>
      <div className="mt-3 grid grid-cols-5 gap-1.5">
        {[2, 3, 5, 6, 8].map((n, i) => (
          <motion.div
            key={n}
            animate={{ filter: learned ? "blur(0px) saturate(1)" : "blur(2.2px) saturate(0.5)", scale: learned ? [1, 1.12, 1] : 1 }}
            transition={{ delay: learned ? i * 0.08 : 0, duration: 0.45 }}
            className="aspect-[2/3] overflow-hidden border-2 border-black bg-paper"
          >
            <ComicPage n={n} />
          </motion.div>
        ))}
      </div>
      <button type="button" onClick={learn} className="btn-comic-magenta mt-4 inline-flex w-fit items-center gap-2 px-3 py-1.5 text-xs uppercase">
        {busy ? "Opening one page…" : learned ? "Back to previews" : "Get full size"}
      </button>
    </Shell>
  );
}

/* ------------------------------ series batch ------------------------------ */

function SeriesBatch() {
  const chapters = [1, 2, 3, 4, 5, 6, 7, 8];
  const [picked, setPicked] = useState<Set<number>>(new Set([3, 4, 5]));
  const [progress, setProgress] = useState<Record<number, number>>({});
  const [running, setRunning] = useState(false);
  const reduce = useReducedMotion();

  useEffect(() => {
    if (!running) return;
    const t = setInterval(() => {
      setProgress((p) => {
        const next = { ...p };
        const queue = Array.from(picked).sort((a, b) => a - b).filter((c) => (next[c] ?? 0) < 100);
        // two chapters at a time, like the real queue
        for (const c of queue.slice(0, 2)) next[c] = Math.min(100, (next[c] ?? 0) + (reduce ? 100 : 9 + ((c * 7) % 6)));
        return next;
      });
    }, 90);
    return () => clearInterval(t);
  }, [running, picked, reduce]);
  useEffect(() => {
    if (running && picked.size && Array.from(picked).every((c) => (progress[c] ?? 0) >= 100)) setRunning(false);
  }, [running, picked, progress]);

  const toggle = (c: number) => {
    if (running) return;
    setProgress({});
    setPicked((s) => {
      const n = new Set(s);
      if (n.has(c)) n.delete(c);
      else n.add(c);
      return n;
    });
  };
  return (
    <Shell className="lg:col-span-7" tone="bg-[#2FD17A]" icon={ListChecks} title="Whole series in one go" kicker="Batch" delay={0.1}>
      <div className="grid flex-1 gap-5 sm:grid-cols-2">
        <div>
          <p className="text-sm font-medium leading-relaxed text-newsprint/80">
            On a series page, pick chapters (or a range) and KomiK opens each one in the background, finds its pages and saves it as its own comic, numbered and named so Komik groups
            them into one series.
          </p>
          <div className="mt-4 flex flex-wrap gap-2">
            <button type="button" onClick={() => !running && (setProgress({}), setPicked(new Set([1, 2, 3, 4, 5, 6, 7, 8])))} className="btn-comic-secondary px-2.5 py-1 text-[11px] uppercase">
              All
            </button>
            <button type="button" onClick={() => !running && (setProgress({}), setPicked(new Set([3, 4, 5, 6, 7])))} className="btn-comic-secondary px-2.5 py-1 text-[11px] uppercase">
              Range 3–7
            </button>
            <button
              type="button"
              disabled={!picked.size}
              onClick={() => {
                setProgress({});
                setRunning(true);
              }}
              className="btn-comic-primary inline-flex items-center gap-1.5 px-3 py-1 text-[11px] uppercase disabled:opacity-50"
            >
              <Play className="h-3.5 w-3.5" /> Get {picked.size} chapter{picked.size === 1 ? "" : "s"}
            </button>
          </div>
        </div>
        <div className="space-y-1.5">
          {chapters.map((c) => {
            const on = picked.has(c);
            const p = progress[c] ?? 0;
            return (
              <button
                key={c}
                type="button"
                onClick={() => toggle(c)}
                className={`relative flex w-full items-center gap-2 overflow-hidden border-2 border-black px-2 py-1 text-left text-xs font-bold transition-colors ${on ? "bg-amber text-black" : "bg-panel-card text-newsprint hover:bg-cyan/20"}`}
              >
                {p > 0 && <motion.span className="absolute inset-0 origin-left bg-[#2FD17A]" initial={{ scaleX: 0 }} animate={{ scaleX: p / 100 }} transition={{ duration: 0.1 }} />}
                <span className={`relative flex h-4 w-4 items-center justify-center border-2 border-black ${on ? "bg-black text-amber" : "bg-white"}`}>{on && <Check className="h-3 w-3 stroke-[4]" />}</span>
                <span className="relative font-bangers text-base leading-none">Ch. {c}</span>
                <span className="relative ml-auto font-mono text-[10px]">{p >= 100 ? "saved ✓" : p > 0 ? `${Math.round(p)}%` : `${18 + c * 2} pages`}</span>
              </button>
            );
          })}
        </div>
      </div>
    </Shell>
  );
}

/* --------------------------------- resume --------------------------------- */

function Resume() {
  const [pct, setPct] = useState(0);
  const [state, setState] = useState<"running" | "closed" | "done">("running");
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { margin: "-15% 0px" });
  useEffect(() => {
    if (!inView || state !== "running") return;
    const t = setInterval(() => setPct((p) => (p >= 100 ? 100 : p + 1.4)), 60);
    return () => clearInterval(t);
  }, [inView, state]);
  useEffect(() => {
    if (pct >= 100 && state === "running") setState("done");
  }, [pct, state]);
  const pages = Math.round((pct / 100) * 64);
  return (
    <Shell className="lg:col-span-6" tone="bg-amber" icon={Power} title="Survives a restart" kicker="Resumable" delay={0.1}>
      <div ref={ref} className="flex flex-1 flex-col">
        <p className="text-sm font-medium leading-relaxed text-newsprint/80">
          Pages are kept as they download, with parallel connections and retries. Close the browser mid-download and KomiK carries on from the same page next time. Nothing starts over.
        </p>
        <div className="relative mt-4 border-[3px] border-black bg-black p-3">
          <AnimatePresence>
            {state === "closed" && (
              <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="absolute inset-0 z-10 flex items-center justify-center bg-black/85 font-bangers text-3xl tracking-wide text-white">
                BROWSER CLOSED… ZZZ
              </motion.div>
            )}
          </AnimatePresence>
          <div className="flex items-center justify-between font-mono text-[11px] font-bold text-white">
            <span>The Long Harbor · Vol. 3</span>
            <span className={state === "done" ? "text-[#2FD17A]" : "text-amber"}>{state === "done" ? "SAVED" : `${pages}/64 pages`}</span>
          </div>
          <div className="mt-2 h-5 border-2 border-white/40 bg-[#1c1c22]">
            <motion.div className="h-full origin-left bg-[linear-gradient(90deg,#FFD700,#FF1F6D)]" initial={{ scaleX: 0 }} animate={{ scaleX: pct / 100 }} transition={{ duration: 0.1 }} />
          </div>
          <div className="mt-1.5 font-mono text-[10px] text-white/60">{state === "running" ? `${(3.1 + (pct % 7) * 0.3).toFixed(1)} MB/s · 6 connections` : state === "closed" ? `paused at page ${pages}` : "all pages in, ComicInfo.xml written"}</div>
        </div>
        <div className="mt-3 flex gap-2">
          {state === "running" && (
            <button type="button" onClick={() => setState("closed")} className="btn-comic-magenta inline-flex items-center gap-1.5 px-3 py-1.5 text-xs uppercase">
              <Pause className="h-3.5 w-3.5" /> Close the browser
            </button>
          )}
          {state === "closed" && (
            <button type="button" onClick={() => setState("running")} className="btn-comic-primary inline-flex items-center gap-1.5 px-3 py-1.5 text-xs uppercase">
              <Play className="h-3.5 w-3.5" /> Reopen · resume at {pages}
            </button>
          )}
          {state === "done" && (
            <button type="button" onClick={() => { setPct(0); setState("running"); }} className="btn-comic-secondary px-3 py-1.5 text-xs uppercase">
              Again
            </button>
          )}
        </div>
      </div>
    </Shell>
  );
}

/* ------------------------------- pick pages ------------------------------- */

function PickPages() {
  const [picked, setPicked] = useState<number[]>([2, 3]);
  const toggle = (n: number) => setPicked((p) => (p.includes(n) ? p.filter((x) => x !== n) : [...p, n].sort((a, b) => a - b)));
  return (
    <Shell className="lg:col-span-6" tone="bg-cyan" icon={MousePointer2} title="Pick pages yourself" kicker="Any site" delay={0.15}>
      <p className="text-sm font-medium leading-relaxed text-newsprint/80">
        For the odd site nothing else can read: click the pages right on the page. KomiK keeps them in reading order. Go on, click some.
      </p>
      <div className="mt-4 grid grid-cols-6 gap-2">
        {[1, 2, 3, 4, 5, 6].map((n) => {
          const i = picked.indexOf(n);
          return (
            <motion.button
              key={n}
              type="button"
              onClick={() => toggle(n)}
              whileHover={{ y: -4 }}
              whileTap={{ scale: 0.92 }}
              className={`relative aspect-[2/3] overflow-hidden border-[3px] ${i >= 0 ? "border-amber shadow-[0_0_0_2px_#000,4px_4px_0_2px_#000]" : "border-black"}`}
              aria-pressed={i >= 0}
              aria-label={`Page ${n}`}
            >
              <ComicPage n={n + 6} />
              <AnimatePresence>
                {i >= 0 && (
                  <motion.span
                    initial={{ scale: 0, rotate: -40 }}
                    animate={{ scale: 1, rotate: 0 }}
                    exit={{ scale: 0 }}
                    className="absolute left-1/2 top-1/2 flex h-7 w-7 -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-full border-2 border-black bg-amber font-bangers text-lg text-black"
                  >
                    {i + 1}
                  </motion.span>
                )}
              </AnimatePresence>
            </motion.button>
          );
        })}
      </div>
      <div className="mt-4 flex items-center justify-between border-[3px] border-black bg-black px-3 py-2 font-bangers text-xl tracking-wide text-white">
        <span>
          <span className="text-amber">{picked.length}</span> pages picked
        </span>
        <span className="font-mono text-[10px] text-white/60">Enter to finish · Esc to cancel</span>
      </div>
    </Shell>
  );
}

export default function ExtensionPanels() {
  return (
    <div className="grid gap-6 lg:grid-cols-12">
      <TagCleaner />
      <LazyPages />
      <FullSize />
      <SeriesBatch />
      <Resume />
      <PickPages />
    </div>
  );
}
