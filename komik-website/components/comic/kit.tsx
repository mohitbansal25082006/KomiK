"use client";

import React, { createContext, useContext, useId } from "react";

/* ------------------------------------------------------------------ */
/*  Komik demo comic — SVG lettering & panel kit                       */
/*  Every page is a 600 × 900 native-size SVG so it stays razor sharp  */
/*  at any zoom level inside the reader mockup.                        */
/* ------------------------------------------------------------------ */

export const PAGE_W = 600;
export const PAGE_H = 900;

export const INK = "#0A0A0F";
export const PAPER = "#F5EFE3";
export const YELLOW = "#FFD700";
export const CYAN = "#00C2FF";
export const MAGENTA = "#FF1F6D";
export const NIGHT = "#0D0B22";

export const FONT_LETTER = "var(--font-comic), 'Comic Sans MS', 'Chalkboard SE', cursive";
export const FONT_SFX = "var(--font-bangers), Impact, 'Arial Black', sans-serif";

type PageCtx = { url: (name: string) => string };
const Ctx = createContext<PageCtx>({ url: (n) => `url(#${n})` });
export const usePage = () => useContext(Ctx);

/** Deterministic PRNG so illustrations are identical on server & client. */
export function rng(seed: number) {
  let a = seed >>> 0;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/* ----------------------------- Page ------------------------------- */

export function ComicPageFrame({
  number,
  children,
  bleed = false,
  bg = PAPER,
  folio = true,
  className,
}: {
  number: number;
  children: React.ReactNode;
  bleed?: boolean;
  bg?: string;
  folio?: boolean;
  className?: string;
}) {
  const raw = useId();
  const u = "k" + raw.replace(/[^a-zA-Z0-9]/g, "");
  const url = (name: string) => `url(#${u}-${name})`;
  const id = (name: string) => `${u}-${name}`;

  return (
    <Ctx.Provider value={{ url }}>
      <svg
        viewBox={`0 0 ${PAGE_W} ${PAGE_H}`}
        width="100%"
        height="100%"
        className={className}
        xmlns="http://www.w3.org/2000/svg"
        role="img"
        aria-label={`Cyberpunk Chronicles #01, page ${number}`}
        style={{ display: "block", userSelect: "none" }}
      >
        <defs>
          <pattern id={id("dots")} width="7" height="7" patternUnits="userSpaceOnUse">
            <circle cx="3.5" cy="3.5" r="1.4" fill="#000" opacity="0.22" />
          </pattern>
          <pattern id={id("dotsW")} width="7" height="7" patternUnits="userSpaceOnUse">
            <circle cx="3.5" cy="3.5" r="1.3" fill="#fff" opacity="0.18" />
          </pattern>
          <pattern id={id("dotsBig")} width="12" height="12" patternUnits="userSpaceOnUse">
            <circle cx="6" cy="6" r="2.6" fill="#000" opacity="0.18" />
          </pattern>
          <pattern id={id("dotsM")} width="9" height="9" patternUnits="userSpaceOnUse">
            <circle cx="4.5" cy="4.5" r="2" fill={MAGENTA} opacity="0.35" />
          </pattern>
          <pattern id={id("dotsC")} width="9" height="9" patternUnits="userSpaceOnUse">
            <circle cx="4.5" cy="4.5" r="2" fill={CYAN} opacity="0.3" />
          </pattern>
          <pattern id={id("bricks")} width="40" height="20" patternUnits="userSpaceOnUse">
            <rect width="40" height="20" fill="#3A1F2B" />
            <path d="M0 0 H40 M0 10 H40 M10 0 V10 M30 10 V20" stroke="#1A0D14" strokeWidth="2" />
          </pattern>
          <pattern id={id("paper")} width="5" height="5" patternUnits="userSpaceOnUse">
            <circle cx="1" cy="1" r="0.6" fill="#8B7355" opacity="0.08" />
          </pattern>
          <linearGradient id={id("skyNight")} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#07061A" />
            <stop offset="0.6" stopColor="#1E1348" />
            <stop offset="1" stopColor="#4A1B5E" />
          </linearGradient>
          <linearGradient id={id("skyNeon")} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#0A0620" />
            <stop offset="0.55" stopColor="#3B0F4F" />
            <stop offset="1" stopColor="#B3124F" />
          </linearGradient>
          <linearGradient id={id("skyDawn")} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#2B1B5A" />
            <stop offset="0.45" stopColor="#E84A6F" />
            <stop offset="0.8" stopColor="#FFB547" />
            <stop offset="1" stopColor="#FFE38A" />
          </linearGradient>
          <linearGradient id={id("steel")} x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#F2F5FA" />
            <stop offset="1" stopColor="#9AA3B5" />
          </linearGradient>
          <radialGradient id={id("glowC")}>
            <stop offset="0" stopColor={CYAN} stopOpacity="0.9" />
            <stop offset="1" stopColor={CYAN} stopOpacity="0" />
          </radialGradient>
          <radialGradient id={id("glowY")}>
            <stop offset="0" stopColor="#FFF4B0" stopOpacity="1" />
            <stop offset="0.4" stopColor={YELLOW} stopOpacity="0.85" />
            <stop offset="1" stopColor="#FF8A00" stopOpacity="0" />
          </radialGradient>
          <radialGradient id={id("glowR")}>
            <stop offset="0" stopColor="#FF3B3B" stopOpacity="0.9" />
            <stop offset="1" stopColor="#FF3B3B" stopOpacity="0" />
          </radialGradient>
          <radialGradient id={id("glowM")}>
            <stop offset="0" stopColor={MAGENTA} stopOpacity="0.8" />
            <stop offset="1" stopColor={MAGENTA} stopOpacity="0" />
          </radialGradient>
        </defs>

        <rect width={PAGE_W} height={PAGE_H} fill={bg} />
        {!bleed && <rect width={PAGE_W} height={PAGE_H} fill={url("paper")} />}
        {children}

        {folio && !bleed && (
          <g style={{ fontFamily: FONT_LETTER }} fontWeight={700}>
            <text x={24} y={880} fontSize={13} fill={INK} opacity={0.75} letterSpacing={1}>
              CYBERPUNK CHRONICLES #01
            </text>
            <rect x={532} y={864} width={44} height={22} fill={INK} />
            <text x={554} y={880} fontSize={14} fill={PAPER} textAnchor="middle">
              {number}
            </text>
          </g>
        )}
      </svg>
    </Ctx.Provider>
  );
}

/* ----------------------------- Panel ------------------------------ */

export function Panel({
  x,
  y,
  w,
  h,
  bg = NIGHT,
  children,
  border = 5,
}: {
  x: number;
  y: number;
  w: number;
  h: number;
  bg?: string;
  children?: React.ReactNode;
  border?: number;
}) {
  return (
    <g>
      <svg x={x} y={y} width={w} height={h} viewBox={`0 0 ${w} ${h}`} overflow="hidden">
        <rect width={w} height={h} fill={bg} />
        {children}
      </svg>
      <rect x={x} y={y} width={w} height={h} fill="none" stroke={INK} strokeWidth={border} />
    </g>
  );
}

/* ---------------------------- Lettering --------------------------- */

function textWidth(line: string, size: number) {
  let w = 0;
  for (const ch of line) {
    if (ch === " ") w += size * 0.32;
    else if ("IJ1.,'!:;|".includes(ch)) w += size * 0.34;
    else if ("MW".includes(ch)) w += size * 0.82;
    else w += size * 0.62;
  }
  return w;
}

function burstPath(cx: number, cy: number, rx: number, ry: number, spikes: number, depth: number, seed = 3) {
  const r = rng(seed);
  const pts: string[] = [];
  const n = spikes * 2;
  for (let i = 0; i < n; i++) {
    const a = (i / n) * Math.PI * 2;
    const out = i % 2 === 0;
    const k = out ? 1 + (depth / Math.max(rx, ry)) * (0.7 + r() * 0.6) : 0.9;
    pts.push(`${(cx + Math.cos(a) * rx * k).toFixed(1)},${(cy + Math.sin(a) * ry * k).toFixed(1)}`);
  }
  return `M${pts.join(" L")} Z`;
}

type BalloonKind = "speech" | "shout" | "robot" | "whisper" | "thought";

export function Balloon({
  x,
  y,
  text,
  tail,
  kind = "speech",
  size = 19,
  w,
  seed = 7,
}: {
  x: number;
  y: number;
  text: string;
  tail?: [number, number];
  kind?: BalloonKind;
  size?: number;
  w?: number;
  seed?: number;
}) {
  const lines = text.split("\n");
  const lh = size * 1.14;
  const bw = w ?? Math.max(...lines.map((l) => textWidth(l, size))) + size * 1.7;
  const bh = lines.length * lh + size * 1.05;
  const left = x - bw / 2;
  const top = y - bh / 2;
  const fill = kind === "robot" ? "#DFF7FF" : "#FFFFFF";
  const stroke = kind === "robot" ? "#006C8F" : INK;

  const bodyShape =
    kind === "shout" ? (
      <path d={burstPath(x, y, bw / 2 + 10, bh / 2 + 10, 14, 16, seed)} />
    ) : kind === "thought" ? (
      <ellipse cx={x} cy={y} rx={bw / 2 + 10} ry={bh / 2 + 8} />
    ) : (
      <rect
        x={left}
        y={top}
        width={bw}
        height={bh}
        rx={kind === "robot" ? 6 : Math.min(bh / 2, bw / 2, 30)}
      />
    );

  let tailShape: React.ReactNode = null;
  if (tail && kind !== "thought") {
    const [tx, ty] = tail;
    const off = Math.max(-bw / 3, Math.min(bw / 3, (tx - x) * 0.35));
    const dist = Math.hypot(tx - x, ty - y);
    const base = Math.min(bw / 3, Math.max(20, Math.min(44, dist * 0.2)));
    const bx = x + off;
    const by = y + (ty > y ? bh * 0.15 : -bh * 0.15);
    tailShape = <path d={`M${bx - base / 2},${by} L${tx},${ty} L${bx + base / 2},${by} Z`} />;
  }

  const firstBase = top + (bh - lines.length * lh) / 2 + size * 0.86;

  return (
    <g>
      {kind === "thought" && tail && (
        <g fill="#fff" stroke={INK} strokeWidth={3}>
          <circle cx={(x + tail[0]) / 2} cy={(y + bh / 2 + tail[1]) / 2} r={7} />
          <circle cx={tail[0]} cy={tail[1]} r={4} />
        </g>
      )}
      {tailShape && (
        <g fill={fill} stroke={stroke} strokeWidth={7} strokeLinejoin="round">
          {tailShape}
        </g>
      )}
      <g
        fill={fill}
        stroke={stroke}
        strokeWidth={kind === "shout" ? 4 : 3.5}
        strokeDasharray={kind === "whisper" ? "8 6" : undefined}
        strokeLinejoin="round"
      >
        {bodyShape}
      </g>
      {tailShape && <g fill={fill}>{tailShape}</g>}
      <text
        x={x}
        y={firstBase}
        textAnchor="middle"
        fontSize={size}
        fontWeight={700}
        fill={kind === "robot" ? "#003A4D" : INK}
        style={{ fontFamily: FONT_LETTER }}
      >
        {lines.map((l, i) => (
          <tspan key={i} x={x} dy={i === 0 ? 0 : lh}>
            {l}
          </tspan>
        ))}
      </text>
    </g>
  );
}

export function Caption({
  x,
  y,
  text,
  size = 16,
  bg = YELLOW,
  color = INK,
  w,
  rotate = 0,
}: {
  x: number;
  y: number;
  text: string;
  size?: number;
  bg?: string;
  color?: string;
  w?: number;
  rotate?: number;
}) {
  const lines = text.split("\n");
  const lh = size * 1.18;
  const bw = w ?? Math.max(...lines.map((l) => textWidth(l, size))) + size * 1.2;
  const bh = lines.length * lh + size * 0.8;
  return (
    <g transform={rotate ? `rotate(${rotate} ${x} ${y})` : undefined}>
      <rect x={x + 4} y={y + 4} width={bw} height={bh} fill={INK} />
      <rect x={x} y={y} width={bw} height={bh} fill={bg} stroke={INK} strokeWidth={3} />
      <text
        x={x + size * 0.6}
        y={y + size * 0.4 + size * 0.9}
        fontSize={size}
        fontWeight={700}
        fill={color}
        style={{ fontFamily: FONT_LETTER }}
      >
        {lines.map((l, i) => (
          <tspan key={i} x={x + size * 0.6} dy={i === 0 ? 0 : lh}>
            {l}
          </tspan>
        ))}
      </text>
    </g>
  );
}

export function Sfx({
  x,
  y,
  text,
  size = 64,
  fill = YELLOW,
  rotate = -8,
  stroke = INK,
  shadow = MAGENTA,
}: {
  x: number;
  y: number;
  text: string;
  size?: number;
  fill?: string;
  rotate?: number;
  stroke?: string;
  shadow?: string;
}) {
  const common = {
    x,
    y,
    fontSize: size,
    textAnchor: "middle" as const,
    style: { fontFamily: FONT_SFX, letterSpacing: size * 0.02 },
  };
  return (
    <g transform={`rotate(${rotate} ${x} ${y})`}>
      <text {...common} x={x + size * 0.07} y={y + size * 0.07} fill={shadow} stroke={stroke} strokeWidth={size * 0.12} paintOrder="stroke" strokeLinejoin="round">
        {text}
      </text>
      <text {...common} fill={fill} stroke={stroke} strokeWidth={size * 0.12} paintOrder="stroke" strokeLinejoin="round">
        {text}
      </text>
    </g>
  );
}

/* ---------------------------- FX shapes --------------------------- */

export function Burst({
  cx,
  cy,
  r,
  spikes = 16,
  fill = YELLOW,
  stroke = INK,
  depth,
  seed = 11,
  strokeWidth = 5,
}: {
  cx: number;
  cy: number;
  r: number;
  spikes?: number;
  fill?: string;
  stroke?: string;
  depth?: number;
  seed?: number;
  strokeWidth?: number;
}) {
  return (
    <path
      d={burstPath(cx, cy, r, r, spikes, depth ?? r * 0.45, seed)}
      fill={fill}
      stroke={stroke}
      strokeWidth={strokeWidth}
      strokeLinejoin="round"
    />
  );
}

export function SpeedLines({
  cx,
  cy,
  w,
  h,
  count = 48,
  color = "#fff",
  inner = 90,
  opacity = 0.55,
  seed = 5,
}: {
  cx: number;
  cy: number;
  w: number;
  h: number;
  count?: number;
  color?: string;
  inner?: number;
  opacity?: number;
  seed?: number;
}) {
  const r = rng(seed);
  const R = Math.hypot(w, h);
  const paths: string[] = [];
  for (let i = 0; i < count; i++) {
    const a = (i / count) * Math.PI * 2 + r() * 0.05;
    const spread = 0.012 + r() * 0.02;
    const ri = inner * (0.8 + r() * 0.6);
    const x1 = cx + Math.cos(a) * ri;
    const y1 = cy + Math.sin(a) * ri;
    const x2 = cx + Math.cos(a - spread) * R;
    const y2 = cy + Math.sin(a - spread) * R;
    const x3 = cx + Math.cos(a + spread) * R;
    const y3 = cy + Math.sin(a + spread) * R;
    paths.push(`M${x1.toFixed(1)},${y1.toFixed(1)} L${x2.toFixed(1)},${y2.toFixed(1)} L${x3.toFixed(1)},${y3.toFixed(1)} Z`);
  }
  return <path d={paths.join(" ")} fill={color} opacity={opacity} />;
}

export function MotionLines({
  x,
  y,
  w,
  count = 8,
  spacing = 12,
  color = "#fff",
  angle = 0,
  seed = 9,
}: {
  x: number;
  y: number;
  w: number;
  count?: number;
  spacing?: number;
  color?: string;
  angle?: number;
  seed?: number;
}) {
  const r = rng(seed);
  return (
    <g transform={`rotate(${angle} ${x} ${y})`} stroke={color} strokeLinecap="round">
      {Array.from({ length: count }).map((_, i) => {
        const len = w * (0.5 + r() * 0.5);
        const off = r() * (w - len);
        return (
          <line
            key={i}
            x1={x + off}
            y1={y + i * spacing}
            x2={x + off + len}
            y2={y + i * spacing}
            strokeWidth={2 + r() * 3}
            opacity={0.5 + r() * 0.5}
          />
        );
      })}
    </g>
  );
}

export function Rain({ w, h, count = 90, seed = 4, color = "#9FD8FF", opacity = 0.45 }: { w: number; h: number; count?: number; seed?: number; color?: string; opacity?: number }) {
  const r = rng(seed);
  const d: string[] = [];
  for (let i = 0; i < count; i++) {
    const x = r() * (w + 60);
    const y = r() * h;
    const l = 14 + r() * 22;
    d.push(`M${x.toFixed(1)},${y.toFixed(1)} l${(-l * 0.28).toFixed(1)},${l.toFixed(1)}`);
  }
  return <path d={d.join(" ")} stroke={color} strokeWidth={1.6} opacity={opacity} strokeLinecap="round" />;
}

export function Halftone({ w, h, kind = "dots", opacity = 1 }: { w: number; h: number; kind?: "dots" | "dotsW" | "dotsBig" | "dotsM" | "dotsC"; opacity?: number }) {
  const { url } = usePage();
  return <rect width={w} height={h} fill={url(kind)} opacity={opacity} />;
}
