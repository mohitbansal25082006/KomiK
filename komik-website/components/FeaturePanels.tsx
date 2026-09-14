"use client";

import { Database, Bookmark, BookOpen, Search, Moon, Sliders, Zap, CheckCircle, Tag, Eye } from "lucide-react";

export default function FeaturePanels() {
  return (
    <section id="reading-engine" className="relative border-b-[3px] border-black bg-ink py-16 sm:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Asymmetric Comic Spread Frame */}
        <div className="relative border-[3px] border-black bg-[#0C0C0F] p-6 sm:p-10 lg:p-12 shadow-[8px_8px_0px_#000000]">
          {/* Overlapping Diegetic Caption Box */}
          <div className="absolute -top-3.5 left-6 sm:left-10 z-20 caption-box px-3.5 py-1 text-xs font-black tracking-wider rotate-[-0.6deg]">
            PAGE 03 · THE NATIVE READING ENGINE · FLUID & LOCAL-FIRST
          </div>

          <div className="mb-10 max-w-3xl">
            <h2 className="font-display text-3xl sm:text-4xl lg:text-5xl font-black tracking-tight text-newsprint leading-[1.08]">
              Engineered for the page. <br />
              <span className="text-amber underline decoration-amber/40 underline-offset-8">
                Not for the algorithm.
              </span>
            </h2>
            <p className="mt-3 text-base sm:text-lg text-newsprint/80 font-medium">
              Every interaction is tuned to simulate physical graphic novel reading with instant native responsiveness.
            </p>
          </div>

          {/* ASYMMETRIC COMIC PAGE LAYOUT WITH SHARED GUTTERS */}
          <div className="space-y-6">
            {/* DOMINANT HERO FEATURE PANEL: The Reading Experience & Dual Spread Engine (Spans Full Width) */}
            <div className="border-[3px] border-black bg-[#141419] p-6 sm:p-8 lg:p-10 shadow-[6px_6px_0px_#000000] relative overflow-hidden">
              <div className="flex items-center justify-between mb-4">
                <span className="inline-flex items-center gap-2 px-3 py-1 border-2 border-black bg-amber text-black text-xs font-mono font-black uppercase shadow-[2px_2px_0px_#000]">
                  <BookOpen className="h-3.5 w-3.5 stroke-[2.5]" />
                  DOMINANT PANEL · CORE READING EXPERIENCE
                </span>
                <span className="text-xs font-mono font-bold text-muted">
                  SPREAD 3.1
                </span>
              </div>

              <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-center">
                <div className="lg:col-span-7">
                  <h3 className="font-display text-2xl sm:text-3xl lg:text-4xl font-black text-newsprint tracking-tight">
                    Authentic Two-Page Spreads & Manga RTL Navigation
                  </h3>

                  <p className="mt-3 text-sm sm:text-base text-newsprint/80 leading-relaxed font-medium">
                    Physical comic books are designed around double-page splashes. Komik intelligently detects page 1 as an isolated cover, correctly pairing subsequent interior pages (2-3, 4-5) without awkward offset errors.
                  </p>

                  <p className="mt-3 text-sm sm:text-base text-newsprint/80 leading-relaxed font-medium">
                    Reading Japanese manga? Press <code className="bg-black border border-black text-amber px-1.5 py-0.5 font-mono text-xs font-bold shadow-[1px_1px_0px_#000]">Ctrl + R</code> to instantly flip reading order, page pairing, and arrow keys to Right-to-Left mode.
                  </p>

                  {/* Feature Highlights Grid */}
                  <div className="mt-6 grid grid-cols-1 sm:grid-cols-2 gap-3 font-mono text-xs text-newsprint">
                    <div className="border-2 border-black bg-[#1A1A22] p-3 shadow-[2px_2px_0px_#000]">
                      <span className="block text-amber font-bold mb-1">✓ Cover Page Offset</span>
                      <span className="text-muted text-[11px]">Cover stays single; interior pages pair seamlessly.</span>
                    </div>
                    <div className="border-2 border-black bg-[#1A1A22] p-3 shadow-[2px_2px_0px_#000]">
                      <span className="block text-cyan font-bold mb-1">✓ Manga RTL Mode</span>
                      <span className="text-muted text-[11px]">Flips navigation logic for Japanese tankōbon.</span>
                    </div>
                    <div className="border-2 border-black bg-[#1A1A22] p-3 shadow-[2px_2px_0px_#000]">
                      <span className="block text-amber font-bold mb-1">✓ Fit Modes [W / H / A]</span>
                      <span className="text-muted text-[11px]">Fit to width, fit to height, or 1:1 actual pixel size.</span>
                    </div>
                    <div className="border-2 border-black bg-[#1A1A22] p-3 shadow-[2px_2px_0px_#000]">
                      <span className="block text-cyan font-bold mb-1">✓ Hardware Color LUT</span>
                      <span className="text-muted text-[11px]">Real-time 256-entry GPU channel warmth & contrast.</span>
                    </div>
                  </div>
                </div>

                {/* Visual Comic Comparison Block */}
                <div className="lg:col-span-5 border-2 border-black bg-black p-4 shadow-[4px_4px_0px_#000]">
                  <div className="text-[11px] font-mono font-black text-amber uppercase tracking-wider mb-3">
                    REAL-TIME PAGE SPREAD ARCHITECTURE
                  </div>

                  <div className="space-y-3">
                    {/* Western LTR Spread */}
                    <div className="border-2 border-black bg-paper text-black p-3 shadow-[2px_2px_0px_#000]">
                      <div className="flex justify-between items-center text-[10px] font-mono font-black mb-1.5">
                        <span className="bg-black text-white px-1 py-0.5">WESTERN MODE</span>
                        <span className="text-black/60">Pages 2 & 3</span>
                      </div>
                      <div className="grid grid-cols-2 gap-2 text-center text-[10px] font-mono font-bold">
                        <div className="border border-black bg-white p-2">LEFT: Page 2</div>
                        <div className="border border-black bg-white p-2">RIGHT: Page 3</div>
                      </div>
                    </div>

                    {/* Manga RTL Spread */}
                    <div className="border-2 border-black bg-paper text-black p-3 shadow-[2px_2px_0px_#000]">
                      <div className="flex justify-between items-center text-[10px] font-mono font-black mb-1.5">
                        <span className="bg-crimson text-white px-1 py-0.5">MANGA RTL (Ctrl+R)</span>
                        <span className="text-black/60">Pages 2 & 3 (Flipped)</span>
                      </div>
                      <div className="grid grid-cols-2 gap-2 text-center text-[10px] font-mono font-bold">
                        <div className="border border-black bg-amber p-2">RIGHT READ FIRST: Page 2</div>
                        <div className="border border-black bg-white p-2">LEFT: Page 3</div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* ASYMMETRIC TIER 2: Two Irregularly Sized Panels (7 cols vs 5 cols) */}
            <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
              {/* Panel 3.2: SQLite Watched Folders & Instant Search (7 cols) */}
              <div className="lg:col-span-7 border-[3px] border-black bg-[#121216] p-6 sm:p-8 shadow-[6px_6px_0px_#000000] flex flex-col justify-between">
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <span className="inline-flex items-center gap-1.5 text-xs font-mono font-black text-cyan uppercase tracking-wider">
                      <Database className="h-3.5 w-3.5" />
                      Local SQLite Database
                    </span>
                    <span className="text-xs font-mono font-bold text-muted">
                      PANEL 3.2
                    </span>
                  </div>

                  <h3 className="font-display text-2xl sm:text-3xl font-black text-newsprint mb-3">
                    Watched Folders & Instant Library Search
                  </h3>

                  <p className="text-sm sm:text-base text-newsprint/75 leading-relaxed mb-6 font-medium">
                    Point Komik at your comic folders. It scans subdirectories recursively, indexes metadata into an embedded SQLite database, extracts cover art asynchronously, and caches thumbnails locally without modifying original archive files.
                  </p>
                </div>

                {/* Mini Search & Filter Visual Simulation */}
                <div className="border-2 border-black bg-black p-3.5 shadow-[3px_3px_0px_#000]">
                  <div className="flex items-center gap-2 border-b-2 border-black/80 pb-2 mb-2.5 text-xs font-mono text-muted">
                    <Search className="h-3.5 w-3.5 text-amber" />
                    <span className="text-newsprint font-bold">Query: &quot;Batman&quot;</span>
                    <span className="ml-auto text-[10px] text-amber font-bold">48 matches · 0.004s</span>
                  </div>
                  <div className="grid grid-cols-3 gap-2 text-center text-[10px] font-mono">
                    <div className="border border-black bg-[#1A1A20] p-2 text-newsprint font-bold truncate">
                      Year One #01
                      <span className="block text-[8px] text-amber mt-0.5">PG 24 · 100%</span>
                    </div>
                    <div className="border border-black bg-[#1A1A20] p-2 text-newsprint font-bold truncate">
                      The Long Halloween
                      <span className="block text-[8px] text-cyan mt-0.5">IN PROGRESS</span>
                    </div>
                    <div className="border border-black bg-[#1A1A20] p-2 text-newsprint font-bold truncate">
                      Hush Vol. 1
                      <span className="block text-[8px] text-muted mt-0.5">UNREAD</span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Panel 3.3: Automatic Progress & Bookmarks (5 cols) */}
              <div className="lg:col-span-5 border-[3px] border-black bg-[#121216] p-6 sm:p-8 shadow-[6px_6px_0px_#000000] flex flex-col justify-between">
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <span className="inline-flex items-center gap-1.5 text-xs font-mono font-black text-amber uppercase tracking-wider">
                      <Bookmark className="h-3.5 w-3.5" />
                      Session State
                    </span>
                    <span className="text-xs font-mono font-bold text-muted">
                      PANEL 3.3
                    </span>
                  </div>

                  <h3 className="font-display text-2xl sm:text-3xl font-black text-newsprint mb-3">
                    Pick Up Exactly Where You Left Off
                  </h3>

                  <p className="text-sm text-newsprint/75 leading-relaxed mb-6 font-medium">
                    Never lose your place. Every page turn automatically commits to local SQLite. Reopening a comic shows an unobtrusive resume toast. Bookmark favorite panels with custom notes via <code className="bg-black text-amber px-1 py-0.5 font-mono text-xs font-bold border border-black">Ctrl + D</code>.
                  </p>
                </div>

                {/* Bookmark Toast Simulation */}
                <div className="border-2 border-black bg-paper text-black p-3.5 shadow-[3px_3px_0px_#000] flex items-center gap-3">
                  <div className="h-9 w-9 bg-amber border-2 border-black flex items-center justify-center shrink-0 shadow-[1px_1px_0px_#000]">
                    <Bookmark className="h-4 w-4 text-black stroke-[2.5]" />
                  </div>
                  <div className="text-xs font-mono">
                    <div className="font-black text-black uppercase">Resumed at Page 34</div>
                    <div className="text-black/70 text-[10px]">Session restored in 42ms · 0 network calls</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
