"use client";

import { useRef } from "react";
import { motion, useInView } from "framer-motion";
import { Download, Github, Heart, Puzzle, ShieldCheck } from "lucide-react";
import Link from "next/link";
import { APP_CONFIG, EXTENSION_CONFIG } from "@/lib/config";
import KomikLogo from "@/components/KomikLogo";
import { Byte } from "@/components/comic/art";

export default function Footer() {
  const byteRef = useRef<HTMLDivElement>(null);
  const byteVisible = useInView(byteRef, { margin: "100px 0px" });
  return (
    <footer className="relative overflow-hidden bg-ink pb-10 pt-16">
      <div className="bg-halftone pointer-events-none absolute inset-0 opacity-20" />
      <div className="relative mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col items-center text-center">
          <motion.div ref={byteRef} animate={byteVisible ? { y: [0, -10, 0], rotate: [-4, 4, -4] } : undefined} transition={{ duration: 4, repeat: Infinity, ease: "easeInOut" }} aria-hidden>
            <svg viewBox="-60 -70 120 110" className="h-24 w-24">
              <Byte x={0} y={0} mood="wink" />
            </svg>
          </motion.div>
          <div className="balloon -mt-2 mb-6 px-4 py-1.5 text-sm">Thanks for reading!</div>
          <h2 className="text-comic-outline font-bangers text-[22vw] leading-[0.8] text-newsprint sm:text-[180px]" style={{ WebkitTextStroke: "4px #000", textShadow: "8px 8px 0 #FF1F6D" }}>
            The End
          </h2>
          <p className="mt-4 font-comic text-lg font-bold text-amber">…or is it? Your next issue is waiting on your PC.</p>
        </div>

        <div className="mt-14 flex flex-col items-start justify-between gap-6 border-t-[3px] border-black pt-8 md:flex-row md:items-center">
          <div className="flex items-center gap-3.5">
            <div className="flex h-11 w-11 items-center justify-center border-2 border-black bg-[#121316] shadow-[3px_3px_0_#FFD700]">
              <KomikLogo size={28} />
            </div>
            <div>
              <div className="font-bangers text-2xl leading-none tracking-wider text-newsprint">
                KOMIK {APP_CONFIG.version} <span className="text-cyan">+ Downloader {EXTENSION_CONFIG.version}</span>
              </div>
              <p className="max-w-md text-xs font-medium text-muted">{APP_CONFIG.oneLiner}</p>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-3 font-mono text-xs font-bold">
            <a href={APP_CONFIG.downloadUrl} className="btn-comic-primary inline-flex items-center gap-1.5 px-3 py-2 uppercase">
              <Download className="h-3.5 w-3.5" /> Download
            </a>
            <a href={APP_CONFIG.repoUrl} target="_blank" rel="noopener noreferrer" className="btn-comic-secondary inline-flex items-center gap-1.5 px-3 py-2">
              <Github className="h-3.5 w-3.5" /> GitHub
            </a>
            <Link href="/#extension" className="btn-comic-secondary inline-flex items-center gap-1.5 px-3 py-2">
              <Puzzle className="h-3.5 w-3.5" /> Extension
            </Link>
            <a href={APP_CONFIG.releasesUrl} target="_blank" rel="noopener noreferrer" className="btn-comic-secondary inline-flex items-center gap-1.5 px-3 py-2">
              Releases
            </a>
            <Link href="/privacy" className="btn-comic-secondary inline-flex items-center gap-1.5 px-3 py-2">
              <ShieldCheck className="h-3.5 w-3.5" /> Privacy
            </Link>
          </div>
        </div>

        <div className="mt-8 flex flex-col items-center justify-between gap-2 font-mono text-[11px] text-muted sm:flex-row">
          <span>
            Created &amp; maintained by <strong className="text-newsprint">{APP_CONFIG.author}</strong> · MIT License
          </span>
          <span className="inline-flex items-center gap-1">
            Built natively for Windows with WinUI 3 &amp; .NET 8 <Heart className="h-3 w-3 fill-magenta text-magenta" />
          </span>
        </div>
      </div>
    </footer>
  );
}
