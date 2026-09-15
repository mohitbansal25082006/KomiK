import { motion } from "framer-motion";
import type { ReactNode } from "react";

/** The Komik crest (same artwork as the app icon and website logo). */
export function Crest({ size = 32, className = "" }: { size?: number; className?: string }) {
  return (
    <svg viewBox="0 0 100 100" width={size} height={size} className={`shrink-0 select-none ${className}`} aria-hidden="true">
      <g transform="translate(3.5,4)">
        <polygon points="13,74 50,83 87,74 87,78 50,87 13,78" fill="#000" opacity="0.85" />
        <polygon points="14,20 48,28 48,78 14,70" fill="#000" opacity="0.85" />
        <polygon points="52,28 86,20 86,70 52,78" fill="#000" opacity="0.85" />
      </g>
      <polygon points="13,74 50,83 87,74 87,78 50,87 13,78" fill="#16171D" stroke="#000" strokeWidth="2.5" strokeLinejoin="round" />
      <polygon points="14,20 48,28 48,78 14,70" fill="#0E0F14" stroke="#000" strokeWidth="3.5" strokeLinejoin="round" />
      <polygon points="18,24 44,30 44,74 18,67" fill="#FFD700" stroke="#000" strokeWidth="2.2" strokeLinejoin="round" />
      <polygon points="23,34 29,35 29,63 23,62" fill="#000" />
      <polygon points="29,48 38,37 42,38 32,50" fill="#000" />
      <polygon points="31,47 42,62 38,63 29,51" fill="#000" />
      <polygon points="52,28 86,20 86,70 52,78" fill="#FFFDF6" stroke="#000" strokeWidth="3.5" strokeLinejoin="round" />
      <polygon points="56,32 82,26 82,46 56,52" fill="#00C2FF" stroke="#000" strokeWidth="2" />
      <polygon points="56,56 68,53 68,72 56,75" fill="#FF1F6D" stroke="#000" strokeWidth="2" />
      <polygon points="71,52 82,49 82,68 71,71" fill="#FFD700" stroke="#000" strokeWidth="2" />
      <polygon points="47,23 53,24 53,89 50,85 47,89" fill="#FF1F6D" stroke="#000" strokeWidth="2" />
    </svg>
  );
}

export function Wordmark({ size = "text-[26px]", sub = "Downloader" }: { size?: string; sub?: string }) {
  return (
    <div className="flex items-center gap-2 leading-none">
      <Crest size={34} className="drop-shadow-[2px_2px_0_rgba(0,0,0,0.25)]" />
      <div className="flex flex-col">
        <span className={`letter-outline-yellow ${size} leading-[0.9]`}>KOMIK</span>
        <span className="font-comic text-[10px] font-bold uppercase tracking-[0.22em] text-muted">{sub}</span>
      </div>
    </div>
  );
}

/** A spiky comic starburst. */
export function Burst({ className = "", fill = "#FFD700", stroke = "#08080A", points = 14, children, spin = false }: { className?: string; fill?: string; stroke?: string; points?: number; children?: ReactNode; spin?: boolean }) {
  const path = Array.from({ length: points * 2 }, (_, i) => {
    const r = i % 2 === 0 ? 48 : 34 + ((i * 7) % 5);
    const a = (Math.PI * i) / points - Math.PI / 2;
    return `${50 + r * Math.cos(a)},${50 + r * Math.sin(a)}`;
  }).join(" ");
  return (
    <div className={`relative grid place-items-center ${className}`}>
      <svg viewBox="0 0 100 100" className={`absolute inset-0 h-full w-full ${spin ? "animate-spin-slow" : ""}`} aria-hidden="true">
        <polygon points={path} fill={fill} stroke={stroke} strokeWidth="3" strokeLinejoin="round" />
      </svg>
      <div className="relative">{children}</div>
    </div>
  );
}

/** The "QUEUED!" stamp that slams onto the screen after a download starts. */
export function Stamp({ text, color = "#FF1F6D", onDone }: { text: string; color?: string; onDone?: () => void }) {
  return (
    <motion.div
      className="pointer-events-none fixed inset-0 z-50 grid place-items-center"
      initial={{ opacity: 1 }}
      animate={{ opacity: [1, 1, 0] }}
      transition={{ duration: 1.5, times: [0, 0.75, 1] }}
      onAnimationComplete={onDone}
    >
      <motion.div
        initial={{ scale: 3.2, rotate: -24, opacity: 0 }}
        animate={{ scale: 1, rotate: -9, opacity: 1 }}
        transition={{ type: "spring", stiffness: 520, damping: 17 }}
        className="letter rounded-md border-[5px] px-6 pb-1 pt-2 text-[54px] leading-none"
        style={{ color, borderColor: color, background: "rgba(255,255,255,0.93)", boxShadow: `6px 6px 0 #08080A`, textShadow: "2px 2px 0 rgba(0,0,0,0.12)" }}
      >
        {text}
      </motion.div>
    </motion.div>
  );
}
