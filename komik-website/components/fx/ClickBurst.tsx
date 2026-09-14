"use client";

import { useEffect, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { prefersReducedMotion } from "@/lib/gsap";

const WORDS = ["POW!", "BAM!", "ZAP!", "KLIK!", "WHAM!", "BOOM!", "ZOK!", "KAPOW!"];
const COLORS = ["#FFD700", "#00C2FF", "#FF1F6D", "#FFFFFF"];

type Burst = { id: number; x: number; y: number; word: string; color: string; rot: number };

/** Sprinkles a tiny comic sound effect wherever you click. */
export default function ClickBurst() {
  const [bursts, setBursts] = useState<Burst[]>([]);

  useEffect(() => {
    if (prefersReducedMotion()) return;
    let n = 0;
    const onDown = (e: PointerEvent) => {
      const t = e.target as HTMLElement;
      if (t.closest("[data-no-burst], input, textarea, select, label")) return;
      const b: Burst = {
        id: ++n,
        x: e.clientX,
        y: e.clientY,
        word: WORDS[Math.floor(Math.random() * WORDS.length)],
        color: COLORS[Math.floor(Math.random() * COLORS.length)],
        rot: Math.random() * 40 - 20,
      };
      setBursts((all) => [...all.slice(-5), b]);
      setTimeout(() => setBursts((all) => all.filter((x) => x.id !== b.id)), 650);
    };
    window.addEventListener("pointerdown", onDown);
    return () => window.removeEventListener("pointerdown", onDown);
  }, []);

  return (
    <div aria-hidden className="pointer-events-none fixed inset-0 z-[400] overflow-hidden">
      <AnimatePresence>
        {bursts.map((b) => (
          <motion.span
            key={b.id}
            initial={{ scale: 0, rotate: b.rot - 30, opacity: 1 }}
            animate={{ scale: 1, rotate: b.rot, opacity: 1, y: -18 }}
            exit={{ scale: 1.4, opacity: 0 }}
            transition={{ type: "spring", stiffness: 600, damping: 18 }}
            className="text-comic-outline-thin absolute select-none text-3xl"
            style={{ left: b.x - 40, top: b.y - 40, color: b.color }}
          >
            {b.word}
          </motion.span>
        ))}
      </AnimatePresence>
    </div>
  );
}
