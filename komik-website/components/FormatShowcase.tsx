"use client";

import { APP_CONFIG } from "@/lib/config";
import { Cpu, RefreshCw, Layers, Check, ArrowRight, Shield, FolderOpen, HardDrive, FileArchive } from "lucide-react";

export default function FormatShowcase() {
  const formats = [
    {
      ext: ".CBZ",
      label: "UNIVERSAL STANDARD",
      engine: "System.IO.Compression",
      desc: "Standard comic ZIP archive with zero-latency streaming. Pages indexed in memory without disk unpacking.",
      color: "border-amber bg-[#161510] text-amber",
      accent: "bg-amber text-black",
      badge: "Pure .NET 8",
    },
    {
      ext: ".CBR",
      label: "MANAGED RAR4 & RAR5",
      engine: "SharpCompress Managed",
      desc: "Decodes both vintage RAR4 and modern RAR5 comics. Gracefully rejects password locks without crashing.",
      color: "border-crimson bg-[#181113] text-crimson",
      accent: "bg-crimson text-white",
      badge: "No unrar.dll",
    },
    {
      ext: ".CB7",
      label: "LZMA2 SOLID ARCHIVE",
      engine: "SevenZip Managed Engine",
      desc: "Full solid 7-Zip decompression with multi-threaded LZMA2 decoding. Zero external 7z CLI requirements.",
      color: "border-cyan bg-[#10151A] text-cyan",
      accent: "bg-cyan text-black",
      badge: "Multi-Threaded",
    },
    {
      ext: ".PDF",
      label: "VECTOR & SCAN RASTER",
      engine: "Docnet.Core + PDFium",
      desc: "High-DPI multi-page PDF rendering with sharp typography, smooth vector scaling, and natural page aspect preservation.",
      color: "border-[#FF6B00] bg-[#1A1410] text-[#FF6B00]",
      accent: "bg-[#FF6B00] text-black",
      badge: "Direct PDFium",
    },
    {
      ext: ".ZIP",
      label: "STANDARD ARCHIVES",
      engine: "Deflate & Store Stream",
      desc: "Directly opens standard compressed .zip archives containing graphic novel page scans.",
      color: "border-newsprint bg-[#131418] text-newsprint",
      accent: "bg-[#252834] text-white",
      badge: "Zero-Copy",
    },
    {
      ext: ".RAR",
      label: "MULTI-PART ARCHIVES",
      engine: "SharpCompress Streamer",
      desc: "Reads standard compressed and uncompressed RAR volumes without third-party utilities.",
      color: "border-newsprint bg-[#131418] text-newsprint",
      accent: "bg-[#252834] text-white",
      badge: "Native Managed",
    },
    {
      ext: ".7Z",
      label: "7-ZIP COMPRESSION",
      engine: "Managed 7z Decoder",
      desc: "Unpacks standard 7z multi-stream containers with memory-conscious stream extraction.",
      color: "border-newsprint bg-[#131418] text-newsprint",
      accent: "bg-[#252834] text-white",
      badge: "Memory-Tuned",
    },
    {
      ext: "FOLDERS",
      label: "RAW IMAGE DIRECTORIES",
      engine: "StrCmpLogicalW Natural Sort",
      desc: "Drop any directory of loose JPEGs, PNGs, WebPs, or AVIFs. Pages automatically sort in natural numeric order.",
      color: "border-amber bg-[#161510] text-amber",
      accent: "bg-amber text-black",
      badge: "Natural Sort",
    },
  ];

  return (
    <section id="formats" className="relative border-b-[3px] border-black bg-ink py-16 sm:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Dominant Collector's Panel Frame */}
        <div className="relative border-[3px] border-black bg-[#0C0C0F] p-6 sm:p-10 lg:p-12 shadow-[8px_8px_0px_#000000]">
          {/* Overlapping Diegetic Caption Box */}
          <div className="absolute -top-3.5 left-6 sm:left-10 z-20 caption-box-cyan px-3.5 py-1 text-xs font-black tracking-wider rotate-[0.6deg]">
            ARCHIVE ENGINE · PURE .NET EXTRACTION · NO EXTERNAL CODECS
          </div>

          <div className="flex flex-col lg:flex-row lg:items-end justify-between gap-6 mb-10">
            <div>
              <h2 className="font-display text-3xl sm:text-4xl lg:text-5xl font-black tracking-tight text-newsprint leading-[1.08]">
                Every format in your collection. <br />
                <span className="text-cyan underline decoration-cyan/30 underline-offset-8">
                  Zero external codecs.
                </span>
              </h2>
              <p className="mt-3 text-base text-newsprint/80 max-w-2xl font-medium">
                Tired of comic readers that crash on 7-Zip files, complain about missing unrar.dll,
                or choke on high-res PDFs? Komik embeds pure managed .NET engines for bulletproof decoding.
              </p>
            </div>

            <div className="flex items-center gap-2.5 font-mono text-xs text-newsprint border-2 border-black bg-black px-4 py-2 shadow-[3px_3px_0px_#000] self-start lg:self-auto font-bold">
              <Cpu className="h-4 w-4 text-amber" />
              <span>Managed .NET 8 Runtime</span>
            </div>
          </div>

          {/* Unified 8-Format Collector's Arsenal (Consistent Size & High Readability) */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            {formats.map((fmt, idx) => (
              <div
                key={fmt.ext}
                className="border-[2.5px] border-black p-5 bg-[#141419] flex flex-col justify-between shadow-[4px_4px_0px_#000000] group hover:-translate-x-0.5 hover:-translate-y-0.5 transition-all"
              >
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <span className={`px-2 py-0.5 text-[9px] font-mono font-black border border-black shadow-[1px_1px_0px_#000] ${fmt.accent}`}>
                      {fmt.badge}
                    </span>
                    <span className="text-[11px] font-mono font-bold text-muted">
                      #{idx + 1}
                    </span>
                  </div>

                  <div className="my-1.5">
                    <span className="font-display text-3xl sm:text-4xl font-black tracking-wider text-newsprint group-hover:text-amber transition-colors">
                      {fmt.ext}
                    </span>
                    <span className="block text-[11px] font-mono text-amber/90 font-bold uppercase tracking-wider mt-1">
                      {fmt.label}
                    </span>
                  </div>

                  <p className="text-xs text-newsprint/75 leading-relaxed mt-2 font-medium">
                    {fmt.desc}
                  </p>
                </div>

                <div className="mt-4 pt-3 border-t-2 border-black/80 flex items-center justify-between text-[11px] font-mono text-newsprint font-bold">
                  <span className="text-muted text-[10px] truncate max-w-[150px]">
                    {fmt.engine}
                  </span>
                  <span className="text-amber">✓ Native</span>
                </div>
              </div>
            ))}
          </div>

          {/* Integrated Universal Archive Converter (High-Impact Comic Lab Panel) */}
          <div className="mt-8 border-[3px] border-black bg-[#15151B] text-newsprint p-6 sm:p-8 lg:p-10 shadow-[6px_6px_0px_#000000] relative overflow-hidden">
            <div className="relative z-10 max-w-4xl">
              <div className="inline-flex items-center gap-2 px-3 py-1 border-2 border-black bg-crimson text-white text-xs font-mono font-black uppercase mb-4 shadow-[2px_2px_0px_#000]">
                <RefreshCw className="h-3.5 w-3.5 stroke-[2.5]" />
                <span>Lossless Batch Conversion</span>
              </div>

              <h3 className="font-display text-2xl sm:text-3xl lg:text-4xl font-black tracking-tight text-newsprint">
                Convert Any Archive, Folder, or PDF to Clean .CBZ
              </h3>

              <p className="mt-3 text-sm sm:text-base text-newsprint/85 leading-relaxed font-medium">
                Standardize your messy digital comic collection with a single right-click or drag-and-drop. Convert loose image folders, legacy CBR (RAR), CB7 (7-Zip), or multi-page PDFs directly into clean, portable, lossless CBZ archives with zero-padded natural sorting (<code className="bg-black text-amber px-1.5 py-0.5 font-mono text-xs font-bold border border-black">0001_Cover.jpg</code>, <code className="bg-black text-amber px-1.5 py-0.5 font-mono text-xs font-bold border border-black">0002_Page_02.png</code>).
              </p>

              {/* Conversion Diagram Strip */}
              <div className="mt-6 grid grid-cols-1 md:grid-cols-3 gap-3 font-mono text-xs">
                <div className="border-2 border-black bg-[#1B1C22] p-4 shadow-[3px_3px_0px_#000]">
                  <span className="block text-[10px] font-black uppercase text-crimson">STEP 1 · SOURCE</span>
                  <span className="font-bold text-newsprint text-sm block mt-0.5">Scans, CBR, CB7, PDF</span>
                  <p className="text-[11px] text-muted mt-1 leading-snug">Accepts chaotic archive structures, subfolders, and multi-page vector documents.</p>
                </div>

                <div className="border-2 border-black bg-amber text-black p-4 shadow-[3px_3px_0px_#000] flex flex-col justify-between">
                  <div>
                    <span className="block text-[10px] font-black uppercase text-black">STEP 2 · PROCESSING</span>
                    <span className="font-black text-black text-sm block mt-0.5">Lossless Zero-Copy Pass</span>
                  </div>
                  <p className="text-[11px] text-black/90 mt-1 font-medium leading-snug">Images re-packed without generational quality loss. Background thread with live progress.</p>
                </div>

                <div className="border-2 border-black bg-[#1B1C22] p-4 shadow-[3px_3px_0px_#000]">
                  <span className="block text-[10px] font-black uppercase text-cyan">STEP 3 · STANDARDIZED</span>
                  <span className="font-bold text-newsprint text-sm block mt-0.5">Standardized .CBZ</span>
                  <p className="text-[11px] text-muted mt-1 leading-snug">Clean numeric page ordering (<code className="text-amber">0001_Cover.jpg</code>). Portable to any modern viewer.</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
