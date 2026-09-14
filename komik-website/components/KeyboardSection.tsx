"use client";

import { useEffect, useRef, useState } from "react";
import { AnimatePresence, motion, useInView } from "framer-motion";
import { Keyboard } from "lucide-react";
import SectionHeading from "@/components/fx/SectionHeading";

type Shortcut = { id: string; keys: string[]; label: string; action: string; sfx: string; color: string; match: (e: KeyboardEvent) => boolean };

const SHORTCUTS: Shortcut[] = [
  { id: "next", keys: ["→"], label: "→ / Space / PgDn", action: "Next page or next spread", sfx: "FLIP!", color: "#FFD700", match: (e) => !e.ctrlKey && ["ArrowRight", "PageDown"].includes(e.key) },
  { id: "prev", keys: ["←"], label: "← / PgUp / Shift+Space", action: "Previous page or spread", sfx: "FLOP!", color: "#FFD700", match: (e) => !e.ctrlKey && !e.altKey && ["ArrowLeft", "PageUp"].includes(e.key) },
  { id: "home", keys: ["Home", "End"], label: "Home / End", action: "Jump to first or last page", sfx: "WARP!", color: "#00C2FF", match: (e) => ["Home", "End"].includes(e.key) },
  { id: "spread", keys: ["D"], label: "D", action: "Toggle single page / two-page spread", sfx: "SPREAD!", color: "#FF1F6D", match: (e) => !e.ctrlKey && e.key.toLowerCase() === "d" },
  { id: "webtoon", keys: ["V"], label: "V", action: "Toggle webtoon continuous scroll", sfx: "SCROLL!", color: "#00C2FF", match: (e) => !e.ctrlKey && e.key.toLowerCase() === "v" },
  { id: "rtl", keys: ["Ctrl", "R"], label: "Ctrl + R", action: "Western ↔ manga right-to-left", sfx: "FLIP-FLOP!", color: "#FF1F6D", match: (e) => e.ctrlKey && e.key.toLowerCase() === "r" },
  { id: "fit", keys: ["W", "H", "A"], label: "W / H / A", action: "Fit width, fit height or actual size", sfx: "FIT!", color: "#FFD700", match: (e) => !e.ctrlKey && ["w", "h", "a"].includes(e.key.toLowerCase()) },
  { id: "zoom", keys: ["Ctrl", "+", "−", "0"], label: "Ctrl + + / − / 0", action: "Zoom in, zoom out, reset zoom", sfx: "ZOOM!", color: "#2FD17A", match: (e) => e.ctrlKey && ["+", "=", "-", "0"].includes(e.key) },
  { id: "bookmark", keys: ["Ctrl", "D"], label: "Ctrl + D / B", action: "Bookmark the current page", sfx: "SAVED!", color: "#F59E0B", match: (e) => e.ctrlKey && ["d", "b"].includes(e.key.toLowerCase()) },
  { id: "ocr", keys: ["Ctrl", "F"], label: "Ctrl + F", action: "Search comic dialogue (offline OCR)", sfx: "FOUND!", color: "#A78BFA", match: (e) => e.ctrlKey && e.key.toLowerCase() === "f" },
  { id: "back", keys: ["Alt", "←"], label: "Alt + ← / Backspace", action: "Back to library (saves progress)", sfx: "HOME!", color: "#00C2FF", match: (e) => (e.altKey && e.key === "ArrowLeft") || e.key === "Backspace" },
  { id: "full", keys: ["F11"], label: "F11 / Esc", action: "Full screen / exit full screen", sfx: "BIG!", color: "#FF1F6D", match: (e) => ["F11", "Escape"].includes(e.key) },
];

export default function KeyboardSection() {
  const sectionRef = useRef<HTMLElement>(null);
  const inView = useInView(sectionRef, { margin: "-30% 0px" });
  const [active, setActive] = useState<Shortcut>(SHORTCUTS[0]);
  const [pulse, setPulse] = useState(0);

  const trigger = (s: Shortcut) => {
    setActive(s);
    setPulse((p) => p + 1);
  };

  useEffect(() => {
    if (!inView) return;
    const onKey = (e: KeyboardEvent) => {
      const t = e.target as HTMLElement;
      if (t.closest("input, textarea, [aria-label^='Interactive Komik reader']")) return;
      const s = SHORTCUTS.find((x) => x.match(e));
      if (!s) return;
      // only swallow browser shortcuts that would otherwise hijack the demo
      if (e.ctrlKey && ["r", "d", "b", "f"].includes(e.key.toLowerCase())) e.preventDefault();
      trigger(s);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [inView]);

  return (
    <section ref={sectionRef} id="shortcuts" className="relative scroll-mt-16 overflow-hidden border-b-[3px] border-black bg-[#0B0B10] py-20 sm:py-28">
      <div className="bg-halftone pointer-events-none absolute inset-0 opacity-25" />
      <div className="relative mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <SectionHeading
          caption="Appendix · Keyboard-first reading"
          title="Read 200 pages."
          accent="Never touch the mouse."
          tone="magenta"
          sub={
            <>
              <Keyboard className="mr-1.5 inline h-5 w-5 text-magenta" />
              Go ahead: press the real keys on your keyboard (or tap a key below) and watch what each shortcut does.
            </>
          }
        />

        <div className="mt-12 grid gap-8 lg:grid-cols-12">
          {/* SFX display */}
          <div className="relative flex min-h-[260px] flex-col justify-between overflow-hidden border-[3px] border-black bg-ink p-6 shadow-[8px_8px_0_#000] lg:col-span-5">
            <div className="bg-speedlines animate-spin-slow absolute left-1/2 top-1/2 h-[200%] w-[200%] -translate-x-1/2 -translate-y-1/2 opacity-60" />
            <div className="relative font-mono text-[11px] font-bold uppercase text-muted">Now showing</div>
            <div className="relative flex flex-1 items-center justify-center py-6">
              <AnimatePresence mode="popLayout">
                <motion.div
                  key={`${active.id}-${pulse}`}
                  initial={{ scale: 0.2, rotate: -25, opacity: 0 }}
                  animate={{ scale: 1, rotate: -6, opacity: 1 }}
                  exit={{ scale: 1.6, opacity: 0 }}
                  transition={{ type: "spring", stiffness: 500, damping: 14 }}
                  className="text-comic-outline text-center font-bangers text-7xl leading-none sm:text-8xl"
                  style={{ color: active.color }}
                >
                  {active.sfx}
                </motion.div>
              </AnimatePresence>
            </div>
            <div className="relative">
              <div className="flex flex-wrap gap-1.5">
                {active.keys.map((k) => (
                  <kbd key={k} className="border-[3px] border-black bg-paper px-2.5 py-1 font-mono text-sm font-black text-black shadow-[0_4px_0_#000]">
                    {k}
                  </kbd>
                ))}
              </div>
              <div className="mt-3 text-lg font-bold text-newsprint">{active.action}</div>
            </div>
          </div>

          {/* keycaps */}
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:col-span-7">
            {SHORTCUTS.map((s) => {
              const on = s.id === active.id;
              return (
                <motion.button
                  key={s.id}
                  type="button"
                  onClick={() => trigger(s)}
                  animate={on ? { y: 4, boxShadow: "0px 2px 0px #000" } : { y: 0, boxShadow: "0px 6px 0px #000" }}
                  whileHover={{ y: -3 }}
                  transition={{ type: "spring", stiffness: 600, damping: 25 }}
                  className="flex flex-col items-start gap-2 border-[3px] border-black p-3 text-left"
                  style={{ background: on ? s.color : "#1A1A21" }}
                >
                  <span className={`border-2 border-black px-2 py-0.5 font-mono text-[11px] font-black ${on ? "bg-black text-white" : "bg-paper text-black"}`}>{s.label}</span>
                  <span className={`text-[12px] font-bold leading-snug ${on ? "text-black" : "text-newsprint/85"}`}>{s.action}</span>
                </motion.button>
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
}
