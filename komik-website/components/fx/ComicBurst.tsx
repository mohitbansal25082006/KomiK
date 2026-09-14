import React from "react";

function burstPoints(spikes: number, inner = 0.72, seed = 3) {
  let s = seed;
  const rand = () => {
    s = (s * 9301 + 49297) % 233280;
    return s / 233280;
  };
  const pts: string[] = [];
  const n = spikes * 2;
  for (let i = 0; i < n; i++) {
    const a = (i / n) * Math.PI * 2 - Math.PI / 2;
    const r = i % 2 === 0 ? 50 * (0.92 + rand() * 0.08) : 50 * inner * (0.9 + rand() * 0.15);
    pts.push(`${(50 + Math.cos(a) * r).toFixed(2)},${(50 + Math.sin(a) * r).toFixed(2)}`);
  }
  return pts.join(" ");
}

/** A classic comic "starburst" badge with centered content. */
export default function ComicBurst({
  size = 140,
  fill = "#FFD700",
  stroke = "#000",
  spikes = 12,
  inner = 0.72,
  seed = 3,
  shadow = true,
  className = "",
  children,
}: {
  size?: number;
  fill?: string;
  stroke?: string;
  spikes?: number;
  inner?: number;
  seed?: number;
  shadow?: boolean;
  className?: string;
  children?: React.ReactNode;
}) {
  const points = burstPoints(spikes, inner, seed);
  return (
    <div className={`relative flex items-center justify-center text-center ${className}`} style={{ width: size, height: size }}>
      <svg viewBox="-4 -4 110 110" className="absolute inset-0 h-full w-full overflow-visible" aria-hidden>
        {shadow && <polygon points={points} fill="#000" transform="translate(4 4)" />}
        <polygon points={points} fill={fill} stroke={stroke} strokeWidth={3} strokeLinejoin="round" />
      </svg>
      <div className="relative">{children}</div>
    </div>
  );
}
