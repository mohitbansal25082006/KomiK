"use client";

import { motion } from "framer-motion";
import { ArrowUp, Cpu, Download, ExternalLink, HardDrive, Scale, ShieldAlert } from "lucide-react";
import { APP_CONFIG } from "@/lib/config";
import ComicBurst from "@/components/fx/ComicBurst";

const SPECS = [
  { icon: HardDrive, k: "Installer", v: `${APP_CONFIG.installerSize}, self-contained`, c: "bg-amber" },
  { icon: Cpu, k: "Platform", v: "Windows 10 1809+ / 11, x64", c: "bg-cyan" },
  { icon: Scale, k: "License", v: "MIT open source", c: "bg-magenta text-white" },
];

export default function DownloadSection() {
  return (
    <section id="download" className="relative scroll-mt-16 overflow-hidden border-b-[3px] border-black bg-magenta py-20 sm:py-28">
      {/* sunburst */}
      <div aria-hidden className="pointer-events-none absolute left-1/2 top-1/2 h-[150vmax] w-[150vmax] -translate-x-1/2 -translate-y-1/2">
        <div className="animate-spin-slow h-full w-full [background:repeating-conic-gradient(from_0deg,#FF4D8B_0deg_6deg,#FF1F6D_6deg_12deg)]" />
      </div>
      <div className="bg-halftone-paper pointer-events-none absolute inset-0 opacity-40" />

      <div className="relative mx-auto max-w-5xl px-4 text-center sm:px-6 lg:px-8">
        <motion.div
          initial={{ scale: 0, rotate: -20 }}
          whileInView={{ scale: 1, rotate: -3 }}
          viewport={{ once: true }}
          transition={{ type: "spring", stiffness: 260, damping: 14 }}
          className="caption-box inline-block px-3 py-1 text-xs"
        >
          Final panel · Get Komik
        </motion.div>

        <motion.h2
          initial={{ y: 60, opacity: 0 }}
          whileInView={{ y: 0, opacity: 1 }}
          viewport={{ once: true }}
          transition={{ type: "spring", stiffness: 180, damping: 18 }}
          className="text-comic-outline mt-6 font-bangers text-[17vw] leading-[0.85] text-amber sm:text-9xl"
          style={{ WebkitTextStroke: "4px #000", textShadow: "8px 8px 0 #000" }}
        >
          Start reading
          <br />
          <span className="text-white">in seconds!</span>
        </motion.h2>

        <p className="relative z-20 mx-auto mt-6 max-w-xl text-base font-bold text-white sm:text-lg">
          A self-contained installer that bundles the .NET 8 and Windows App SDK runtimes. No Developer Mode, no manual certificates, no background services.
        </p>

        <div className="relative mt-14 flex flex-col items-center justify-center gap-6 sm:flex-row">
          <div className="relative">
            <div aria-hidden className="pointer-events-none absolute left-1/2 top-1/2 hidden -translate-x-1/2 -translate-y-1/2 sm:block">
              <ComicBurst size={250} fill="#FFD700" spikes={18} seed={9} className="animate-wobble" />
            </div>
            <motion.a
              href={APP_CONFIG.downloadUrl}
              whileHover={{ scale: 1.06, rotate: -2 }}
              whileTap={{ scale: 0.95, rotate: 2 }}
              className="btn-comic-primary relative z-10 inline-flex items-center gap-3 px-8 py-5 font-bangers text-3xl tracking-wider sm:text-4xl"
            >
              <Download className="h-8 w-8 stroke-[3]" />
              Download .exe
            </motion.a>
          </div>
          <a href={APP_CONFIG.releasesUrl} target="_blank" rel="noopener noreferrer" className="btn-comic-secondary relative z-10 inline-flex items-center gap-2 px-6 py-4 text-sm">
            Releases &amp; changelog <ExternalLink className="h-4 w-4" />
          </a>
        </div>

        <div className="mt-14 grid gap-4 text-left sm:grid-cols-3">
          {SPECS.map((s, i) => (
            <motion.div
              key={s.k}
              initial={{ opacity: 0, y: 40, rotate: i === 1 ? 4 : -4 }}
              whileInView={{ opacity: 1, y: 0, rotate: i === 1 ? 1 : -1 }}
              viewport={{ once: true }}
              transition={{ type: "spring", stiffness: 240, damping: 18, delay: i * 0.08 }}
              className={`${s.c} border-[3px] border-black p-4 text-black shadow-[6px_6px_0_#000]`}
            >
              <s.icon className="h-6 w-6" />
              <div className="mt-2 font-mono text-[11px] font-black uppercase opacity-80">{s.k}</div>
              <div className="font-bangers text-2xl leading-tight tracking-wide">{s.v}</div>
            </motion.div>
          ))}
        </div>

        <motion.div
          initial={{ opacity: 0, rotate: 3, y: 30 }}
          whileInView={{ opacity: 1, rotate: -0.5, y: 0 }}
          viewport={{ once: true }}
          className="relative mx-auto mt-12 max-w-3xl border-[3px] border-black bg-paper-grain p-5 text-left text-black shadow-[6px_6px_0_#000]"
        >
          <div className="absolute -top-3 left-1/2 h-6 w-28 -translate-x-1/2 rotate-2 bg-amber/80 shadow-sm" aria-hidden />
          <div className="flex items-start gap-4">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center border-2 border-black bg-amber">
              <ShieldAlert className="h-5 w-5" />
            </span>
            <div className="text-sm leading-relaxed">
              <strong className="font-bangers text-2xl tracking-wide">About &quot;Windows protected your PC&quot;</strong>
              <p className="mt-1 font-medium text-black/80">
                Komik is free, open-source software distributed without a costly code-signing certificate, so SmartScreen may warn you the first time you run{" "}
                <code className="bg-black/10 px-1 font-mono font-bold">{APP_CONFIG.installerName}</code>. Click <mark className="bg-amber px-1 font-bold">More info</mark> and then{" "}
                <mark className="bg-amber px-1 font-bold">Run anyway</mark>. Every line of code is public on GitHub.
              </p>
            </div>
          </div>
        </motion.div>

        <a href="#reader" className="mt-10 inline-flex items-center gap-2 font-mono text-xs font-black uppercase text-white underline-offset-4 hover:underline">
          <ArrowUp className="h-4 w-4" /> Or finish reading the demo comic first
        </a>
      </div>
    </section>
  );
}
