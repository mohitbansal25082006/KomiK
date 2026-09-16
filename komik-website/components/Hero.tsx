"use client";

import { useRef } from "react";
import { motion } from "framer-motion";
import { ArrowDown, ArrowRight, Download, Github, Hand, Keyboard, Maximize2, Monitor, Puzzle } from "lucide-react";
import { APP_CONFIG } from "@/lib/config";
import { gsap, SplitText, useGSAP, prefersReducedMotion } from "@/lib/gsap";
import ReaderMockup from "@/components/reader/ReaderMockup";
import ComicBurst from "@/components/fx/ComicBurst";

const TAPE = ["CBZ", "CBR", "CB7", "PDF", "ZIP", "RAR", "7Z", "IMAGE FOLDERS", "BROWSER EXTENSION", "EVERY TAG", "WEBTOON MODE", "MANGA RTL", "NIGHT MODE", "OFFLINE OCR", "SERIES & VOLUMES", "READING STATS"];

export default function Hero() {
  const sectionRef = useRef<HTMLElement>(null);
  const headRef = useRef<HTMLHeadingElement>(null);
  const stageRef = useRef<HTMLDivElement>(null);
  useGSAP(
    () => {
      if (prefersReducedMotion() || !headRef.current) return;
      let split: SplitText | null = null;
      document.fonts.ready.then(() => {
        if (!headRef.current) return;
        split = SplitText.create(headRef.current.querySelectorAll("[data-split]"), {
          type: "chars",
          charsClass: "inline-block will-change-transform",
        });
        gsap.from(split.chars, {
          yPercent: -140,
          rotate: () => gsap.utils.random(-40, 40),
          scale: 1.6,
          opacity: 0,
          duration: 0.9,
          ease: "elastic.out(1, 0.55)",
          stagger: { each: 0.035, from: "start" },
          delay: 0.35,
        });
      });
      gsap.from("[data-hero-pop]", { scale: 0, rotate: -25, opacity: 0, duration: 0.7, ease: "back.out(2.2)", stagger: 0.12, delay: 1.1 });
      gsap.from("[data-hero-rise]", { y: 30, opacity: 0, duration: 0.7, ease: "power3.out", stagger: 0.08, delay: 0.9 });
      return () => split?.revert();
    },
    { scope: sectionRef },
  );

  return (
    <section ref={sectionRef} id="top" className="relative overflow-hidden border-b-[3px] border-black bg-ink">
      {/* ---------- animated backdrop ---------- */}
      <div aria-hidden className="pointer-events-none absolute inset-0">
        <div className="absolute left-1/2 top-[22%] h-[170vmax] w-[170vmax] -translate-x-1/2 -translate-y-1/2">
          <div className="bg-speedlines animate-spin-slow h-full w-full" />
        </div>
        <div className="absolute -left-60 -top-20 h-[760px] w-[760px] bg-[radial-gradient(circle,rgba(255,31,109,0.22),transparent_65%)]" />
        <div className="absolute -right-60 top-20 h-[760px] w-[760px] bg-[radial-gradient(circle,rgba(0,194,255,0.18),transparent_65%)]" />
        <div className="bg-halftone-fade absolute inset-0" />
        <div className="absolute inset-x-0 bottom-0 h-64 bg-gradient-to-t from-ink to-transparent" />
      </div>

      <div className="relative mx-auto max-w-7xl px-4 pb-10 pt-10 sm:px-6 sm:pt-14 lg:px-8">
        {/* ---------- headline block ---------- */}
        <div className="relative mx-auto max-w-5xl text-center">
          {/* GSAP moves the wrapper; the link keeps its own hover transform */}
          <div data-hero-rise className="mb-4 flex justify-center">
            <a
              href="#extension"
              className="group flex w-fit items-center gap-2 border-[3px] border-black bg-cyan py-1 pl-1 pr-3 font-bold text-black shadow-[4px_4px_0_#000] transition-transform hover:-translate-y-0.5 hover:rotate-[-1deg]"
            >
              <span className="flex items-center gap-1 border-2 border-black bg-magenta px-1.5 py-0.5 font-bangers text-sm leading-none tracking-wide text-white">
                <Puzzle className="h-3.5 w-3.5" /> NEW
              </span>
              <span className="text-xs sm:text-sm">KomiK Downloader: save comics from any site, tags and all</span>
              <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-1" />
            </a>
          </div>
          <div data-hero-rise className="caption-box mx-auto inline-flex -rotate-1 items-center gap-2 px-3 py-1 text-[11px] sm:text-xs">
            <span className="h-2 w-2 animate-pulse rounded-full bg-magenta" />
            Issue {APP_CONFIG.version} · Windows 10 &amp; 11 · Free forever
          </div>

          <h1 ref={headRef} className="mt-6 font-bangers leading-[0.9] tracking-wide" aria-label="Your comics. Your PC. Zero cloud.">
            <span data-split className="text-comic-outline block text-[15vw] text-amber sm:text-[96px] lg:text-[128px]" style={{ WebkitTextStroke: "4px #000" }}>
              Your comics.
            </span>
            <span data-split className="text-comic-outline block text-[15vw] text-cyan sm:text-[96px] lg:text-[128px]" style={{ WebkitTextStroke: "4px #000" }}>
              Your PC.
            </span>
            <span data-split className="text-comic-outline block text-[15vw] text-magenta sm:text-[96px] lg:text-[128px]" style={{ WebkitTextStroke: "4px #000" }}>
              Zero cloud.
            </span>
          </h1>

          {/* draggable stickers */}
          <div data-hero-pop className="absolute -left-2 top-24 hidden lg:block">
          <motion.div drag dragSnapToOrigin whileDrag={{ scale: 1.15, rotate: 8 }} className="cursor-grab active:cursor-grabbing" title="Drag me!">
            <ComicBurst size={150} fill="#FFD700" spikes={14} className="animate-wobble">
              <span className="font-bangers text-2xl leading-none text-black">
                100%
                <br />
                OFFLINE!
              </span>
            </ComicBurst>
          </motion.div>
          </div>
          <div data-hero-pop className="absolute -right-2 top-52 hidden lg:block">
          <motion.div drag dragSnapToOrigin whileDrag={{ scale: 1.15, rotate: -8 }} className="cursor-grab active:cursor-grabbing" title="Drag me!">
            <div className="animate-float flex h-32 w-32 rotate-12 items-center justify-center rounded-full border-[3px] border-black bg-magenta text-center shadow-[5px_5px_0_#000]">
              <span className="font-bangers text-3xl leading-none text-white">
                FREE
                <br />
                <span className="text-xl">&amp; open source</span>
              </span>
            </div>
          </motion.div>
          </div>
          <div data-hero-pop className="absolute -right-10 top-14 hidden xl:block">
          <motion.div drag dragSnapToOrigin whileDrag={{ scale: 1.1 }} className="cursor-grab active:cursor-grabbing" title="Drag me!">
            <div className="-rotate-6 border-[3px] border-black bg-cyan px-3 py-1.5 font-bangers text-xl text-black shadow-[4px_4px_0_#000]">NEW! Browser extension</div>
          </motion.div>
          </div>

          <p data-hero-rise className="mx-auto mt-7 max-w-2xl text-base font-medium leading-relaxed text-newsprint/85 sm:text-lg">
            Komik is a fast, native Windows reader for comics, manga &amp; webtoons. Pure .NET archive loaders, two-page spreads, manga RTL, a parallel Webtoon engine,
            offline OCR and reading stats. <strong className="text-newsprint">No accounts. No telemetry. No network calls.</strong>
          </p>

          <div data-hero-rise className="mt-8 flex flex-col items-center justify-center gap-4 sm:flex-row">
            <a href={APP_CONFIG.downloadUrl} className="btn-comic-primary group inline-flex w-full items-center justify-center gap-3 px-8 py-4 text-base uppercase tracking-wider sm:w-auto">
              <Download className="h-5 w-5 stroke-[2.5] transition-transform group-hover:translate-y-0.5" />
              Download for Windows
              <span className="hidden whitespace-nowrap rounded-sm bg-black px-1.5 py-0.5 font-mono text-[10px] text-amber sm:inline">{APP_CONFIG.installerSize}</span>
            </a>
            <a href="#reader" className="btn-comic-secondary inline-flex w-full items-center justify-center gap-2 px-6 py-4 text-sm tracking-wide sm:w-auto">
              Read the demo comic
              <ArrowDown className="h-4 w-4 animate-bounce" />
            </a>
            <a
              href={APP_CONFIG.repoUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="btn-comic-secondary hidden items-center justify-center gap-2 px-4 py-4 text-sm sm:inline-flex"
              aria-label="View source on GitHub"
            >
              <Github className="h-5 w-5" />
            </a>
          </div>

          <div data-hero-rise className="mx-auto mt-5 flex max-w-md items-center justify-center gap-2 border-2 border-black bg-paper px-3 py-2 text-left font-mono text-[11px] font-bold text-black shadow-[3px_3px_0_#000] md:hidden">
            <Monitor className="h-4 w-4 shrink-0" />
            Komik is a Windows desktop app. Read the demo here, install on your PC.
          </div>
        </div>

        {/* ---------- the interactive reader ---------- */}
        <div id="reader" ref={stageRef} className="relative mx-auto mt-16 max-w-6xl scroll-mt-24 sm:mt-28">
          <div className="pointer-events-none absolute -top-[4.5rem] left-2 z-10 hidden sm:block">
            <div className="balloon -rotate-3 px-5 py-2 text-sm">Psst! This reader actually works. Go on, read the whole comic!</div>
          </div>
          <div className="pointer-events-none absolute -right-3 -top-12 z-10 hidden rotate-6 lg:block">
            <div className="caption-box-magenta px-3 py-1 text-xs">12-page original comic inside</div>
          </div>

          <motion.div
            data-no-burst
            initial={{ opacity: 0, y: 48, scale: 0.97 }}
            whileInView={{ opacity: 1, y: 0, scale: 1 }}
            viewport={{ once: true, amount: 0.05 }}
            transition={{ duration: 0.8, ease: [0.22, 1, 0.36, 1] }}
          >
            <ReaderMockup />
          </motion.div>

          <div className="mt-6 flex flex-wrap items-center justify-center gap-2 font-mono text-[11px] font-bold text-newsprint/80">
            <span className="inline-flex items-center gap-1.5 border-2 border-black bg-black/70 px-2.5 py-1">
              <Keyboard className="h-3.5 w-3.5 text-amber" /> Click the reader, then ← → · D · V · W/H/A · Ctrl+R · Ctrl+F
            </span>
            <span className="inline-flex items-center gap-1.5 border-2 border-black bg-black/70 px-2.5 py-1">
              <Hand className="h-3.5 w-3.5 text-cyan" /> Swipe to turn · pinch to zoom
            </span>
            <span className="inline-flex items-center gap-1.5 border-2 border-black bg-black/70 px-2.5 py-1">
              <Maximize2 className="h-3.5 w-3.5 text-magenta" /> Full screen for the best read
            </span>
          </div>
        </div>
      </div>

      {/* ---------- crossing tape marquees ---------- */}
      <div aria-hidden className="relative -mb-2 mt-10 h-28 sm:h-32">
        <div className="absolute inset-x-[-5%] top-4 -rotate-2 border-y-[3px] border-black bg-amber py-2.5 shadow-[0_6px_0_#000]">
          <div className="animate-marquee flex w-max gap-8 whitespace-nowrap font-bangers text-2xl tracking-wider text-black sm:text-3xl">
            {[...TAPE, ...TAPE].map((t, i) => (
              <span key={i} className="flex items-center gap-8">
                {t} <span className="text-magenta">✦</span>
              </span>
            ))}
          </div>
        </div>
        <div className="absolute inset-x-[-5%] top-16 rotate-1 border-y-[3px] border-black bg-magenta py-2 sm:top-[4.5rem]">
          <div className="animate-marquee-reverse flex w-max gap-8 whitespace-nowrap font-bangers text-xl tracking-wider text-white sm:text-2xl">
            {[...TAPE, ...TAPE].reverse().map((t, i) => (
              <span key={i} className="flex items-center gap-8">
                {t} <span className="text-amber">★</span>
              </span>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
