// A compact hand-drawn-feeling icon set (stroke icons, ink weight).
const PATHS: Record<string, string> = {
  download: "M12 3v12m0 0l-5-5m5 5l5-5M4 17v3h16v-3",
  refresh: "M20 11a8 8 0 10-2.3 5.7M20 4v7h-7",
  cursor: "M5 3l6 17 2.5-7L20 10.5z",
  queue: "M4 6h16M4 12h10M4 18h7m9-3v6m-3-3h6",
  settings: "M12 15a3 3 0 100-6 3 3 0 000 6zm7.4-3a7.4 7.4 0 00-.1-1.2l2-1.6-2-3.4-2.4 1a7 7 0 00-2-1.2L14.5 3h-4l-.4 2.6a7 7 0 00-2 1.2l-2.4-1-2 3.4 2 1.6a7.4 7.4 0 000 2.4l-2 1.6 2 3.4 2.4-1a7 7 0 002 1.2l.4 2.6h4l.4-2.6a7 7 0 002-1.2l2.4 1 2-3.4-2-1.6c.1-.4.1-.8.1-1.2z",
  pause: "M8 5v14M16 5v14",
  play: "M7 4l13 8-13 8z",
  x: "M6 6l12 12M18 6L6 18",
  retry: "M4 12a8 8 0 0114-5.3L20 9M20 4v5h-5M20 12a8 8 0 01-14 5.3L4 15m0 5v-5h5",
  trash: "M4 7h16M9 7V4h6v3m-9 0l1 13h10l1-13",
  folder: "M3 6h6l2 2h10v11H3z",
  open: "M14 4h6v6m0-6L10 14M18 14v6H4V6h6",
  star: "M12 3l2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1L3.2 9.5l6.1-.9z",
  check: "M4 12l5 5L20 6",
  chevron: "M9 6l6 6-6 6",
  search: "M11 18a7 7 0 100-14 7 7 0 000 14zm10 3l-5-5",
  tag: "M3 12V3h9l9 9-9 9zM7.5 7.5h.01",
  book: "M4 5a2 2 0 012-2h13v16H6a2 2 0 00-2 2zm0 0v16M8 7h7",
  pages: "M8 3h9l3 3v12H8zM4 7v14h12",
  file: "M6 3h8l4 4v14H6zM14 3v4h4",
  link: "M10 14a4 4 0 005.7 0l3-3a4 4 0 00-5.7-5.7l-1 1M14 10a4 4 0 00-5.7 0l-3 3a4 4 0 005.7 5.7l1-1",
  sun: "M12 17a5 5 0 100-10 5 5 0 000 10zM12 1v2m0 18v2M4.2 4.2l1.4 1.4m12.8 12.8l1.4 1.4M1 12h2m18 0h2M4.2 19.8l1.4-1.4M18.4 5.6l1.4-1.4",
  moon: "M21 13A9 9 0 1111 3a7 7 0 0010 10z",
  bolt: "M13 2L4 14h7l-1 8 9-12h-7z",
  sparkle: "M12 3l1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8zM19 16l.8 2.2L22 19l-2.2.8L19 22l-.8-2.2L16 19l2.2-.8z",
  sort: "M7 4v16m0 0l-3-3m3 3l3-3M17 20V4m0 0l-3 3m3-3l3 3",
  panel: "M3 4h18v16H3zM15 4v16",
  info: "M12 22a10 10 0 100-20 10 10 0 000 20zm0-6v-5m0-3h.01",
  grip: "M9 5h.01M9 12h.01M9 19h.01M15 5h.01M15 12h.01M15 19h.01",
  eye: "M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12zm10 3a3 3 0 100-6 3 3 0 000 6z",
  reverse: "M4 7h13l-3-3M20 17H7l3 3",
  globe: "M12 22a10 10 0 100-20 10 10 0 000 20zM2 12h20M12 2c3 3 3 17 0 20M12 2c-3 3-3 17 0 20",
  keyboard: "M3 6h18v12H3zM7 10h.01M11 10h.01M15 10h.01M7 14h10",
  shield: "M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z",
  plus: "M12 5v14M5 12h14",
  upload: "M12 21V9m0 0l-5 5m5-5l5 5M4 7V4h16v3"
};

export function Icon({ name, size = 18, className = "", stroke = 2.4 }: { name: keyof typeof PATHS | string; size?: number; className?: string; stroke?: number }) {
  return (
    <svg viewBox="0 0 24 24" width={size} height={size} className={`shrink-0 ${className}`} fill="none" stroke="currentColor" strokeWidth={stroke} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d={PATHS[name] ?? PATHS.info} />
    </svg>
  );
}
