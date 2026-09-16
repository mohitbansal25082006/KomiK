"use client";

import { Fragment, useEffect, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { ArrowRight, Chrome, Download, FolderInput, Github, ShieldCheck, ToggleRight } from "lucide-react";
import Link from "next/link";
import { APP_CONFIG, EXTENSION_CONFIG } from "@/lib/config";
import SectionHeading from "@/components/fx/SectionHeading";
import ComicBurst from "@/components/fx/ComicBurst";
import KomikLogo from "@/components/KomikLogo";
import BrowserDemo from "@/components/extension/BrowserDemo";
import ExtensionPanels from "@/components/extension/ExtensionPanels";

const TAPE = ["KOMIK DOWNLOADER", "CHROME", "EDGE", "BRAVE", "ANY SITE → CBZ", "EVERY TAG", "WHOLE SERIES", "FULL-SIZE PAGES", "RESUMES AFTER RESTART", "COLLECTS NOTHING"];

const FORMATS = [
  { f: "CBZ", d: "Best for Komik", c: "bg-amber" },
  { f: "ZIP", d: "Same, .zip name", c: "bg-cyan" },
  { f: "PDF", d: "Title & keywords", c: "bg-magenta text-white" },
  { f: "FOLDER", d: "Images + XML", c: "bg-paper" },
];

const STEPS = [
  { icon: Download, t: "Download the zip", d: `${EXTENSION_CONFIG.zipName} from the latest release, then extract it.` },
  { icon: ToggleRight, t: "Turn on Developer mode", d: "Open chrome://extensions (or edge://extensions) and flip the switch." },
  { icon: FolderInput, t: "Load unpacked", d: "Pick the extracted folder. Pin the crest to your toolbar and you're done." },
];

/** Keycaps that light up when the real shortcut is pressed on this page. */
function Shortcuts() {
  const [lit, setLit] = useState<number | null>(null);
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (!e.altKey || e.key.toLowerCase() !== "k") return;
      setLit(e.shiftKey ? 1 : 0);
      window.setTimeout(() => setLit(null), 900);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);
  return (
    <div className="space-y-2.5">
      {EXTENSION_CONFIG.shortcuts.map((s, i) => (
        <button
          key={s.label}
          type="button"
          onClick={() => {
            setLit(i);
            window.setTimeout(() => setLit(null), 900);
          }}
          className="relative flex w-full items-center gap-3 text-left"
        >
          <span className="flex gap-1">
            {s.keys.map((k) => (
              <motion.kbd
                key={k}
                animate={lit === i ? { y: 3, boxShadow: "0px 0px 0 #000" } : { y: 0, boxShadow: "3px 3px 0 #000" }}
                className={`min-w-[34px] border-2 border-black px-1.5 py-1 text-center font-mono text-[11px] font-black ${lit === i ? "bg-amber text-black" : "bg-white text-black"}`}
              >
                {k}
              </motion.kbd>
            ))}
          </span>
          <span className="text-sm font-semibold text-newsprint/85">{s.label}</span>
          <AnimatePresence>
            {lit === i && (
              <motion.span
                initial={{ scale: 0, rotate: -20 }}
                animate={{ scale: 1, rotate: -8 }}
                exit={{ scale: 0, opacity: 0 }}
                className="absolute -top-3 right-0 font-bangers text-2xl text-amber [-webkit-text-stroke:1.5px_#000] [paint-order:stroke_fill]"
              >
                {i === 0 ? "OPEN!" : "GRAB!"}
              </motion.span>
            )}
          </AnimatePresence>
        </button>
      ))}
      <p className="font-mono text-[10px] text-newsprint/50">Try it: press Alt + K right now.</p>
    </div>
  );
}

export default function ExtensionSection() {
  const storeLive = Boolean(EXTENSION_CONFIG.chromeStoreUrl);
  return (
    <section id="extension" className="relative scroll-mt-16 overflow-hidden border-b-[3px] border-black bg-ink pb-20 sm:pb-28">
      {/* backdrop */}
      <div aria-hidden className="pointer-events-none absolute inset-0">
        <div className="bg-speedlines absolute left-1/2 top-[18%] h-[1400px] w-[1400px] -translate-x-1/2 -translate-y-1/2 opacity-70" />
        <div className="absolute -right-40 top-40 h-[720px] w-[720px] bg-[radial-gradient(circle,rgba(0,194,255,0.2),transparent_65%)]" />
        <div className="absolute -left-60 bottom-40 h-[720px] w-[720px] bg-[radial-gradient(circle,rgba(255,215,0,0.12),transparent_65%)]" />
        <div className="bg-halftone-cyan absolute inset-0 opacity-25" />
      </div>

      {/* NEW! tape */}
      <div aria-hidden className="relative h-20 sm:h-24">
        <div className="absolute inset-x-[-5%] top-5 rotate-[-1.5deg] border-y-[3px] border-black bg-cyan py-2 shadow-[0_6px_0_#000]">
          <div className="animate-marquee flex w-max gap-8 whitespace-nowrap font-bangers text-2xl tracking-wider text-black sm:text-3xl">
            {[...TAPE, ...TAPE].map((t, i) => (
              <span key={i} className="flex items-center gap-8">
                {t} <span className="text-magenta">✦</span>
              </span>
            ))}
          </div>
        </div>
      </div>

      <div className="relative mx-auto mt-10 max-w-7xl px-4 sm:mt-14 sm:px-6 lg:px-8">
        <div className="flex flex-wrap items-end justify-between gap-6">
          <SectionHeading
            caption={`Special feature · ${EXTENSION_CONFIG.name} ${EXTENSION_CONFIG.version}`}
            title="Any site."
            accent="Straight to your shelf!"
            tone="cyan"
            sub={
              <>
                <strong className="text-newsprint">KomiK Downloader</strong> is a free extension for Chrome, Edge and Brave. It finds every page of the comic, manga or webtoon you&apos;re
                reading, saves it as a CBZ with the title, credits and every tag inside, and Komik {EXTENSION_CONFIG.pairsWith} files it into your library on its own.
              </>
            }
          />
          <motion.div
            initial={{ scale: 0, rotate: -40 }}
            whileInView={{ scale: 1, rotate: 10 }}
            viewport={{ once: true }}
            transition={{ type: "spring", stiffness: 260, damping: 12, delay: 0.25 }}
            className="hidden md:block"
          >
            <ComicBurst size={190} fill="#00C2FF" spikes={16} seed={7} className="animate-wobble">
              <span className="block font-bangers text-3xl leading-[0.9] text-black">
                FREE
                <br />
                <span className="text-xl">browser</span>
                <br />
                extension!
              </span>
            </ComicBurst>
          </motion.div>
        </div>

        <div className="mt-12">
          <BrowserDemo />
        </div>

        <div className="mt-16 flex flex-wrap items-end justify-between gap-4">
          <h3 className="text-comic-outline font-bangers text-5xl leading-none text-amber sm:text-6xl">
            Tricks up its sleeve
          </h3>
          <p className="max-w-md font-mono text-xs font-bold text-newsprint/70">Every panel below is live. Click around: this is exactly what the extension does on real sites.</p>
        </div>
        <div className="mt-8">
          <ExtensionPanels />
        </div>

        {/* ------------------------------- install ------------------------------- */}
        <div id="get-extension" className="mt-16 grid scroll-mt-24 gap-6 lg:grid-cols-12">
          <motion.div
            initial={{ opacity: 0, y: 40 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-10% 0px" }}
            transition={{ type: "spring", stiffness: 200, damping: 22 }}
            className="relative overflow-hidden border-[3px] border-black bg-cyan p-5 text-black shadow-[9px_9px_0_#000] sm:p-7 lg:col-span-7"
          >
            <div className="bg-halftone-paper pointer-events-none absolute inset-0 opacity-40" />
            <div className="relative">
              <div className="flex items-center gap-3">
                <div className="flex h-14 w-14 items-center justify-center border-[3px] border-black bg-[#121316] shadow-[4px_4px_0_#FFD700]">
                  <KomikLogo size={38} />
                </div>
                <div>
                  <div className="font-bangers text-4xl leading-none tracking-wide">Add it to your browser</div>
                  <div className="font-mono text-[11px] font-black uppercase">
                    v{EXTENSION_CONFIG.version} · {EXTENSION_CONFIG.browsers.join(" · ")} · {EXTENSION_CONFIG.zipSize}
                  </div>
                </div>
              </div>

              <div className="mt-6 flex flex-col gap-3 sm:flex-row">
                {storeLive ? (
                  <a href={EXTENSION_CONFIG.chromeStoreUrl} target="_blank" rel="noopener noreferrer" className="btn-comic-primary inline-flex items-center justify-center gap-2 px-6 py-4 font-bangers text-2xl tracking-wider">
                    <Chrome className="h-6 w-6" /> Add to Chrome
                  </a>
                ) : (
                  <div className="relative">
                    <span className="btn-comic-secondary inline-flex cursor-default items-center justify-center gap-2 px-5 py-4 font-bangers text-2xl tracking-wider opacity-80">
                      <Chrome className="h-6 w-6" /> Chrome Web Store
                    </span>
                    <span className="absolute -right-3 -top-3 rotate-6 border-2 border-black bg-magenta px-2 py-0.5 font-bangers text-sm tracking-wide text-white shadow-[2px_2px_0_#000]">
                      Coming soon!
                    </span>
                  </div>
                )}
                <motion.a
                  href={EXTENSION_CONFIG.zipUrl}
                  whileHover={{ scale: 1.04, rotate: -1 }}
                  whileTap={{ scale: 0.96 }}
                  className="btn-comic-primary inline-flex items-center justify-center gap-2 px-6 py-4 font-bangers text-2xl tracking-wider"
                >
                  <Download className="h-6 w-6 stroke-[2.5]" /> Download .zip
                </motion.a>
              </div>

              <div className="mt-6 grid grid-cols-2 gap-2 sm:grid-cols-4">
                {FORMATS.map((f, i) => (
                  <motion.div
                    key={f.f}
                    whileHover={{ y: -4, rotate: i % 2 ? 2 : -2 }}
                    className={`${f.c} border-[3px] border-black px-2 py-1.5 shadow-[3px_3px_0_#000]`}
                  >
                    <div className="font-bangers text-2xl leading-none">{f.f}</div>
                    <div className="text-[10px] font-bold uppercase">{f.d}</div>
                  </motion.div>
                ))}
              </div>

              <div className="mt-6 border-[3px] border-black bg-[#121216] p-4">
                <Shortcuts />
              </div>

              {/* how a download reaches the Komik shelf */}
              <div className="mt-6 grid grid-cols-[1fr_auto_1fr_auto_1fr] items-center gap-1.5 text-center sm:gap-2">
                {[
                  { k: "Save", v: "Downloads/KomiK", c: "bg-white" },
                  { k: "Watch", v: "Komik sees it", c: "bg-amber" },
                  { k: "Read", v: "On your shelf", c: "bg-magenta text-white" },
                ].map((s, i) => (
                  <Fragment key={s.k}>
                    {i > 0 && <ArrowRight className="h-5 w-5 shrink-0 stroke-[3]" aria-hidden />}
                    <motion.div
                      initial={{ opacity: 0, y: 12 }}
                      whileInView={{ opacity: 1, y: 0 }}
                      viewport={{ once: true }}
                      transition={{ delay: 0.2 + i * 0.12, type: "spring", stiffness: 320, damping: 20 }}
                      className={`${s.c} border-[3px] border-black px-1 py-2 shadow-[3px_3px_0_#000]`}
                    >
                      <div className="font-bangers text-xl leading-none tracking-wide sm:text-2xl">{s.k}</div>
                      <div className="mt-0.5 font-mono text-[9px] font-black uppercase leading-tight sm:text-[10px]">{s.v}</div>
                    </motion.div>
                  </Fragment>
                ))}
              </div>
              <p className="mt-3 text-center font-mono text-[11px] font-bold text-black/75">Add Downloads/KomiK as a watched folder in Komik once, and every download files itself.</p>
            </div>
          </motion.div>

          <motion.div
            initial={{ opacity: 0, y: 40 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: "-10% 0px" }}
            transition={{ type: "spring", stiffness: 200, damping: 22, delay: 0.1 }}
            className="flex flex-col gap-4 lg:col-span-5"
          >
            <div className="comic-panel-dark p-5">
              <div className="caption-box inline-block -rotate-1 px-2 py-0.5 text-[11px]">Install in three panels</div>
              <ol className="mt-4 space-y-3">
                {STEPS.map((s, i) => (
                  <motion.li
                    key={s.t}
                    initial={{ opacity: 0, x: 30 }}
                    whileInView={{ opacity: 1, x: 0 }}
                    viewport={{ once: true }}
                    transition={{ delay: 0.15 + i * 0.1, type: "spring", stiffness: 300, damping: 24 }}
                    className="flex gap-3"
                  >
                    <span className="flex h-10 w-10 shrink-0 items-center justify-center border-[3px] border-black bg-amber font-bangers text-2xl text-black shadow-[3px_3px_0_#000]">{i + 1}</span>
                    <div>
                      <div className="flex items-center gap-1.5 font-bangers text-xl leading-none tracking-wide">
                        <s.icon className="h-4 w-4 text-cyan" /> {s.t}
                      </div>
                      <p className="mt-1 text-sm text-newsprint/75">{s.d}</p>
                    </div>
                  </motion.li>
                ))}
              </ol>
              <div className="mt-5 flex flex-wrap gap-2">
                <a href={APP_CONFIG.releasesUrl} target="_blank" rel="noopener noreferrer" className="btn-comic-secondary inline-flex items-center gap-1.5 px-3 py-1.5 text-xs">
                  <Github className="h-3.5 w-3.5" /> All releases
                </a>
                <a href="#download" className="btn-comic-secondary inline-flex items-center gap-1.5 px-3 py-1.5 text-xs">
                  <Download className="h-3.5 w-3.5" /> Get the Komik app too
                </a>
              </div>
            </div>

            <Link
              href="/privacy#extension"
              className="group relative flex flex-1 items-center gap-4 overflow-hidden border-[3px] border-black bg-paper-grain p-5 text-black shadow-[7px_7px_0_#000] transition-transform hover:-translate-y-1"
            >
              <motion.div
                initial={{ scale: 2.2, rotate: -30, opacity: 0 }}
                whileInView={{ scale: 1, rotate: -12, opacity: 1 }}
                viewport={{ once: true }}
                transition={{ type: "spring", stiffness: 300, damping: 14, delay: 0.3 }}
                className="shrink-0 border-[4px] border-[#c8102e] px-3 py-1 text-center font-bangers text-2xl leading-none text-[#c8102e] [mask-image:radial-gradient(rgba(0,0,0,0.9)_60%,rgba(0,0,0,0.55))]"
              >
                COLLECTS
                <br />
                NOTHING
              </motion.div>
              <div>
                <div className="flex items-center gap-1.5 font-bangers text-2xl leading-none tracking-wide">
                  <ShieldCheck className="h-5 w-5" /> Private by design
                </div>
                <p className="mt-1 text-sm font-medium text-black/75">
                  Everything happens in your browser. No accounts, no analytics, no servers. Only the comic&apos;s own pages are downloaded.
                </p>
                <span className="mt-2 inline-flex items-center gap-1 font-mono text-[11px] font-black uppercase underline-offset-4 group-hover:underline">
                  Read the privacy policy <ArrowRight className="h-3.5 w-3.5" />
                </span>
              </div>
            </Link>
          </motion.div>
        </div>
      </div>
    </section>
  );
}
