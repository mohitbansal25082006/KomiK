"use client";

import { Keyboard } from "lucide-react";

export default function KeyboardSection() {
  const shortcuts = [
    { key: "Right / Space / PgDn", action: "Next Page / Next Spread" },
    { key: "Left / PgUp / Shift+Space", action: "Previous Page / Previous Spread" },
    { key: "D", action: "Toggle Single / Two-Page Spread" },
    { key: "Ctrl + R", action: "Toggle Reading Direction (Western vs. Manga RTL)" },
    { key: "W / H / A", action: "Fit Width / Fit Height / Actual Size (1:1)" },
    { key: "Ctrl + + / - / 0", action: "Zoom In / Zoom Out / Reset Zoom" },
    { key: "Ctrl + D / Ctrl + B", action: "Bookmark Current Page with Note" },
    { key: "Alt + Left / Backspace", action: "Return to Library (Flushes Progress)" },
    { key: "F11", action: "Toggle Distraction-Free Full-Screen" },
    { key: "Escape", action: "Exit Full-Screen / Dismiss Dialogs" },
  ];

  return (
    <section id="shortcuts" className="relative border-b-[3px] border-black bg-ink py-16 sm:py-20">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Panel Container */}
        <div className="relative border-[3px] border-black bg-[#0E0E12] p-6 sm:p-10 shadow-[8px_8px_0px_#000000]">
          {/* Overlapping Diegetic Caption Box */}
          <div className="absolute -top-3.5 left-6 sm:left-10 z-20 caption-box px-3.5 py-1 text-xs font-black tracking-wider rotate-[0.5deg]">
            APPENDIX · RAPID KEYBOARD NAVIGATION CHEAT SHEET
          </div>

          <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 mb-8">
            <div>
              <h2 className="font-display text-3xl sm:text-4xl font-black tracking-tight text-newsprint">
                Keyboard-First Navigation
              </h2>
              <p className="mt-2 text-sm sm:text-base text-newsprint/75 font-medium">
                Read whole 200-page volumes without touching your mouse.
              </p>
            </div>

            <div className="flex items-center gap-2 text-xs font-mono text-amber border-2 border-black bg-black px-3 py-1.5 shadow-[2px_2px_0px_#000] font-bold self-start sm:self-auto">
              <Keyboard className="h-4 w-4 text-amber" />
              <span>Zero-Latency Input Hook</span>
            </div>
          </div>

          {/* Keycap Panels Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
            {shortcuts.map((sc) => (
              <div
                key={sc.key}
                className="border-2 border-black bg-[#15151A] p-3.5 flex flex-col justify-between shadow-[3px_3px_0px_#000000] group hover:-translate-x-0.5 hover:-translate-y-0.5 transition-transform"
              >
                <kbd className="font-mono text-xs font-black text-black bg-amber px-2 py-1 border-2 border-black shadow-[1.5px_1.5px_0px_#000] self-start mb-2.5">
                  {sc.key}
                </kbd>
                <span className="text-xs text-newsprint font-bold leading-snug">
                  {sc.action}
                </span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
