"use client";

import { useEffect, useRef, useState } from "react";
import { AnimatePresence, animate, motion, useInView, useReducedMotion } from "framer-motion";
import { BookOpenText, Columns2, Copy, FileCode2, FolderSync, Infinity as InfinityIcon, RefreshCw, Rows3, Sparkles, Tags } from "lucide-react";
import SectionHeading from "@/components/fx/SectionHeading";
import { ComicPage } from "@/components/comic/pages";

function Panel({
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
  icon: typeof Tags;
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

/* ------------------------------ ComicInfo.xml ------------------------------ */

const FIELDS = [
  { xml: "Title", label: "Title", value: "Cyberpunk Chronicles #01" },
  { xml: "Series", label: "Series", value: "Cyberpunk Chronicles" },
  { xml: "Writer", label: "Writers", value: "Mira Sol" },
  { xml: "Penciller", label: "Artists", value: "Dex Halloway" },
  { xml: "Year", label: "Released", value: "2024" },
  { xml: "Tags", label: "Tags", value: "Cyberpunk, Sci-Fi, Action, Drones" },
];

function ComicInfoPanel() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-15% 0px" });
  const reduce = useReducedMotion();
  const [shown, setShown] = useState(0);
  useEffect(() => {
    if (!inView) return;
    if (reduce) return setShown(FIELDS.length);
    const t = setInterval(() => setShown((n) => (n >= FIELDS.length ? n : n + 1)), 380);
    return () => clearInterval(t);
  }, [inView, reduce]);
  return (
    <Panel className="lg:col-span-7" tone="bg-amber" tag="Library" icon={FileCode2} title="Reads what's inside">
      <div ref={ref} className="grid flex-1 gap-4 sm:grid-cols-2">
        <div>
          <p className="text-sm font-semibold leading-relaxed text-black/80">
            Komik now reads <strong>ComicInfo.xml</strong> from CBZ, ZIP, CBR, CB7 and image folders: title, series, number, writers, artists, publisher, date, summary and tags. Your own
            edits always win.
          </p>
          <div className="mt-3 border-[3px] border-black bg-black p-2.5 font-mono text-[10.5px] leading-relaxed text-white">
            {FIELDS.map((f, i) => (
              <motion.div key={f.xml} animate={{ opacity: shown > i ? 1 : 0.25 }} className="truncate">
                <span className="text-magenta">&lt;{f.xml}&gt;</span>
                {f.value}
                <span className="text-magenta">&lt;/{f.xml}&gt;</span>
              </motion.div>
            ))}
          </div>
        </div>
        <div className="flex flex-col border-[3px] border-black bg-[#fff8e7] shadow-[4px_4px_0_#000]">
          <div className="flex gap-3 border-b-[3px] border-black p-3">
            <div className="w-16 shrink-0 border-2 border-black shadow-[2px_2px_0_#000]">
              <div className="aspect-[2/3] overflow-hidden bg-paper">
                <ComicPage n={1} />
              </div>
            </div>
            <div className="min-w-0">
              <div className="font-mono text-[9px] font-black uppercase text-black/50">Comic details</div>
              <AnimatePresence mode="wait">
                <motion.div key={shown > 0 ? "t" : "f"} initial={{ opacity: 0, y: 6 }} animate={{ opacity: 1, y: 0 }} className="font-bangers text-xl leading-none">
                  {shown > 0 ? FIELDS[0].value : "cyberpunk_chronicles_01.cbz"}
                </motion.div>
              </AnimatePresence>
            </div>
          </div>
          <dl className="flex-1 space-y-1.5 p-3 text-xs">
            {FIELDS.slice(1).map((f, i) => (
              <div key={f.label} className="grid grid-cols-[62px_1fr] items-baseline gap-2">
                <dt className="font-mono text-[9px] font-black uppercase text-black/50">{f.label}</dt>
                <dd className="min-h-[18px]">
                  {shown > i + 1 ? (
                    f.label === "Tags" ? (
                      <span className="flex flex-wrap gap-1">
                        {f.value.split(", ").map((t, k) => (
                          <motion.span key={t} initial={{ scale: 0 }} animate={{ scale: 1 }} transition={{ delay: k * 0.06, type: "spring", stiffness: 500, damping: 16 }} className="rounded-full border-2 border-black bg-amber px-1.5 font-bold">
                            {t}
                          </motion.span>
                        ))}
                      </span>
                    ) : (
                      <motion.span initial={{ opacity: 0, x: -6 }} animate={{ opacity: 1, x: 0 }} className="font-bold">
                        {f.value}
                      </motion.span>
                    )
                  ) : (
                    <span className="inline-block h-2.5 w-20 bg-black/10" />
                  )}
                </dd>
              </div>
            ))}
          </dl>
        </div>
      </div>
    </Panel>
  );
}

/* -------------------------------- no limit -------------------------------- */

const MANY_TAGS = ["Action", "Sci-Fi", "Cyberpunk", "Drones", "Neon City", "Heist", "Found Family", "Rain", "Rooftops", "Archives", "Couriers", "Mecha", "Hackers", "Night Mode", "Dystopia", "Rebels", "Robots", "Street Food", "Chase", "Mystery", "Friendship", "Slow Burn", "Comedy", "Drama", "Full Color", "Oneshot", "Twist", "Villain Arc", "Libraries", "Blimps", "Retro PCs", "Storms", "Skylines", "Comebacks"];

function NoLimitPanel() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-15% 0px" });
  const [count, setCount] = useState(0);
  useEffect(() => {
    if (!inView) return;
    const c = animate(0, MANY_TAGS.length, { duration: 2.2, ease: [0.2, 0.7, 0.3, 1], onUpdate: (v) => setCount(Math.round(v)) });
    return () => c.stop();
  }, [inView]);
  return (
    <Panel className="lg:col-span-5" tone="bg-magenta text-white" tag="Tags" icon={InfinityIcon} title="Every tag. No cap." delay={0.05}>
      <div ref={ref} className="flex flex-1 flex-col">
        <p className="text-sm font-semibold leading-relaxed text-black/80">A comic with 34 tags now shows all 34. Genres and tags come in together, cleaned and de-duplicated, with no limit on how many.</p>
        <div className="relative mt-3 flex flex-1 flex-wrap content-start gap-1">
          {MANY_TAGS.slice(0, count).map((t, i) => (
            <motion.span
              key={t}
              initial={{ y: -40, opacity: 0, rotate: i % 2 ? 20 : -20 }}
              animate={{ y: 0, opacity: 1, rotate: 0 }}
              transition={{ type: "spring", stiffness: 500, damping: 20 }}
              className={`rounded-full border-2 border-black px-1.5 text-[10.5px] font-bold ${["bg-amber", "bg-cyan", "bg-[#2FD17A]", "bg-white"][i % 4]}`}
            >
              {t}
            </motion.span>
          ))}
        </div>
        <div className="mt-3 flex items-end justify-between border-t-[3px] border-black pt-2">
          <span className="font-bangers text-6xl leading-none">{count}</span>
          <span className="border-2 border-black bg-black px-2 py-1 font-bangers text-lg tracking-wide text-amber">
            old limit: <span className="line-through decoration-magenta decoration-[3px]">30</span>
          </span>
        </div>
      </div>
    </Panel>
  );
}

/* ----------------------------- watched folders ----------------------------- */

function WatchedPanel() {
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { margin: "-15% 0px" });
  const reduce = useReducedMotion();
  const [n, setN] = useState(0);
  useEffect(() => {
    if (!inView || reduce) return;
    const t = setInterval(() => setN((x) => (x + 1) % 4), 1600);
    return () => clearInterval(t);
  }, [inView, reduce]);
  return (
    <Panel className="lg:col-span-5" tone="bg-cyan" tag="Automatic" icon={FolderSync} title="Folders fill the shelf" delay={0.05}>
      <div ref={ref} className="flex flex-1 flex-col">
        <p className="text-sm font-semibold leading-relaxed text-black/80">
          Finished downloads in a watched folder are added on their own, details and tags included, once the file stops growing. Anything added while Komik was closed is picked up at start.
        </p>
        <div className="relative mt-4 flex flex-1 items-end justify-between gap-4">
          <div className="relative h-32 w-32 overflow-hidden">
            <AnimatePresence>
              <motion.div
                key={n}
                initial={{ y: -24, opacity: 0, rotate: -15 }}
                animate={{ y: 44, opacity: [0, 1, 1, 0], rotate: 5 }}
                transition={{ duration: 1.3, ease: "easeIn" }}
                className="absolute left-8 top-0 z-10 border-2 border-black bg-white px-1.5 py-0.5 font-mono text-[10px] font-black shadow-[2px_2px_0_#000]"
              >
                ch-{String(12 + n).padStart(2, "0")}.cbz
              </motion.div>
            </AnimatePresence>
            <svg viewBox="0 0 120 90" className="absolute bottom-0 h-24 w-32" aria-hidden>
              <path d="M6 22 h40 l10 10 h58 v52 h-108z" fill="#FFD700" stroke="#000" strokeWidth="4" strokeLinejoin="round" />
              <path d="M6 38 h108 v46 h-108z" fill="#FFE34D" stroke="#000" strokeWidth="4" strokeLinejoin="round" />
              <text x="60" y="68" textAnchor="middle" fontFamily="Impact, sans-serif" fontSize="15">KomiK</text>
            </svg>
          </div>
          <div className="flex-1 border-[3px] border-black bg-black p-2.5 font-mono text-[11px] text-white">
            <div className="text-white/50">Library</div>
            <motion.div key={n} initial={{ scale: 1.4, color: "#FFD700" }} animate={{ scale: 1, color: "#ffffff" }} className="font-bangers text-4xl leading-none">
              {240 + n} comics
            </motion.div>
            <div className="mt-1 text-[#2FD17A]">+1 added · tags imported</div>
          </div>
        </div>
      </div>
    </Panel>
  );
}

/* ------------------------------- re-download ------------------------------- */

const OLD_TAGS = ["Action", "Sci-Fi", "Drones", "Heist"];
const NEW_TAGS = ["Mecha", "Storms", "Rooftops", "Rebels"];

function RefreshPanel() {
  const [updated, setUpdated] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const inView = useInView(ref, { once: true, margin: "-20% 0px" });
  useEffect(() => {
    if (!inView) return;
    const t = setTimeout(() => setUpdated(true), 1500);
    return () => clearTimeout(t);
  }, [inView]);
  return (
    <Panel className="lg:col-span-7" tone="bg-[#2FD17A]" tag="Always current" icon={RefreshCw} title="Saved it again? Refreshed." delay={0.1}>
      <div ref={ref} className="grid flex-1 gap-4 sm:grid-cols-[1fr_1.1fr]">
        <div>
          <p className="text-sm font-semibold leading-relaxed text-black/80">
            Download a comic again under the same name (say, now with more tags) and Komik notices the file changed and adds what&apos;s new. Reading progress, favourites and your edits
            stay put.
          </p>
          <button type="button" onClick={() => setUpdated((u) => !u)} className="btn-comic-primary mt-4 inline-flex items-center gap-2 px-3 py-1.5 text-xs uppercase">
            <RefreshCw className={`h-3.5 w-3.5 ${updated ? "" : "animate-spin"}`} /> {updated ? "Show the old file" : "Save it again"}
          </button>
        </div>
        <div className="border-[3px] border-black bg-[#fff8e7] p-3 shadow-[4px_4px_0_#000]">
          <div className="flex items-center justify-between font-mono text-[10px] font-black">
            <span>neon_ronin_vol1.cbz</span>
            <motion.span key={String(updated)} initial={{ scale: 1.5, color: "#FF1F6D" }} animate={{ scale: 1, color: "#000000" }}>
              {updated ? "35,959,555" : "35,959,517"} bytes
            </motion.span>
          </div>
          <div className="mt-2 flex min-h-[64px] flex-wrap content-start gap-1">
            {OLD_TAGS.map((t) => (
              <span key={t} className="rounded-full border-2 border-black bg-white px-1.5 text-[11px] font-bold">
                {t}
              </span>
            ))}
            <AnimatePresence>
              {updated &&
                NEW_TAGS.map((t, i) => (
                  <motion.span
                    key={t}
                    initial={{ scale: 0, rotate: -30 }}
                    animate={{ scale: 1, rotate: 0 }}
                    exit={{ scale: 0 }}
                    transition={{ type: "spring", stiffness: 500, damping: 14, delay: 0.2 + i * 0.1 }}
                    className="rounded-full border-2 border-black bg-amber px-1.5 text-[11px] font-bold"
                  >
                    + {t}
                  </motion.span>
                ))}
            </AnimatePresence>
          </div>
          <div className="mt-2 flex items-center justify-between border-t-2 border-dashed border-black/40 pt-2 font-mono text-[10px] font-bold">
            <span>Read to page 18 of 64</span>
            <span className="text-[#1a9c56]">kept ✓</span>
          </div>
        </div>
      </div>
    </Panel>
  );
}

/* ------------------------------- duplicates ------------------------------- */

function DuplicatesPanel() {
  const [matched, setMatched] = useState(false);
  return (
    <Panel className="lg:col-span-6" tone="bg-amber" tag="Duplicates" icon={Copy} title="Sharper duplicate finder" delay={0.1}>
      <p className="text-sm font-semibold leading-relaxed text-black/80">
        Some downloaders put a label like <code className="bg-black/10 px-1 font-mono font-bold">original -</code> in front of the credit. Komik now looks past it, so both copies are found,
        while different parts of a series stay apart.
      </p>
      <div className="relative mt-4 h-[118px]">
        {[0, 1].map((i) => (
          <motion.div
            key={i}
            animate={matched ? { y: i * 12, x: i * 12, rotate: i ? 2 : -2 } : { y: i * 60, x: 0, rotate: 0 }}
            transition={{ type: "spring", stiffness: 300, damping: 20 }}
            className="absolute inset-x-0 border-[3px] border-black bg-white px-3 py-2 font-mono text-[11px] font-bold shadow-[3px_3px_0_#000]"
            style={{ zIndex: 2 - i }}
          >
            {i === 1 && (
              <AnimatePresence>
                {!matched && (
                  <motion.span exit={{ x: -80, rotate: -25, opacity: 0 }} transition={{ duration: 0.35 }} className="mr-1 inline-block bg-magenta px-1 text-white">
                    original -
                  </motion.span>
                )}
              </AnimatePresence>
            )}
            [Neon Ink (Mira Sol)] The Last Archive [English]
          </motion.div>
        ))}
        <AnimatePresence>
          {matched && (
            <motion.span initial={{ scale: 0, rotate: -30 }} animate={{ scale: 1, rotate: 8 }} exit={{ scale: 0 }} className="absolute -right-2 bottom-0 z-10 border-[3px] border-black bg-[#2FD17A] px-2 py-0.5 font-bangers text-xl shadow-[3px_3px_0_#000]">
              SAME COMIC!
            </motion.span>
          )}
        </AnimatePresence>
      </div>
      <button type="button" onClick={() => setMatched((m) => !m)} className="btn-comic-primary mt-3 inline-flex w-fit items-center gap-2 px-3 py-1.5 text-xs uppercase">
        <Sparkles className="h-3.5 w-3.5" /> {matched ? "Reset" : "Find duplicates"}
      </button>
    </Panel>
  );
}

/* ------------------------------ mode transition ------------------------------ */

function TransitionPanel() {
  const [webtoon, setWebtoon] = useState(false);
  return (
    <Panel className="lg:col-span-6" tone="bg-cyan" tag="Reader" icon={BookOpenText} title="Silky mode switch" delay={0.15}>
      <p className="text-sm font-semibold leading-relaxed text-black/80">
        Switching between two-page spreads and webtoon scrolling now glides: the page settles back, the new layout lays itself out off-screen and rises in. It follows Windows&apos; animation
        setting too.
      </p>
      <div className="mt-4 flex flex-1 items-center gap-4">
        <div className="relative flex h-44 flex-1 items-center justify-center overflow-hidden border-[3px] border-black bg-[#0f0f12]">
          <AnimatePresence mode="wait">
            <motion.div
              key={webtoon ? "w" : "s"}
              initial={{ opacity: 0, scale: 0.96, y: 18 }}
              animate={{ opacity: 1, scale: 1, y: 0, transition: { duration: 0.32, ease: [0.22, 1, 0.36, 1] } }}
              exit={{ opacity: 0, scale: 0.97, y: -8, transition: { duration: 0.15 } }}
              className={webtoon ? "flex h-full w-16 flex-col gap-1 py-2" : "flex h-32 gap-0.5"}
            >
              {(webtoon ? [5, 6, 7] : [2, 3]).map((n) => (
                <div key={n} className={`overflow-hidden border border-black bg-paper ${webtoon ? "aspect-[2/3] w-full" : "aspect-[2/3] h-full"}`}>
                  <ComicPage n={n} />
                </div>
              ))}
            </motion.div>
          </AnimatePresence>
        </div>
        <div className="flex flex-col gap-2">
          <button type="button" onClick={() => setWebtoon(false)} className={`${!webtoon ? "btn-comic-primary" : "btn-comic-secondary"} inline-flex items-center gap-1.5 px-3 py-2 text-xs uppercase`}>
            <Columns2 className="h-4 w-4" /> Spread · D
          </button>
          <button type="button" onClick={() => setWebtoon(true)} className={`${webtoon ? "btn-comic-primary" : "btn-comic-secondary"} inline-flex items-center gap-1.5 px-3 py-2 text-xs uppercase`}>
            <Rows3 className="h-4 w-4" /> Webtoon · V
          </button>
        </div>
      </div>
    </Panel>
  );
}

export default function NewInV12() {
  return (
    <section id="whats-new" className="relative scroll-mt-16 overflow-hidden border-b-[3px] border-black bg-paper-grain py-20 text-black sm:py-28">
      <div className="bg-halftone-cyan pointer-events-none absolute -left-24 top-10 h-[520px] w-[520px] -rotate-12 opacity-70" />
      <div className="bg-halftone-magenta pointer-events-none absolute -right-20 bottom-0 h-[420px] w-[420px] rotate-12 opacity-50" />
      <div className="relative mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex flex-wrap items-end justify-between gap-6">
          <SectionHeading
            onPaper
            caption="Special edition · Issue 1.2.0"
            title="Hot off"
            accent="the press!"
            tone="cyan"
            sub="Version 1.2.0 teams Komik up with KomiK Downloader: comics arrive with their details and every tag, folders fill your shelf on their own, and the reader feels smoother than ever."
          />
          <motion.div
            initial={{ scale: 0, rotate: -30 }}
            whileInView={{ scale: 1, rotate: -8 }}
            viewport={{ once: true }}
            transition={{ type: "spring", stiffness: 300, damping: 12, delay: 0.3 }}
            className="hidden h-36 w-36 shrink-0 flex-col items-center justify-center rounded-full border-[4px] border-black bg-cyan text-center font-bangers text-3xl leading-none text-black shadow-[6px_6px_0_#000] md:flex"
          >
            <span>1.2.0</span>
            <span className="text-xl">out now!</span>
          </motion.div>
        </div>
        <div className="mt-12 grid gap-6 lg:grid-cols-12">
          <ComicInfoPanel />
          <NoLimitPanel />
          <WatchedPanel />
          <RefreshPanel />
          <DuplicatesPanel />
          <TransitionPanel />
        </div>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          className="mt-8 flex flex-wrap items-center justify-center gap-2 font-mono text-[11px] font-black uppercase"
        >
          {["Comic-style Settings dialogs", "Bigger, sharper taskbar icon", "Larger title-bar crest", "Installer license page", "Updates close Komik safely"].map((t, i) => (
            <span key={t} className={`border-2 border-black px-2 py-1 shadow-[2px_2px_0_#000] ${["bg-amber", "bg-white", "bg-cyan", "bg-white", "bg-magenta text-white"][i]}`}>
              ✦ {t}
            </span>
          ))}
        </motion.div>
      </div>
    </section>
  );
}
