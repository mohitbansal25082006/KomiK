"use client";

import { useRef, useState } from "react";
import { motion, useInView, useMotionTemplate, useMotionValue, useSpring } from "framer-motion";
import { ArrowRight, Cog, FileArchive, FileText, FolderOpen, RefreshCw, RotateCcw, Sparkles } from "lucide-react";
import SectionHeading from "@/components/fx/SectionHeading";

const FORMATS = [
  { ext: ".CBZ", name: "Comic Book ZIP", rarity: "Common", stars: 3, color: "#FFD700", ink: "#000", engine: "System.IO.Compression", badge: "Pure .NET 8", desc: "The universal standard. Pages stream straight from the archive, no unpacking to disk." },
  { ext: ".CBR", name: "Comic Book RAR", rarity: "Rare", stars: 4, color: "#FF1F6D", ink: "#fff", engine: "SharpCompress (managed)", badge: "No unrar.dll", desc: "Vintage RAR4 and modern RAR5 comics decode in pure managed code. Password locks fail gracefully." },
  { ext: ".CB7", name: "Comic Book 7-Zip", rarity: "Epic", stars: 4, color: "#00C2FF", ink: "#000", engine: "Managed 7-Zip decoder", badge: "LZMA / LZMA2", desc: "Solid 7-Zip archives with LZMA2 compression, with zero external 7z command-line tools." },
  { ext: ".PDF", name: "Portable Document", rarity: "Legendary", stars: 5, color: "#FF7A00", ink: "#000", engine: "Docnet.Core + PDFium", badge: "HQ raster", desc: "Scanned and vector PDFs rasterized page by page with natural aspect ratios preserved." },
  { ext: ".ZIP", name: "ZIP Archive", rarity: "Common", stars: 2, color: "#F5EFE3", ink: "#000", engine: "Deflate & store", badge: "Zero-copy", desc: "Plain .zip archives full of page scans open exactly like CBZ files." },
  { ext: ".RAR", name: "RAR Archive", rarity: "Uncommon", stars: 3, color: "#A78BFA", ink: "#000", engine: "SharpCompress streamer", badge: "RAR4 + RAR5", desc: "Standard RAR archives read directly, no third-party utilities or DLLs to install." },
  { ext: ".7Z", name: "7-Zip Archive", rarity: "Uncommon", stars: 3, color: "#2FD17A", ink: "#000", engine: "Managed 7z decoder", badge: "Memory-tuned", desc: "Standard 7z containers unpacked with memory-conscious stream extraction." },
  { ext: "DIR", name: "Image Folders", rarity: "Mythic", stars: 5, color: "#FFFFFF", ink: "#000", engine: "Natural sort comparer", badge: "JPG PNG WEBP BMP GIF", desc: "Point at any folder of loose images. Page 2 comes before page 10, the way numbers should sort." },
];

function FormatCard({ f, i }: { f: (typeof FORMATS)[number]; i: number }) {
  const ref = useRef<HTMLButtonElement>(null);
  const [flipped, setFlipped] = useState(false);
  const rx = useMotionValue(0);
  const ry = useMotionValue(0);
  const mx = useMotionValue(50);
  const my = useMotionValue(50);
  const srx = useSpring(rx, { stiffness: 250, damping: 20 });
  const sry = useSpring(ry, { stiffness: 250, damping: 20 });
  const sheen = useMotionTemplate`radial-gradient(circle at ${mx}% ${my}%, rgba(255,255,255,0.55), rgba(255,255,255,0) 45%)`;

  const onMove = (e: React.PointerEvent) => {
    if (e.pointerType !== "mouse" || !ref.current) return;
    const r = ref.current.getBoundingClientRect();
    const px = (e.clientX - r.left) / r.width;
    const py = (e.clientY - r.top) / r.height;
    ry.set((px - 0.5) * 22);
    rx.set(-(py - 0.5) * 22);
    mx.set(px * 100);
    my.set(py * 100);
  };
  const onLeave = () => {
    rx.set(0);
    ry.set(0);
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 60, rotate: i % 2 ? 6 : -6 }}
      whileInView={{ opacity: 1, y: 0, rotate: 0 }}
      viewport={{ once: true, margin: "-8% 0px" }}
      transition={{ type: "spring", stiffness: 220, damping: 20, delay: (i % 4) * 0.07 }}
      style={{ perspective: 1000 }}
    >
      <motion.button
        ref={ref}
        type="button"
        onPointerMove={onMove}
        onPointerLeave={onLeave}
        onClick={() => setFlipped((v) => !v)}
        aria-label={`${f.ext} ${f.name}. ${flipped ? "Showing details, click to flip back" : "Click to flip for engine details"}`}
        style={{ rotateX: srx, rotateY: sry, transformStyle: "preserve-3d" }}
        className="relative block aspect-[3/4] w-full text-left lg:aspect-[4/5]"
      >
        <motion.div
          animate={{ rotateY: flipped ? 180 : 0 }}
          transition={{ type: "spring", stiffness: 260, damping: 24 }}
          style={{ transformStyle: "preserve-3d" }}
          className="absolute inset-0"
        >
          {/* front */}
          <div className="absolute inset-0 flex flex-col overflow-hidden border-[3px] border-black shadow-[6px_6px_0_#000] [backface-visibility:hidden]" style={{ background: f.color, color: f.ink }}>
            <div className="flex items-center justify-between border-b-[3px] border-black bg-black px-2.5 py-1.5 font-mono text-[10px] font-bold uppercase text-white">
              <span>#{String(i + 1).padStart(2, "0")} · {f.rarity}</span>
              <span className="text-amber">{"★".repeat(f.stars)}</span>
            </div>
            <div className="relative flex flex-1 items-center justify-center overflow-hidden">
              <div className="bg-halftone-paper absolute inset-0 opacity-60" />
              <div className="bg-speedlines absolute inset-[-50%] opacity-60 [background:repeating-conic-gradient(from_0deg,rgba(0,0,0,0.08)_0deg_4deg,transparent_4deg_12deg)]" />
              <span className="relative font-bangers text-[3.4rem] leading-none tracking-wide drop-shadow-[4px_4px_0_rgba(0,0,0,0.9)] sm:text-6xl lg:text-7xl" style={{ WebkitTextStroke: "2px #000", color: "#fff" }}>
                {f.ext}
              </span>
            </div>
            <div className="border-t-[3px] border-black bg-white/90 px-3 py-2 text-black">
              <div className="font-bangers text-xl leading-none tracking-wide">{f.name}</div>
              <div className="mt-1 flex items-center gap-1 font-mono text-[9px] font-bold uppercase text-black/60">
                <RotateCcw className="h-3 w-3" /> Tap to flip
              </div>
            </div>
            <motion.div className="pointer-events-none absolute inset-0 mix-blend-soft-light" style={{ background: sheen }} />
          </div>
          {/* back */}
          <div className="absolute inset-0 flex flex-col justify-between border-[3px] border-black bg-ink p-3.5 text-newsprint shadow-[6px_6px_0_#000] [backface-visibility:hidden] [transform:rotateY(180deg)]">
            <div className="bg-halftone pointer-events-none absolute inset-0 opacity-40" />
            <div className="relative">
              <div className="font-bangers text-3xl leading-none" style={{ color: f.color === "#F5EFE3" || f.color === "#FFFFFF" ? "#FFD700" : f.color }}>
                {f.ext}
              </div>
              <p className="mt-2 text-[12px] font-medium leading-snug text-newsprint/85 sm:text-[13px]">{f.desc}</p>
            </div>
            <div className="relative space-y-1.5">
              <div className="inline-block border-2 border-black px-1.5 py-0.5 font-mono text-[9px] font-black uppercase text-black" style={{ background: f.color }}>
                {f.badge}
              </div>
              <div className="font-mono text-[10px] text-muted">ENGINE: {f.engine}</div>
            </div>
          </div>
        </motion.div>
      </motion.button>
    </motion.div>
  );
}

const INPUTS = [
  { label: "CBR", icon: FileArchive, color: "bg-magenta text-white" },
  { label: "CB7", icon: FileArchive, color: "bg-cyan text-black" },
  { label: "PDF", icon: FileText, color: "bg-[#FF7A00] text-black" },
  { label: "FOLDER", icon: FolderOpen, color: "bg-paper text-black" },
];

function ConverterMachine() {
  const ref = useRef<HTMLDivElement>(null);
  const active = useInView(ref, { margin: "200px 0px" });
  return (
    <div ref={ref} className="relative mt-14 overflow-hidden border-[3px] border-black bg-[#15151B] p-5 shadow-[8px_8px_0_#000] sm:p-8 lg:p-10">
      <div className="bg-halftone-cyan pointer-events-none absolute inset-0 opacity-25" />
      <div className="relative grid items-center gap-8 lg:grid-cols-12">
        <div className="lg:col-span-5">
          <div className="caption-box-magenta inline-flex items-center gap-2 px-3 py-1 text-xs">
            <RefreshCw className="h-3.5 w-3.5" /> Lossless converter
          </div>
          <h3 className="text-comic-outline mt-4 font-bangers text-5xl leading-[0.95] text-newsprint sm:text-6xl">
            The CBZ-O-Matic <span className="text-amber">3000</span>
          </h3>
          <p className="mt-4 text-sm font-medium leading-relaxed text-newsprint/80 sm:text-base">
            Standardize a messy collection straight from the Library toolbar. Loose image folders, CBR, CB7 and multi-page PDFs come out as clean, portable CBZ archives. Archive images
            are repacked <strong className="text-newsprint">without re-compression</strong>, pages get deterministic zero-padded names, and it all runs on a background thread with
            progress and cancel.
          </p>
          <div className="mt-5 flex flex-wrap gap-2 font-mono text-[11px] font-bold">
            {["0001.jpg", "0002.jpg", "0003.png", "…", "0120.webp"].map((n) => (
              <span key={n} className="border-2 border-black bg-black px-2 py-0.5 text-amber">
                {n}
              </span>
            ))}
          </div>
        </div>

        {/* machine */}
        <div className="lg:col-span-7">
          <div className="flex flex-col items-center gap-4 sm:flex-row sm:items-stretch sm:gap-3">
            <div className="grid w-full grid-cols-4 gap-2 sm:w-28 sm:grid-cols-1">
              {INPUTS.map((inp, i) => (
                <motion.div
                  key={inp.label}
                  animate={active ? { x: [0, 0, 18, 0], scale: [1, 1.08, 0.9, 1] } : undefined}
                  transition={{ duration: 2.4, repeat: Infinity, delay: i * 0.6, times: [0, 0.35, 0.6, 1] }}
                  className={`${inp.color} flex items-center justify-center gap-1 border-[3px] border-black px-2 py-2 font-bangers text-lg shadow-[3px_3px_0_#000]`}
                >
                  <inp.icon className="h-4 w-4" />
                  {inp.label}
                </motion.div>
              ))}
            </div>

            <div className="flex items-center text-amber max-sm:rotate-90">
              <ArrowRight className="h-8 w-8 animate-pulse" />
            </div>

            <div className="relative flex w-full flex-1 flex-col items-center justify-center border-[3px] border-black bg-amber p-4 text-black shadow-[5px_5px_0_#000] sm:min-h-[220px]">
              <div className="bg-halftone-paper absolute inset-0 opacity-50" />
              <div className="relative flex items-center gap-3">
                <Cog className="h-12 w-12 animate-spin [animation-duration:3s]" />
                <Cog className="-ml-4 mt-8 h-9 w-9 animate-spin [animation-direction:reverse] [animation-duration:2s]" />
              </div>
              <div className="relative mt-3 w-full max-w-[220px] border-[3px] border-black bg-black p-2">
                <div className="flex justify-between font-mono text-[10px] font-bold text-amber">
                  <span>REPACKING…</span>
                  <span>LOSSLESS</span>
                </div>
                <div className="mt-1.5 h-3 overflow-hidden border-2 border-amber/40 bg-[#222]">
                  <motion.div
                    className="h-full bg-[repeating-linear-gradient(45deg,#FFD700_0_8px,#FF1F6D_8px_16px)]"
                    animate={active ? { width: ["0%", "100%"] } : undefined}
                    transition={{ duration: 2.4, repeat: Infinity, ease: "easeInOut" }}
                  />
                </div>
              </div>
              <div className="relative mt-2 font-bangers text-xl tracking-wider">Zero quality loss</div>
            </div>

            <div className="flex items-center text-amber max-sm:rotate-90">
              <ArrowRight className="h-8 w-8 animate-pulse" />
            </div>

            <div className="relative flex w-40 items-center justify-center sm:w-32">
              {[2, 1, 0].map((k) => (
                <motion.div
                  key={k}
                  className="absolute flex h-36 w-28 flex-col items-center justify-center border-[3px] border-black bg-cyan font-bangers text-3xl text-black shadow-[4px_4px_0_#000]"
                  style={{ rotate: (k - 1) * 7, x: (k - 1) * 8 }}
                  animate={k === 0 && active ? { y: [20, -6, 0], scale: [0.6, 1.1, 1], opacity: [0, 1, 1] } : undefined}
                  transition={k === 0 ? { duration: 2.4, repeat: Infinity, times: [0, 0.3, 0.45] } : undefined}
                >
                  .CBZ
                  <span className="mt-1 font-mono text-[9px] font-bold">STANDARDIZED</span>
                  {k === 0 && <Sparkles className="absolute -right-3 -top-3 h-6 w-6 fill-amber text-black" />}
                </motion.div>
              ))}
              <div className="h-36" />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function FormatShowcase() {
  return (
    <section id="formats" className="relative scroll-mt-16 overflow-hidden border-b-[3px] border-black bg-[#0B0B10] py-20 sm:py-28">
      <div className="bg-halftone pointer-events-none absolute inset-0 opacity-30" />
      <div className="pointer-events-none absolute -right-60 top-0 h-[720px] w-[720px] bg-[radial-gradient(circle,rgba(0,194,255,0.1),transparent_65%)]" />
      <div className="relative mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <SectionHeading
          caption="Chapter 02 · The archive vault"
          title="Every format."
          accent="Zero codecs."
          tone="cyan"
          sub={
            <>
              Tired of readers that choke on 7-Zip files or demand a missing unrar.dll? Komik ships pure managed .NET decoders for all eight formats.{" "}
              <span className="font-bold text-cyan">Collect them all</span>: hover to tilt, click to flip.
            </>
          }
        />
        <div className="mt-12 grid grid-cols-2 gap-4 sm:gap-6 lg:grid-cols-4">
          {FORMATS.map((f, i) => (
            <FormatCard key={f.ext} f={f} i={i} />
          ))}
        </div>
        <ConverterMachine />
      </div>
    </section>
  );
}
