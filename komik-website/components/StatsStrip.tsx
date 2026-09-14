"use client";

import { useEffect, useRef, useState } from "react";
import { animate, motion, useInView } from "framer-motion";

const STATS = [
  { value: 8, suffix: "", label: "Formats, zero external codecs", tone: "bg-amber", rot: -2 },
  { value: 6, suffix: "", label: "Parallel webtoon page decoders", tone: "bg-cyan", rot: 1.5 },
  { value: 6, suffix: "", label: "Hardware LUT color presets", tone: "bg-magenta text-white", rot: -1 },
  { value: 0, suffix: "", label: "Network calls. Ever.", tone: "bg-paper", rot: 2 },
  { value: 0, suffix: "", label: "Accounts to create", tone: "bg-amber", rot: -1.5 },
  { value: 27, suffix: "", label: "Automated tests passing", tone: "bg-cyan", rot: 1 },
];

function Counter({ to }: { to: number }) {
  const ref = useRef<HTMLSpanElement>(null);
  const inView = useInView(ref, { once: true, margin: "-10% 0px" });
  const [val, setVal] = useState(to === 0 ? 99 : 0);
  useEffect(() => {
    if (!inView) return;
    // zero counts *down* from 99 for comedic effect
    const controls = animate(to === 0 ? 99 : 0, to, {
      duration: to === 0 ? 1.4 : 1.1,
      ease: [0.2, 0.8, 0.2, 1],
      onUpdate: (v) => setVal(Math.round(v)),
    });
    return () => controls.stop();
  }, [inView, to]);
  return <span ref={ref}>{val}</span>;
}

export default function StatsStrip() {
  return (
    <section aria-label="Komik by the numbers" className="relative overflow-hidden border-b-[3px] border-black bg-ink py-14 sm:py-20">
      <div className="bg-halftone-yellow pointer-events-none absolute inset-0 opacity-40" />
      <div className="relative mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="mb-8 flex flex-wrap items-end justify-between gap-3">
          <h2 className="text-comic-outline font-bangers text-5xl leading-none text-amber sm:text-6xl">By the numbers!</h2>
          <p className="max-w-sm font-mono text-xs font-bold text-newsprint/70">Every figure here comes straight from the Komik 1.1.0 codebase and its test suite.</p>
        </div>
        <div className="grid grid-cols-2 gap-4 sm:gap-5 md:grid-cols-3 lg:grid-cols-6">
          {STATS.map((s, i) => (
            <motion.div
              key={s.label}
              initial={{ opacity: 0, y: 40, rotate: s.rot * 4 }}
              whileInView={{ opacity: 1, y: 0, rotate: s.rot }}
              whileHover={{ rotate: 0, y: -6, scale: 1.04 }}
              viewport={{ once: true, margin: "-10% 0px" }}
              transition={{ type: "spring", stiffness: 300, damping: 18, delay: i * 0.06 }}
              className={`${s.tone} relative border-[3px] border-black p-4 text-black shadow-[6px_6px_0_#000]`}
            >
              <div className="font-bangers text-6xl leading-none sm:text-7xl">
                <Counter to={s.value} />
                {s.suffix}
              </div>
              <div className="mt-2 text-xs font-extrabold uppercase leading-snug tracking-wide">{s.label}</div>
              <div className="bg-halftone-paper pointer-events-none absolute inset-0 opacity-40" />
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
}
