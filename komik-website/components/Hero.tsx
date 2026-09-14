"use client";

import { useState } from "react";
import { motion } from "framer-motion";
import {
  Download,
  Moon,
  Sun,
  BookOpen,
  ArrowRight,
  CheckCircle2,
  Sparkles,
  Maximize2,
  FolderOpen,
  ZoomIn,
  ZoomOut,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  Bookmark,
  SlidersHorizontal,
} from "lucide-react";
import { APP_CONFIG } from "@/lib/config";
import { ComicCoverPage, ComicStoryPageA, ComicStoryPageB } from "./MockComicPages";
import KomikLogo from "./KomikLogo";

export default function Hero() {
  // Live interactive controls for the WinUI 3 Canvas Mockup
  const [nightMode, setNightMode] = useState(false);
  const [spreadMode, setSpreadMode] = useState<"single" | "spread">("spread");
  const [fitMode, setFitMode] = useState<"height" | "width">("height");
  const [readingDirection, setReadingDirection] = useState<"ltr" | "rtl">("ltr");
  const [currentPage, setCurrentPage] = useState(14);
  const [zoomLevel, setZoomLevel] = useState(100);
  const totalPages = 32;

  // Spring animation variants for authentic comic panel drop-in
  const panelDrop = {
    hidden: { opacity: 1, y: 15, rotate: -0.4 },
    visible: {
      opacity: 1,
      y: 0,
      rotate: 0,
      transition: { type: "spring" as const, stiffness: 260, damping: 24 },
    },
  };

  const canvasDrop = {
    hidden: { opacity: 1, y: 20, rotate: 0.5 },
    visible: {
      opacity: 1,
      y: 0,
      rotate: 0,
      transition: { type: "spring" as const, stiffness: 240, damping: 22, delay: 0.05 },
    },
  };

  // Turn page helpers
  const handlePrevious = () => {
    if (currentPage > 1) {
      setCurrentPage((prev) => (spreadMode === "spread" && prev > 2 ? prev - 2 : prev - 1));
    }
  };

  const handleNext = () => {
    if (currentPage < totalPages) {
      setCurrentPage((prev) => (spreadMode === "spread" && prev === 1 ? 2 : prev + (spreadMode === "spread" ? 2 : 1)));
    }
  };

  return (
    <section className="relative overflow-hidden border-b-[3px] border-black bg-ink pt-12 pb-20 lg:pt-16 lg:pb-24">
      {/* Background Halftone Screen-Tone Matrix */}
      <div className="absolute inset-0 bg-halftone pointer-events-none opacity-25" />

      <div className="relative mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Splash Spread Frame */}
        <div className="relative border-[3px] border-black bg-[#0F0F12] p-6 sm:p-10 lg:p-12 shadow-[8px_8px_0px_#000000]">
          {/* Overlapping Diegetic Caption Box pinned directly to panel frame */}
          <div className="absolute -top-3.5 left-6 sm:left-10 z-20 caption-box px-3.5 py-1 text-xs font-black tracking-wider rotate-[-0.8deg]">
            SPLASH PAGE · ISSUE #01 · WINDOWS 11 NATIVE
          </div>

          <div className="grid grid-cols-1 gap-12 lg:grid-cols-12 lg:gap-10 items-center">
            {/* Left Column: Comic Narrative Typography & Action */}
            <motion.div
              variants={panelDrop}
              initial="hidden"
              animate="visible"
              className="lg:col-span-5 flex flex-col justify-center"
            >
              <h1 className="font-display text-4xl sm:text-5xl lg:text-6xl font-black tracking-tight text-newsprint leading-[1.04]">
                The comic reader <br />
                <span className="inline-block bg-amber text-black px-3 py-0.5 border-2 border-black mt-2 shadow-[3px_3px_0px_#000000] rotate-[-0.5deg]">
                  built for your PC.
                </span>
              </h1>

              <p className="mt-6 text-base sm:text-lg text-newsprint/85 leading-relaxed font-medium">
                A high-performance Windows desktop app engineered purely for reading
                comics, manga, and graphic novels. Pure .NET archive loaders, real-time
                color correction, automatic session progress, and a native Fluent Mica canvas.
              </p>

              {/* Action Buttons */}
              <div className="mt-8 flex flex-col sm:flex-row items-start sm:items-center gap-4">
                <a
                  href={APP_CONFIG.downloadUrl}
                  className="btn-comic-primary w-full sm:w-auto inline-flex items-center justify-center gap-3 px-8 py-4 text-base font-black tracking-wider uppercase"
                >
                  <Download className="h-5 w-5 stroke-[2.5]" />
                  <span>Download for Windows</span>
                </a>

                <a
                  href="#reading-engine"
                  className="btn-comic-secondary w-full sm:w-auto inline-flex items-center justify-center gap-2 px-6 py-4 text-sm font-bold tracking-wide"
                >
                  <span>Explore Reading Engine</span>
                  <ArrowRight className="h-4 w-4" />
                </a>
              </div>

              {/* Collector's Guarantees */}
              <div className="mt-8 pt-6 border-t-2 border-black/80 flex flex-wrap items-center gap-y-2.5 gap-x-6 text-xs font-mono text-muted">
                <span className="inline-flex items-center gap-1.5 text-newsprint font-bold">
                  <CheckCircle2 className="h-4 w-4 text-amber" />
                  Free & Open Source
                </span>
                <span className="inline-flex items-center gap-1.5 text-newsprint font-bold">
                  <CheckCircle2 className="h-4 w-4 text-cyan" />
                  Windows 10 / 11 64-bit
                </span>
                <span className="inline-flex items-center gap-1.5 text-newsprint font-bold">
                  <CheckCircle2 className="h-4 w-4 text-amber" />
                  100% Offline · Zero Accounts
                </span>
              </div>
            </motion.div>

            {/* Right Column: Pixel-Accurate WinUI 3 Application Mockup */}
            <motion.div
              variants={canvasDrop}
              initial="hidden"
              animate="visible"
              className="lg:col-span-7"
            >
              <div className="relative">
                {/* Windows 11 Native Window Frame with Rounded Top & Mica Backdrop */}
                <div className="rounded-t-xl border-[3px] border-black bg-[#121316]/95 backdrop-blur-xl shadow-[10px_10px_0px_#000000] overflow-hidden flex flex-col">
                  {/* 1. WINDOWS 11 TITLE BAR (36px high, native Windows window controls) */}
                  <div className="flex h-9 items-center justify-between border-b border-white/10 bg-[#16171B]/90 px-3 select-none">
                    {/* App Icon + Window Title */}
                    <div className="flex items-center gap-2">
                      <div className="h-4 w-4 bg-[#121316] border border-white/20 flex items-center justify-center shadow-[1px_1px_0px_#000]">
                        <KomikLogo size={13} />
                      </div>
                      <span className="text-xs font-sans font-semibold text-newsprint/90 truncate max-w-[200px] sm:max-w-xs">
                        Cyberpunk_Chronicles_#01.cbz — Komik
                      </span>
                    </div>

                    {/* Windows 11 Window Controls (Minimize, Maximize, Close - NOT macOS traffic lights!) */}
                    <div className="flex items-center">
                      {/* Minimize Button */}
                      <button
                        type="button"
                        aria-label="Minimize"
                        className="h-9 w-11 flex items-center justify-center text-newsprint/70 hover:bg-white/10 transition-colors"
                      >
                        <span className="w-2.5 h-[1px] bg-current" />
                      </button>

                      {/* Maximize Button */}
                      <button
                        type="button"
                        aria-label="Maximize"
                        className="h-9 w-11 flex items-center justify-center text-newsprint/70 hover:bg-white/10 transition-colors"
                      >
                        <span className="w-2.5 h-2.5 border border-current" />
                      </button>

                      {/* Close Button (Turns red on hover like real Windows 11) */}
                      <button
                        type="button"
                        aria-label="Close"
                        className="h-9 w-11 flex items-center justify-center text-newsprint/70 hover:bg-[#E81123] hover:text-white transition-colors"
                      >
                        <span className="text-xs font-light">✕</span>
                      </button>
                    </div>
                  </div>

                  {/* 2. MAIN READING CANVAS VIEWPORT (Host of the comic pages + Mica acrylic background) */}
                  <div className="relative min-h-[360px] sm:min-h-[420px] bg-[#0E0F12] flex flex-col justify-between overflow-hidden">
                    {/* Mica Backdrop Subtle Pattern */}
                    <div className="absolute inset-0 bg-halftone pointer-events-none opacity-10" />

                    {/* 3. FLOATING TOP ACRYLIC TOOLBAR PILL (Revealed state from real WinUI 3 app) */}
                    <div className="relative z-30 pt-3 px-3 flex justify-center">
                      <div className="rounded-full bg-[#1C1D22]/90 backdrop-blur-md border border-white/20 shadow-2xl px-3 py-1.5 flex flex-wrap items-center justify-center gap-1.5 text-xs select-none max-w-full">
                        {/* Open Button */}
                        <div className="flex items-center gap-1 px-2 py-0.5 text-newsprint/90 font-medium text-[11px] hover:bg-white/10 rounded-full cursor-default">
                          <FolderOpen className="h-3 w-3 text-amber" />
                          <span>Open</span>
                        </div>

                        <div className="w-[1px] h-3.5 bg-white/20" />

                        {/* Fit Mode Toggles */}
                        <div className="flex items-center gap-0.5">
                          <button
                            type="button"
                            onClick={() => setFitMode("height")}
                            className={`px-2 py-0.5 rounded-full text-[10px] font-mono font-bold transition-all ${
                              fitMode === "height"
                                ? "bg-amber text-black shadow-sm font-black"
                                : "text-newsprint/70 hover:text-white hover:bg-white/10"
                            }`}
                            title="Fit Height: Scales page vertically to fit screen height (H)"
                          >
                            Fit H
                          </button>
                          <button
                            type="button"
                            onClick={() => setFitMode("width")}
                            className={`px-2 py-0.5 rounded-full text-[10px] font-mono font-bold transition-all ${
                              fitMode === "width"
                                ? "bg-amber text-black shadow-sm font-black"
                                : "text-newsprint/70 hover:text-white hover:bg-white/10"
                            }`}
                            title="Fit Width: Scales page horizontally to fit screen width (W)"
                          >
                            Fit W
                          </button>
                        </div>

                        <div className="w-[1px] h-3.5 bg-white/20" />

                        {/* Spread Mode Toggle (Single vs 2-Page) */}
                        <button
                          type="button"
                          onClick={() => setSpreadMode(spreadMode === "single" ? "spread" : "single")}
                          className={`px-2.5 py-0.5 rounded-full text-[10px] font-mono font-bold transition-all ${
                            spreadMode === "spread"
                              ? "bg-cyan text-black shadow-sm font-black"
                              : "text-newsprint/70 hover:text-white hover:bg-white/10"
                          }`}
                          title="Toggle Two-Page Spread (D)"
                        >
                          {spreadMode === "spread" ? "2-Page Spread" : "Single Page"}
                        </button>

                        <div className="w-[1px] h-3.5 bg-white/20" />

                        {/* Reading Direction Toggle (Western LTR vs Manga RTL) */}
                        <button
                          type="button"
                          onClick={() => setReadingDirection(readingDirection === "ltr" ? "rtl" : "ltr")}
                          className={`px-2 py-0.5 rounded-full text-[10px] font-mono font-bold transition-all ${
                            readingDirection === "rtl"
                              ? "bg-crimson text-white shadow-sm font-black"
                              : "text-newsprint/70 hover:text-white hover:bg-white/10"
                          }`}
                          title="Toggle Reading Direction: Western vs. Japanese Manga RTL (Ctrl+R)"
                        >
                          {readingDirection === "rtl" ? "Manga RTL" : "Western"}
                        </button>

                        <div className="w-[1px] h-3.5 bg-white/20" />

                        {/* Night Mode Warmth LUT Toggle */}
                        <button
                          type="button"
                          onClick={() => setNightMode(!nightMode)}
                          className={`flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[10px] font-mono font-bold transition-all ${
                            nightMode
                              ? "bg-amber text-black shadow-sm font-black"
                              : "text-newsprint/70 hover:text-white hover:bg-white/10"
                          }`}
                          title="Toggle Hardware Warmth LUT Shader"
                        >
                          <Moon className="h-3 w-3" />
                          <span>{nightMode ? "LUT: ON" : "Night LUT"}</span>
                        </button>

                        <div className="w-[1px] h-3.5 bg-white/20 hidden sm:block" />

                        {/* Zoom Controls */}
                        <div className="hidden sm:flex items-center gap-1 text-[10px] font-mono text-newsprint/80">
                          <button
                            type="button"
                            onClick={() => setZoomLevel((z) => Math.max(75, z - 10))}
                            className="h-5 w-5 flex items-center justify-center hover:bg-white/10 rounded"
                            title="Zoom Out (Ctrl -)"
                          >
                            -
                          </button>
                          <span>{zoomLevel}%</span>
                          <button
                            type="button"
                            onClick={() => setZoomLevel((z) => Math.min(150, z + 10))}
                            className="h-5 w-5 flex items-center justify-center hover:bg-white/10 rounded"
                            title="Zoom In (Ctrl +)"
                          >
                            +
                          </button>
                        </div>
                      </div>
                    </div>

                    {/* 4. ACTUAL COMIC PAGE CONTENT AREA (Visually dynamic, responds to all toggles) */}
                    <div className="flex-1 flex items-center justify-center p-3 sm:p-5 relative z-10">
                      {/* Left Navigation Zone Button (Hover Arrow) */}
                      <button
                        type="button"
                        onClick={handlePrevious}
                        className="absolute left-2 z-20 h-16 w-8 rounded-full bg-black/60 border border-white/20 text-white flex items-center justify-center hover:bg-amber hover:text-black transition-colors"
                        aria-label="Previous Page"
                      >
                        <ChevronLeft className="h-5 w-5" />
                      </button>

                      {/* Right Navigation Zone Button (Hover Arrow) */}
                      <button
                        type="button"
                        onClick={handleNext}
                        className="absolute right-2 z-20 h-16 w-8 rounded-full bg-black/60 border border-white/20 text-white flex items-center justify-center hover:bg-amber hover:text-black transition-colors"
                        aria-label="Next Page"
                      >
                        <ChevronRight className="h-5 w-5" />
                      </button>

                      {/* The Illustrated Comic Pages with Live Warmth LUT Shader Filter */}
                      <div
                        style={{
                          filter: nightMode
                            ? "sepia(0.55) saturate(1.35) hue-rotate(-15deg) brightness(0.92) contrast(1.05)"
                            : "none",
                          transform: `scale(${zoomLevel / 100})`,
                          transition: "filter 0.3s ease, transform 0.2s ease",
                        }}
                        className={`w-full flex items-center justify-center transition-all duration-300 ${
                          fitMode === "width" ? "max-w-full px-2" : "max-w-[580px]"
                        }`}
                      >
                        {/* CASE 1: Page 1 — Cover Page Isolation */}
                        {currentPage === 1 ? (
                          <div className="transition-all duration-300 transform">
                            <ComicCoverPage
                              className={fitMode === "width" ? "w-full max-w-[340px]" : "max-h-[350px]"}
                              isNightMode={nightMode}
                            />
                            {spreadMode === "spread" && (
                              <div className="text-center mt-2">
                                <span className="text-[10px] font-mono font-bold bg-black/80 text-amber px-2 py-0.5 border border-amber/40 rounded-full">
                                  Cover Isolated (Physical Book Mode)
                                </span>
                              </div>
                            )}
                          </div>
                        ) : spreadMode === "single" ? (
                          /* CASE 2: Single Page View */
                          <div className="transition-all duration-300 transform">
                            {currentPage % 2 === 0 ? (
                              <ComicStoryPageA className={fitMode === "width" ? "w-full max-w-[340px]" : "max-h-[350px]"} />
                            ) : (
                              <ComicStoryPageB className={fitMode === "width" ? "w-full max-w-[340px]" : "max-h-[350px]"} />
                            )}
                          </div>
                        ) : (
                          /* CASE 3: Two-Page Spread View with Spine Gutter */
                          <div
                            className={`grid grid-cols-2 gap-2 sm:gap-3 transition-all duration-300 w-full ${
                              fitMode === "width" ? "max-w-[560px]" : "max-w-[500px]"
                            }`}
                          >
                            {/* In Western mode: Left = Page A, Right = Page B. In Manga RTL: Page B on Left, Page A on Right! */}
                            {readingDirection === "ltr" ? (
                              <>
                                <ComicStoryPageA className={fitMode === "width" ? "w-full" : "max-h-[350px]"} />
                                <ComicStoryPageB className={fitMode === "width" ? "w-full" : "max-h-[350px]"} />
                              </>
                            ) : (
                              <>
                                <ComicStoryPageB className={fitMode === "width" ? "w-full" : "max-h-[350px]"} />
                                <ComicStoryPageA className={fitMode === "width" ? "w-full" : "max-h-[350px]"} />
                              </>
                            )}
                          </div>
                        )}
                      </div>
                    </div>

                    {/* 5. FLOATING BOTTOM PAGE SCRUBBER OVERLAY (Faithful recreation from MainPage.xaml lines 634-710) */}
                    <div className="relative z-30 pb-3 px-3 flex justify-center">
                      <div className="rounded-full bg-[#1C1D22]/90 backdrop-blur-md border border-white/20 shadow-2xl px-3.5 py-1.5 flex items-center gap-2 sm:gap-3 text-xs select-none">
                        {/* First Page */}
                        <button
                          type="button"
                          onClick={() => setCurrentPage(1)}
                          className="text-newsprint/70 hover:text-amber transition-colors p-1"
                          title="First Page (Home)"
                        >
                          <ChevronsLeft className="h-3.5 w-3.5" />
                        </button>

                        {/* Previous Page */}
                        <button
                          type="button"
                          onClick={handlePrevious}
                          className="text-newsprint/70 hover:text-amber transition-colors p-1"
                          title="Previous Page (Left Arrow)"
                        >
                          <ChevronLeft className="h-3.5 w-3.5" />
                        </button>

                        {/* Interactive Scrubber Slider */}
                        <input
                          type="range"
                          min="1"
                          max={totalPages}
                          value={currentPage}
                          onChange={(e) => setCurrentPage(parseInt(e.target.value))}
                          className="w-28 sm:w-44 accent-amber h-1.5 bg-black/60 rounded-full cursor-pointer"
                          aria-label="Scrub comic page slider"
                        />

                        {/* Page Counter Label */}
                        <span className="font-mono text-[11px] font-bold text-newsprint min-w-[55px] text-center">
                          {currentPage} / {totalPages}
                        </span>

                        {/* Next Page */}
                        <button
                          type="button"
                          onClick={handleNext}
                          className="text-newsprint/70 hover:text-amber transition-colors p-1"
                          title="Next Page (Right Arrow)"
                        >
                          <ChevronRight className="h-3.5 w-3.5" />
                        </button>

                        {/* Last Page */}
                        <button
                          type="button"
                          onClick={() => setCurrentPage(totalPages)}
                          className="text-newsprint/70 hover:text-amber transition-colors p-1"
                          title="Last Page (End)"
                        >
                          <ChevronsRight className="h-3.5 w-3.5" />
                        </button>

                        {/* Percentage */}
                        <span className="font-mono text-[10px] text-amber font-black hidden sm:inline-block pl-1 border-l border-white/10">
                          {Math.round((currentPage / totalPages) * 100)}%
                        </span>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Tactile hint badge below the mockup */}
                <div className="mt-3.5 text-center">
                  <span className="inline-flex items-center gap-1.5 text-xs font-mono font-bold text-newsprint/80 bg-black/90 px-3.5 py-1 border-2 border-black shadow-[2px_2px_0px_#000]">
                    <Sparkles className="h-3.5 w-3.5 text-amber" />
                    Interactive WinUI 3 Fluent Canvas: Test Night LUT, Spreads, Manga RTL, and the page scrubber live
                  </span>
                </div>
              </div>
            </motion.div>
          </div>
        </div>
      </div>
    </section>
  );
}
