"use client";

import { useEffect, useRef, useState } from "react";
import { animate, motion, useInView, useMotionValue, useSpring } from "framer-motion";
import { ArrowLeft, ArrowLeftRight, ArrowRight, BookOpen, Check, ChevronDown, Columns2, Copy, DatabaseBackup, Files, Flame, FolderPlus, Layers, Monitor, Plus, ScanText, Search, Settings2, Tag, Trash2, Trophy, X } from "lucide-react";
import SectionHeading from "@/components/fx/SectionHeading";

function useCount(to: number, inView: boolean, duration = 1.4) {
  const [v, setV] = useState(0);
  useEffect(() => {
    if (!inView) return;
    const c = animate(0, to, { duration, ease: [0.2, 0.8, 0.2, 1], onUpdate: (x) => setV(Math.round(x)) });
    return () => c.stop();
  }, [inView, to, duration]);
  return v;
}

function PanelShell({
  className = "",
  tone,
  tag,
  icon: Icon,
  title,
  children,
  delay = 0,
}: {
  className?: string;
  tone: string;
  tag: string;
  icon: typeof Layers;
  title: string;
  children: React.ReactNode;
  delay?: number;
}) {
  return (
    <motion.article
      initial={{ opacity: 0, y: 50, rotate: -1.5 }}
      whileInView={{ opacity: 1, y: 0, rotate: 0 }}
      viewport={{ once: true, margin: "-8% 0px" }}
      transition={{ type: "spring", stiffness: 200, damping: 22, delay }}
      className={`relative flex flex-col overflow-hidden border-[3px] border-black bg-white text-black shadow-[7px_7px_0_#000] ${className}`}
    >
      <header className={`${tone} flex items-center justify-between gap-2 border-b-[3px] border-black px-4 py-2`}>
        <span className="inline-flex items-center gap-2 font-bangers text-2xl leading-none tracking-wide">
          <Icon className="h-5 w-5" /> {title}
        </span>
        <span className="border-2 border-black bg-black px-1.5 py-0.5 font-mono text-[9px] font-black uppercase text-white">{tag}</span>
      </header>
      <div className="relative flex flex-1 flex-col p-4 sm:p-5">{children}</div>
    </motion.article>
  );
}

/* --------------------------- Series panel --------------------------- */

const COVERS = [
  { t: "Vol. 1", c: "#FF1F6D" },
  { t: "Vol. 2", c: "#FFD700" },
  { t: "Vol. 3", c: "#00C2FF" },
  { t: "Vol. 4", c: "#2FD17A" },
  { t: "Vol. 5", c: "#A78BFA" },
];

function SeriesPanel() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-15% 0px" });
  const pct = useCount(68, inView);
  const [fanned, setFanned] = useState(false);
  useEffect(() => {
    if (inView) {
      const t = setTimeout(() => setFanned(true), 500);
      return () => clearTimeout(t);
    }
  }, [inView]);
  return (
    <PanelShell className="lg:col-span-7" tone="bg-amber" tag="Standalone screen" icon={Layers} title="Series & Volumes">
      <div ref={ref} className="grid flex-1 items-center gap-5 sm:grid-cols-2">
        <div>
          <p className="text-sm font-semibold leading-relaxed text-black/80">
            Two sections: <strong>Continuation Series</strong> (issues, volumes, chapters and sequels, with typos merged and gaps flagged) and <strong>By Creator</strong>. Edit any
            series: rename it, drag the reading order, pick a cover and <strong>+ Add Comics</strong>. A creator collection can become a continuation series, never the reverse.
          </p>
          <div className="mt-4 border-[3px] border-black bg-black p-3 font-mono text-[11px] text-white">
            <div className="flex justify-between">
              <span>Neon Ronin · 5 volumes · no gaps</span>
            </div>
            <div className="mt-2 h-3 border-2 border-white/30 bg-[#222]">
              <motion.div className="h-full bg-amber" initial={{ width: 0 }} animate={{ width: inView ? `${pct}%` : 0 }} />
            </div>
            <div className="mt-1 flex justify-between font-bold">
              <span className="text-amber">{pct}% read</span>
              <span className="text-[#2FD17A]">▶ Up next: Vol. 4</span>
            </div>
          </div>
          <button
            type="button"
            onClick={() => setFanned((f) => !f)}
            className="btn-comic-primary mt-4 inline-flex items-center gap-1.5 px-3 py-1.5 text-xs uppercase"
          >
            <Plus className="h-3.5 w-3.5" /> {fanned ? "Stack series" : "Fan it out"}
          </button>
        </div>
        <div className="relative h-52" onMouseEnter={() => setFanned(true)}>
          {COVERS.map((c, i) => (
            <motion.div
              key={c.t}
              animate={
                fanned
                  ? { x: (i - 2) * 46, rotate: (i - 2) * 9, y: Math.abs(i - 2) * 10 }
                  : { x: i * 4, rotate: 0, y: -i * 4 }
              }
              transition={{ type: "spring", stiffness: 260, damping: 18, delay: i * 0.04 }}
              className="absolute left-1/2 top-2 -ml-[52px] flex h-[156px] w-[104px] flex-col justify-between border-[3px] border-black p-2 shadow-[4px_4px_0_#000]"
              style={{ background: c.c, zIndex: i }}
            >
              <div className="bg-halftone-paper absolute inset-0 opacity-50" />
              <span className="relative font-bangers text-lg leading-none">Neon Ronin</span>
              <span className="relative self-end border-2 border-black bg-white px-1 font-mono text-[10px] font-black">{c.t}</span>
            </motion.div>
          ))}
        </div>
      </div>
    </PanelShell>
  );
}

/* ---------------------------- Stats panel ---------------------------- */

function StatsPanel() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-15% 0px" });
  const hours = useCount(142, inView);
  const pages = useCount(8931, inView, 1.8);
  const streak = useCount(23, inView);
  const bars = [3, 5, 2, 6, 7, 4, 8, 5, 9, 6, 7, 10, 8, 9];
  const rx = useMotionValue(0);
  const ry = useMotionValue(0);
  const srx = useSpring(rx, { stiffness: 200, damping: 18 });
  const sry = useSpring(ry, { stiffness: 200, damping: 18 });
  return (
    <PanelShell className="lg:col-span-5" tone="bg-cyan" tag="100% local" icon={Trophy} title="Stats & Komik Wrapped" delay={0.08}>
      <div ref={ref} style={{ perspective: 900 }} className="flex flex-1 flex-col">
        <motion.div
          style={{ rotateX: srx, rotateY: sry }}
          onPointerMove={(e) => {
            const r = e.currentTarget.getBoundingClientRect();
            ry.set(((e.clientX - r.left) / r.width - 0.5) * 18);
            rx.set(-((e.clientY - r.top) / r.height - 0.5) * 18);
          }}
          onPointerLeave={() => (rx.set(0), ry.set(0))}
          className="relative flex-1 overflow-hidden border-[3px] border-black bg-[linear-gradient(135deg,#1B1030,#3B0F4F_55%,#FF1F6D)] p-4 text-white shadow-[4px_4px_0_#000]"
        >
          <div className="bg-halftone absolute inset-0 opacity-40" />
          <div className="relative flex items-center justify-between">
            <span className="font-bangers text-2xl tracking-wide text-amber">Komik Wrapped</span>
            <Flame className="h-6 w-6 text-amber" />
          </div>
          <div className="relative mt-3 grid grid-cols-3 gap-2 text-center">
            <div>
              <div className="font-bangers text-4xl leading-none">{hours}h</div>
              <div className="font-mono text-[9px] font-bold uppercase text-white/70">read time</div>
            </div>
            <div>
              <div className="font-bangers text-4xl leading-none">{pages.toLocaleString("en-US")}</div>
              <div className="font-mono text-[9px] font-bold uppercase text-white/70">pages</div>
            </div>
            <div>
              <div className="font-bangers text-4xl leading-none text-amber">{streak}</div>
              <div className="font-mono text-[9px] font-bold uppercase text-white/70">day streak</div>
            </div>
          </div>
          <div className="relative mt-4 flex h-16 items-end gap-1">
            {bars.map((b, i) => (
              <motion.div
                key={i}
                initial={{ height: 0 }}
                animate={{ height: inView ? `${b * 10}%` : 0 }}
                transition={{ delay: 0.3 + i * 0.05, type: "spring", stiffness: 200, damping: 16 }}
                className={`flex-1 border-2 border-black ${i === bars.length - 1 ? "bg-amber" : "bg-cyan"}`}
              />
            ))}
          </div>
          <div className="relative mt-1 font-mono text-[9px] font-bold uppercase text-white/70">14-day reading velocity · top 20 comics &amp; series</div>
        </motion.div>
        <p className="mt-3 text-[13px] font-semibold text-black/75">Every page turn logs a local reading session. No analytics beacons, just your own numbers.</p>
      </div>
    </PanelShell>
  );
}

/* -------------------------- Duplicates panel -------------------------- */

function DuplicatePanel() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { margin: "-15% 0px" });
  const [phase, setPhase] = useState(0);
  const [choice, setChoice] = useState<"keep" | "remove" | null>(null);
  useEffect(() => {
    if (!inView || choice) return;
    const t = setInterval(() => setPhase((p) => (p + 1) % 3), 1600);
    return () => clearInterval(t);
  }, [inView, choice]);
  const merged = phase >= 1 || choice !== null;
  return (
    <PanelShell className="lg:col-span-4" tone="bg-magenta text-white" tag="Confidence scored" icon={Files} title="Duplicate Manager" delay={0.04}>
      <div ref={ref} className="flex flex-1 flex-col">
        <div className="relative flex h-36 items-center justify-center">
          {[
            { ext: ".CBZ", c: "#FFD700", x: -60 },
            { ext: ".PDF", c: "#FF7A00", x: 60 },
          ].map((f) => (
            <motion.div
              key={f.ext}
              animate={{ x: merged ? f.x * 0.28 : f.x, rotate: merged ? f.x / 12 : 0, opacity: choice === "remove" && f.ext === ".PDF" ? 0.15 : 1 }}
              transition={{ type: "spring", stiffness: 260, damping: 18 }}
              className="absolute flex h-28 w-24 flex-col justify-between border-[3px] border-black p-2 shadow-[4px_4px_0_#000]"
              style={{ background: f.c }}
            >
              <span className="font-bangers text-2xl leading-none">{f.ext}</span>
              <span className="font-mono text-[9px] font-bold leading-tight">
                48 pages
                <br />
                212 MB
              </span>
            </motion.div>
          ))}
          <motion.div
            initial={false}
            animate={merged ? { scale: 1, rotate: -12, opacity: 1 } : { scale: 3, rotate: 10, opacity: 0 }}
            transition={{ type: "spring", stiffness: 500, damping: 18 }}
            className="absolute z-10 border-[3px] border-magenta bg-white/90 px-2 py-0.5 font-bangers text-2xl text-magenta"
          >
            {choice === "keep" ? "KEPT BOTH!" : choice === "remove" ? "CLEANED!" : "SAME ISSUE!"}
          </motion.div>
        </div>
        <p className="text-[13px] font-semibold text-black/75">Finds copies across formats (identical bytes, the same series and issue, or the same cleaned title) and recommends the copy to keep.</p>
        <div className="mt-3 grid grid-cols-2 gap-2">
          <button type="button" onClick={() => setChoice("keep")} className="btn-comic-cyan inline-flex items-center justify-center gap-1 px-2 py-2 text-[11px] uppercase">
            <Check className="h-3.5 w-3.5" /> Keep all
          </button>
          <button type="button" onClick={() => setChoice("remove")} className="btn-comic-magenta inline-flex items-center justify-center gap-1 px-2 py-2 text-[11px] uppercase">
            <Trash2 className="h-3.5 w-3.5" /> Remove copy
          </button>
        </div>
      </div>
    </PanelShell>
  );
}

/* ------------------------------ OCR panel ------------------------------ */

const OCR_WORDS = "THEY CAN TRACK ANYTHING THAT'S CONNECTED... SO WE STAY DISCONNECTED!".split(" ");

function OcrPanel() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { margin: "-15% 0px" });
  const [lit, setLit] = useState(0);
  const [copied, setCopied] = useState(false);
  useEffect(() => {
    if (!inView) return;
    const t = setInterval(() => setLit((l) => (l >= OCR_WORDS.length + 6 ? 0 : l + 1)), 180);
    return () => clearInterval(t);
  }, [inView]);
  return (
    <PanelShell className="lg:col-span-4" tone="bg-paper" tag="Windows.Media.Ocr" icon={ScanText} title="Offline OCR" delay={0.08}>
      <div ref={ref} className="flex flex-1 flex-col">
        <div className="relative overflow-hidden border-[3px] border-black bg-[#1E1348] p-4">
          <div className="bg-halftone absolute inset-0 opacity-30" />
          <div className="balloon relative px-4 py-3 text-center text-[13px] leading-snug">
            {OCR_WORDS.map((w, i) => (
              <span key={i} className={`mx-0.5 inline-block rounded-sm px-0.5 transition-colors duration-150 ${i < lit ? "bg-cyan/60" : ""}`}>
                {w}
              </span>
            ))}
          </div>
          <motion.div
            className="pointer-events-none absolute inset-x-0 h-1 bg-cyan shadow-[0_0_18px_4px_#00C2FF]"
            animate={inView ? { top: ["0%", "100%", "0%"] } : undefined}
            transition={{ duration: 3.2, repeat: Infinity, ease: "easeInOut" }}
          />
        </div>
        <p className="mt-3 text-[13px] font-semibold text-black/75">Reads speech bubbles in reading order (manga right to left), filters art noise, and lets you search, jump and copy text, with zero cloud APIs.</p>
        <button
          type="button"
          onClick={async () => {
            try {
              await navigator.clipboard.writeText(OCR_WORDS.join(" "));
              setCopied(true);
              setTimeout(() => setCopied(false), 1500);
            } catch {
              /* clipboard blocked */
            }
          }}
          className="btn-comic-secondary mt-3 inline-flex items-center justify-center gap-1.5 self-start px-3 py-1.5 text-[11px] uppercase"
        >
          {copied ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />} {copied ? "Copied!" : "Copy detected text"}
        </button>
      </div>
    </PanelShell>
  );
}

/* ----------------------------- Backup panel ----------------------------- */

function BackupPanel() {
  const ref = useRef<HTMLDivElement>(null);
  const active = useInView(ref, { margin: "200px 0px" });
  return (
    <PanelShell className="lg:col-span-4" tone="bg-[#2FD17A]" tag=".komikbackup" icon={DatabaseBackup} title="Portable Backups" delay={0.12}>
      <div className="flex flex-1 flex-col">
        <div ref={ref} className="relative flex h-32 items-center justify-between px-2">
          {["Old PC", "New PC"].map((l) => (
            <div key={l} className="flex flex-col items-center gap-1">
              <Monitor className="h-14 w-14" strokeWidth={1.6} />
              <span className="font-mono text-[10px] font-bold uppercase">{l}</span>
            </div>
          ))}
          <motion.div
            className="absolute left-1/2 top-6 flex h-16 w-14 -translate-x-1/2 flex-col items-center justify-center border-[3px] border-black bg-amber font-mono text-[8px] font-black shadow-[3px_3px_0_#000]"
            animate={active ? { x: ["-160%", "60%"], y: [0, -26, 0], rotate: [-10, 10] } : undefined}
            transition={{ duration: 2.2, repeat: Infinity, repeatType: "reverse", ease: "easeInOut" }}
          >
            <DatabaseBackup className="h-5 w-5" />
            .komik
            <br />
            backup
          </motion.div>
        </div>
        <ul className="mt-1 grid grid-cols-2 gap-x-2 gap-y-1 font-mono text-[11px] font-bold">
          {["History", "Bookmarks & notes", "Favorites & tags", "Folders & settings"].map((i) => (
            <li key={i} className="flex items-center gap-1">
              <Check className="h-3 w-3 shrink-0 text-black" /> {i}
            </li>
          ))}
        </ul>
        <p className="mt-2 text-[13px] font-semibold text-black/75">One JSON file with conflict-free import. Move between PCs, no cloud sync required.</p>
      </div>
    </PanelShell>
  );
}

/* ------------------------- How comics open panel ------------------------- */

const LAYOUTS = [
  { id: "page", label: "Page by page", icon: BookOpen },
  { id: "spread", label: "Two-page spread", icon: Columns2 },
  { id: "webtoon", label: "Webtoon", icon: Layers },
] as const;

function OpenModePanel() {
  const [layout, setLayout] = useState<(typeof LAYOUTS)[number]["id"]>("webtoon");
  const [manga, setManga] = useState(true);
  const summary = `Comics open ${layout === "page" ? "page by page" : layout === "spread" ? "as two-page spreads" : "as a webtoon strip"}, ${manga ? "right to left" : "left to right"}.`;
  return (
    <PanelShell className="lg:col-span-6" tone="bg-cyan" tag="Settings → Reading" icon={Settings2} title="How Comics Open" delay={0.04}>
      <div className="flex flex-1 flex-col gap-3">
        <p className="text-[13px] font-semibold text-black/75">Pick the layout and direction every comic starts in. Webtoon and manga combine, and each comic still opens at its saved page.</p>
        <div className="grid grid-cols-3 gap-2">
          {LAYOUTS.map((l) => (
            <button
              key={l.id}
              type="button"
              aria-pressed={layout === l.id}
              onClick={() => setLayout(l.id)}
              className={`flex flex-col items-center gap-1 border-[3px] border-black px-2 py-2 text-[11px] font-black uppercase shadow-[3px_3px_0_#000] transition-transform active:translate-y-0.5 ${layout === l.id ? "bg-amber" : "bg-white"}`}
            >
              <l.icon className="h-5 w-5" /> {l.label}
            </button>
          ))}
        </div>
        <div className="grid grid-cols-2 gap-2">
          {[
            { v: false, label: "Comic · left to right", icon: ArrowRight },
            { v: true, label: "Manga · right to left", icon: ArrowLeft },
          ].map((d) => (
            <button
              key={d.label}
              type="button"
              aria-pressed={manga === d.v}
              onClick={() => setManga(d.v)}
              className={`inline-flex items-center justify-center gap-1.5 border-[3px] border-black px-2 py-2 text-[11px] font-black uppercase shadow-[3px_3px_0_#000] ${manga === d.v ? "bg-magenta text-white" : "bg-white"}`}
            >
              <d.icon className="h-4 w-4" /> {d.label}
            </button>
          ))}
        </div>
        <motion.p
          key={summary}
          initial={{ opacity: 0, y: 6 }}
          animate={{ opacity: 1, y: 0 }}
          className="border-2 border-black bg-paper px-3 py-2 font-mono text-[11px] font-bold"
          aria-live="polite"
        >
          {summary}
        </motion.p>
      </div>
    </PanelShell>
  );
}

/* ------------------------------ Tags panel ------------------------------ */

const TAGS = [
  { name: "Sci-Fi", n: 11 },
  { name: "Manga", n: 6 },
  { name: "Cyberpunk", n: 6 },
  { name: "Mystery", n: 4 },
  { name: "Fantasy", n: 3 },
  { name: "Superhero", n: 3 },
  { name: "Webtoon", n: 3 },
  { name: "Horror", n: 0 },
];

function TagsPanel() {
  const [q, setQ] = useState("");
  const [picked, setPicked] = useState<string | null>("Manga");
  const shown = TAGS.filter((t) => t.name.toLowerCase().includes(q.trim().toLowerCase()));
  const active = TAGS.find((t) => t.name === picked);
  return (
    <PanelShell className="lg:col-span-6" tone="bg-amber" tag="Built for many tags" icon={Tag} title="Tag Manager" delay={0.08}>
      <div className="flex flex-1 flex-col gap-3">
        <p className="text-[13px] font-semibold text-black/75">Search, sort by use, rename, merge and clean up unused tags. Filtering shows a sticker with the match count, and empty filters suggest a next step.</p>
        <label className="flex items-center gap-2 border-[3px] border-black bg-white px-2 py-1.5">
          <Search className="h-4 w-4 shrink-0" />
          <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search tags…" aria-label="Search tags" className="w-full bg-transparent text-sm font-semibold outline-none" />
        </label>
        <div className="flex min-h-[4.5rem] flex-wrap content-start gap-2">
          {shown.map((t) => (
            <button
              key={t.name}
              type="button"
              aria-pressed={picked === t.name}
              onClick={() => setPicked(picked === t.name ? null : t.name)}
              className={`inline-flex items-center gap-1.5 rounded-full border-2 border-black px-2.5 py-1 text-xs font-black ${picked === t.name ? "bg-amber shadow-[2px_2px_0_#000]" : t.n === 0 ? "border-dashed bg-white/60 text-black/50" : "bg-white"}`}
            >
              {t.name} <span className="rounded-full bg-black px-1.5 text-[10px] text-white">{t.n}</span>
            </button>
          ))}
          {shown.length === 0 && <span className="font-mono text-xs font-bold text-black/60">No tag matches “{q}”.</span>}
        </div>
        <div className="flex items-center gap-2">
          {active ? (
            <motion.span
              key={active.name}
              initial={{ scale: 0.6, rotate: -8 }}
              animate={{ scale: 1, rotate: -2 }}
              transition={{ type: "spring", stiffness: 400, damping: 14 }}
              className="inline-flex items-center gap-2 border-[3px] border-black bg-amber px-2 py-1 text-xs font-black shadow-[3px_3px_0_#000]"
            >
              <Tag className="h-3.5 w-3.5" /> TAG {active.name}
              <span className="rounded-full bg-black px-1.5 text-[10px] text-white">{active.n === 1 ? "1 comic" : `${active.n} comics`}</span>
              <button type="button" onClick={() => setPicked(null)} aria-label="Clear tag filter" className="grid h-4 w-4 place-items-center rounded-full bg-white">
                <X className="h-3 w-3" />
              </button>
            </motion.span>
          ) : (
            <span className="font-mono text-[11px] font-bold text-black/60">Pick a tag to filter the library.</span>
          )}
        </div>
      </div>
    </PanelShell>
  );
}

/* ---------------------------- Toolbar panel ---------------------------- */

function ToolbarPanel() {
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const active = useInView(ref, { margin: "200px 0px" });
  useEffect(() => {
    if (!active) return;
    const t = setInterval(() => setOpen((o) => !o), 2200);
    return () => clearInterval(t);
  }, [active]);
  return (
    <PanelShell className="lg:col-span-12" tone="bg-amber" tag="Remembers you" icon={ArrowLeftRight} title="Responsive Library & Window Memory" delay={0.05}>
      <div className="grid items-center gap-6 md:grid-cols-2">
        <div>
          <p className="text-sm font-semibold leading-relaxed text-black/80">
            The Library top bar reflows for any window size. A single accent <strong>ADD</strong> button folds Add Folder and Add Comic into one dropdown, and the mouse wheel
            scrolls toolbars sideways on compact windows. Komik also remembers your window size, position and maximized state between launches.
          </p>
        </div>
        <div ref={ref} className="relative h-44 overflow-hidden border-[3px] border-black bg-[#1b1b1f]">
          <motion.div
            className="absolute left-3 top-3 border-2 border-white/20 bg-[#26262c] shadow-2xl"
            style={{ width: "88%", height: "80%" }}
            animate={active ? { width: ["88%", "58%", "88%"], height: ["80%", "70%", "80%"] } : undefined}
            transition={{ duration: 6, repeat: Infinity, ease: "easeInOut" }}
          >
            <div className="flex h-6 items-center justify-between border-b border-white/10 px-2 text-[9px] text-white/70">
              <span>Komik — Library</span>
              <span>─ ☐ ✕</span>
            </div>
            <div className="no-scrollbar flex items-center gap-1.5 overflow-hidden px-2 py-2">
              <div className="relative">
                <span className="inline-flex items-center gap-1 whitespace-nowrap bg-amber px-2 py-1 text-[10px] font-black text-black">
                  <FolderPlus className="h-3 w-3" /> ADD <ChevronDown className={`h-3 w-3 transition-transform ${open ? "rotate-180" : ""}`} />
                </span>
                <motion.div
                  initial={false}
                  animate={open ? { opacity: 1, y: 0, scale: 1 } : { opacity: 0, y: -6, scale: 0.95 }}
                  className="absolute left-0 top-7 z-10 w-28 border border-white/15 bg-[#2f2f36] p-1 text-[10px] text-white shadow-xl"
                >
                  <div className="bg-white/10 px-1.5 py-1">Add Folder…</div>
                  <div className="px-1.5 py-1">Add Comic…</div>
                </motion.div>
              </div>
              {["Search library…", "Sort: Recent", "Grid", "Series", "Stats", "Duplicates"].map((c) => (
                <span key={c} className="whitespace-nowrap border border-white/15 px-2 py-1 text-[10px] text-white/80">
                  {c}
                </span>
              ))}
            </div>
            <div className="grid grid-cols-6 gap-1.5 px-2">
              {["#FF1F6D", "#FFD700", "#00C2FF", "#2FD17A", "#A78BFA", "#FF7A00"].map((c) => (
                <div key={c} className="aspect-[2/3] border border-black" style={{ background: c }} />
              ))}
            </div>
          </motion.div>
        </div>
      </div>
    </PanelShell>
  );
}

export default function NewInV11() {
  return (
    <section id="whats-new" className="relative scroll-mt-16 overflow-hidden border-b-[3px] border-black bg-paper-grain py-20 text-black sm:py-28">
      <div className="bg-halftone-magenta pointer-events-none absolute -right-20 top-0 h-[520px] w-[520px] rotate-12 opacity-60" />
      <div className="relative mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex flex-wrap items-end justify-between gap-6">
          <SectionHeading
            onPaper
            caption="Special edition · Issue 1.1.0"
            title="Brand-new"
            accent="this issue!"
            tone="magenta"
            sub="Version 1.1.0 is the biggest update yet: a comic-style app, smarter series and tags, a faster reader that opens the way you like, and local analytics."
          />
          <motion.div
            initial={{ scale: 0, rotate: -30 }}
            whileInView={{ scale: 1, rotate: 8 }}
            viewport={{ once: true }}
            transition={{ type: "spring", stiffness: 300, damping: 12, delay: 0.3 }}
            className="hidden h-36 w-36 shrink-0 items-center justify-center rounded-full border-[4px] border-black bg-magenta text-center font-bangers text-3xl leading-none text-white shadow-[6px_6px_0_#000] md:flex"
          >
            15 new
            <br />
            features!
          </motion.div>
        </div>
        <div className="mt-12 grid gap-6 lg:grid-cols-12">
          <SeriesPanel />
          <StatsPanel />
          <DuplicatePanel />
          <OcrPanel />
          <BackupPanel />
          <OpenModePanel />
          <TagsPanel />
          <ToolbarPanel />
        </div>
      </div>
    </section>
  );
}
