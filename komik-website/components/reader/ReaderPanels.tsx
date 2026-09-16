"use client";

import { useMemo, useState } from "react";
import {
  ArrowLeftRight,
  BookOpen,
  Bookmark,
  BookmarkCheck,
  Check,
  Copy,
  Moon,
  MoveHorizontal,
  MoveVertical,
  RotateCcw,
  Scan,
  ScanText,
  ScrollText,
  Search,
  Sun,
  Trash2,
  ZoomIn,
  ZoomOut,
} from "lucide-react";
import {
  ColorState,
  DEFAULT_COLOR,
  Direction,
  FitMode,
  PAGE_TITLES,
  PRESETS,
  PresetId,
  ViewMode,
  pageText,
  searchScript,
} from "./readerModel";

export function PanelTitle({ icon: Icon, children, right }: { icon: typeof Search; children: React.ReactNode; right?: React.ReactNode }) {
  return (
    <div className="mb-3 flex items-center justify-between gap-2">
      <div className="flex items-center gap-2 text-[13px] font-semibold text-[var(--r-text)]">
        <Icon className="h-4 w-4 text-[var(--r-accent-ink)]" />
        {children}
      </div>
      {right}
    </div>
  );
}

/* ----------------------------- Color ------------------------------ */

export function ColorPanel({ color, setColor }: { color: ColorState; setColor: (c: ColorState) => void }) {
  const applyPreset = (id: PresetId) => {
    const p = PRESETS.find((x) => x.id === id)!;
    setColor({ preset: id, brightness: p.brightness, contrast: p.contrast, warmth: p.warmth });
  };
  return (
    <div>
      <PanelTitle
        icon={Sun}
        right={
          <button
            type="button"
            onClick={() => setColor(DEFAULT_COLOR)}
            className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-[11px] text-[var(--r-muted)] hover:bg-[var(--r-hover)] hover:text-[var(--r-text)]"
          >
            <RotateCcw className="h-3 w-3" /> Reset
          </button>
        }
      >
        Color Correction &amp; Night Mode
      </PanelTitle>
      <div className="grid grid-cols-3 gap-1.5">
        {PRESETS.map((p) => (
          <button
            key={p.id}
            type="button"
            onClick={() => applyPreset(p.id)}
            className={`group flex flex-col items-center gap-1 rounded-lg border px-1 py-2 text-[11px] font-medium transition-colors ${
              color.preset === p.id
                ? "border-[var(--r-accent)] bg-[var(--r-accent-soft)] text-[var(--r-text)]"
                : "border-[var(--r-border)] text-[var(--r-muted)] hover:bg-[var(--r-hover)] hover:text-[var(--r-text)]"
            }`}
          >
            <span className="h-5 w-5 rounded-full border border-black/40" style={{ background: p.swatch }} />
            {p.label}
          </button>
        ))}
      </div>
      <div className="mt-4 space-y-3">
        <Slider label="Brightness" value={color.brightness} min={-100} max={100} step={1} display={`${color.brightness > 0 ? "+" : ""}${color.brightness}`} onChange={(v) => setColor({ ...color, brightness: v })} />
        <Slider label="Contrast" value={color.contrast} min={0.5} max={2} step={0.05} display={`${color.contrast.toFixed(2)}×`} onChange={(v) => setColor({ ...color, contrast: v })} />
        <Slider label="Warmth" value={color.warmth} min={-100} max={100} step={1} display={`${color.warmth > 0 ? "+" : ""}${color.warmth}`} onChange={(v) => setColor({ ...color, warmth: v })} />
      </div>
      <p className="mt-3 text-[11px] leading-snug text-[var(--r-muted)]">Non-destructive display shaders. Your original files are never modified.</p>
    </div>
  );
}

function Slider({ label, value, min, max, step, display, onChange }: { label: string; value: number; min: number; max: number; step: number; display: string; onChange: (v: number) => void }) {
  return (
    <label className="block">
      <div className="mb-1 flex justify-between text-[11px] text-[var(--r-muted)]">
        <span>{label}</span>
        <span className="font-mono text-[var(--r-text)]">{display}</span>
      </div>
      <input
        type="range"
        min={min}
        max={max}
        step={step}
        value={value}
        onChange={(e) => onChange(parseFloat(e.target.value))}
        className="reader-range w-full"
      />
    </label>
  );
}

/* --------------------------- Bookmarks ---------------------------- */

export function BookmarksPanel({
  bookmarks,
  page,
  toggle,
  setNote,
  remove,
  goTo,
}: {
  bookmarks: Record<number, string>;
  page: number;
  toggle: () => void;
  setNote: (p: number, note: string) => void;
  remove: (p: number) => void;
  goTo: (p: number) => void;
}) {
  const list = Object.keys(bookmarks)
    .map(Number)
    .sort((a, b) => a - b);
  const marked = page in bookmarks;
  return (
    <div>
      <PanelTitle icon={Bookmark}>Bookmarks</PanelTitle>
      <button
        type="button"
        onClick={toggle}
        className={`mb-3 flex w-full items-center justify-center gap-2 rounded-lg px-3 py-2 text-[12px] font-semibold transition-colors ${
          marked ? "bg-[var(--r-hover)] text-[var(--r-text)]" : "bg-[var(--r-accent)] text-black hover:brightness-110"
        }`}
      >
        {marked ? <BookmarkCheck className="h-4 w-4" /> : <Bookmark className="h-4 w-4" />}
        {marked ? `Remove bookmark on page ${page}` : `Bookmark page ${page}`}
        <kbd className="ml-1 rounded border border-current/30 px-1 font-mono text-[10px] opacity-70">Ctrl+D</kbd>
      </button>
      {list.length === 0 ? (
        <p className="rounded-lg border border-dashed border-[var(--r-border)] p-3 text-center text-[11px] text-[var(--r-muted)]">
          Save favorite panels and scenes with notes to jump back to them any time.
        </p>
      ) : (
        <ul className="space-y-1.5">
          {list.map((p) => (
            <li key={p} className={`rounded-lg border p-2 ${p === page ? "border-[var(--r-accent)]" : "border-[var(--r-border)]"}`}>
              <div className="flex items-center gap-2">
                <button type="button" onClick={() => goTo(p)} className="flex-1 text-left text-[12px] font-semibold text-[var(--r-text)] hover:underline">
                  Page {p} <span className="font-normal text-[var(--r-muted)]">· {PAGE_TITLES[p]}</span>
                </button>
                <button type="button" onClick={() => remove(p)} aria-label={`Remove bookmark on page ${p}`} className="rounded p-1 text-[var(--r-muted)] hover:bg-[var(--r-hover)] hover:text-[var(--r-text)]">
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </div>
              <input
                value={bookmarks[p]}
                onChange={(e) => setNote(p, e.target.value)}
                placeholder="Add a note…"
                className="mt-1 w-full rounded-md border border-[var(--r-border)] bg-transparent px-2 py-1 text-[11px] text-[var(--r-text)] placeholder:text-[var(--r-muted)] focus:border-[var(--r-accent)] focus:outline-none"
              />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

/* ----------------------------- Search ----------------------------- */

export function SearchPanel({ goTo, openOcr, page }: { goTo: (p: number) => void; openOcr: () => void; page: number }) {
  const [q, setQ] = useState("");
  const hits = useMemo(() => searchScript(q), [q]);
  return (
    <div>
      <PanelTitle icon={ScanText}>Offline OCR &amp; Dialogue Search</PanelTitle>
      <div className="relative">
        <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-[var(--r-muted)]" />
        <input
          autoFocus
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder="Search dialogue… try “offline”"
          className="w-full rounded-lg border border-[var(--r-border)] bg-[var(--r-input)] py-2 pl-8 pr-2 text-[12px] text-[var(--r-text)] placeholder:text-[var(--r-muted)] focus:border-[var(--r-accent)] focus:outline-none"
        />
      </div>
      <button
        type="button"
        onClick={openOcr}
        className="mt-2 flex w-full items-center justify-center gap-2 rounded-lg border border-[var(--r-border)] px-3 py-1.5 text-[12px] font-medium text-[var(--r-text)] hover:bg-[var(--r-hover)]"
      >
        <ScanText className="h-3.5 w-3.5" /> Recognize text on page {page}
      </button>
      <div className="mt-3">
        {q.trim().length >= 2 && (
          <div className="mb-1.5 text-[11px] text-[var(--r-muted)]">
            {hits.length} {hits.length === 1 ? "occurrence" : "occurrences"}
          </div>
        )}
        <ul className="space-y-1">
          {hits.slice(0, 30).map((h, i) => (
            <li key={i}>
              <button
                type="button"
                onClick={() => goTo(h.page)}
                className="w-full rounded-lg px-2 py-1.5 text-left text-[11px] leading-snug text-[var(--r-muted)] hover:bg-[var(--r-hover)]"
              >
                <span className="mr-1.5 rounded bg-[var(--r-accent)] px-1 font-mono text-[10px] font-bold text-black">P{h.page}</span>
                {h.text.slice(0, h.start)}
                <mark className="rounded-sm bg-[var(--r-accent-soft)] px-0.5 font-semibold text-[var(--r-text)]">{h.text.slice(h.start, h.start + h.len)}</mark>
                {h.text.slice(h.start + h.len)}
              </button>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

export function OcrOverlay({ page, onClose }: { page: number; onClose: () => void }) {
  const lines = pageText(page);
  const [copied, setCopied] = useState(false);
  const copy = async () => {
    try {
      await navigator.clipboard.writeText(lines.join("\n"));
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      /* clipboard unavailable */
    }
  };
  return (
    <div className="w-[min(92%,380px)] rounded-xl border border-[var(--r-border)] bg-[var(--r-flyout)] p-3 shadow-2xl">
      <div className="mb-2 flex items-center justify-between">
        <div className="flex items-center gap-2 text-[12px] font-semibold text-[var(--r-text)]">
          <ScanText className="h-3.5 w-3.5 text-[var(--r-accent-ink)]" /> Detected dialogue · page {page}
        </div>
        <div className="flex items-center gap-1">
          <button type="button" onClick={copy} className="inline-flex items-center gap-1 rounded-md px-2 py-1 text-[11px] text-[var(--r-muted)] hover:bg-[var(--r-hover)] hover:text-[var(--r-text)]">
            {copied ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />} {copied ? "Copied" : "Copy"}
          </button>
          <button type="button" onClick={onClose} className="rounded-md px-2 py-1 text-[11px] text-[var(--r-muted)] hover:bg-[var(--r-hover)] hover:text-[var(--r-text)]">
            ✕
          </button>
        </div>
      </div>
      <div data-lenis-prevent className="max-h-40 select-text space-y-1 overflow-auto font-mono text-[11px] leading-relaxed text-[var(--r-text)]">
        {lines.map((l, i) => (
          <p key={i}>{l}</p>
        ))}
      </div>
    </div>
  );
}

/* ------------------------ Mobile view menu ------------------------ */

export function ViewPanel({
  fit,
  setFit,
  spread,
  toggleSpread,
  view,
  toggleWebtoon,
  dir,
  toggleDir,
  zoom,
  setZoom,
  theme,
  toggleTheme,
}: {
  fit: FitMode;
  setFit: (f: FitMode) => void;
  spread: boolean;
  toggleSpread: () => void;
  view: ViewMode;
  toggleWebtoon: () => void;
  dir: Direction;
  toggleDir: () => void;
  zoom: number;
  setZoom: (z: number) => void;
  theme: "dark" | "light";
  toggleTheme: () => void;
}) {
  const chip = (active: boolean) =>
    `flex flex-col items-center gap-1 rounded-lg border px-1 py-2 text-[11px] font-medium ${
      active ? "border-[var(--r-accent)] bg-[var(--r-accent-soft)] text-[var(--r-text)]" : "border-[var(--r-border)] text-[var(--r-muted)]"
    }`;
  return (
    <div>
      <PanelTitle icon={BookOpen}>Reading View</PanelTitle>
      <div className="grid grid-cols-3 gap-1.5">
        <button type="button" className={chip(fit === "width")} onClick={() => setFit("width")}>
          <MoveHorizontal className="h-4 w-4" /> Fit Width
        </button>
        <button type="button" className={chip(fit === "height")} onClick={() => setFit("height")}>
          <MoveVertical className="h-4 w-4" /> Fit Height
        </button>
        <button type="button" className={chip(fit === "actual")} onClick={() => setFit("actual")}>
          <Scan className="h-4 w-4" /> Actual
        </button>
        <button type="button" className={chip(spread && view === "paged")} onClick={toggleSpread}>
          <BookOpen className="h-4 w-4" /> 2-Page
        </button>
        <button type="button" className={chip(view === "webtoon")} onClick={toggleWebtoon}>
          <ScrollText className="h-4 w-4" /> Webtoon
        </button>
        <button type="button" className={chip(dir === "rtl")} onClick={toggleDir}>
          <ArrowLeftRight className="h-4 w-4" /> {dir === "rtl" ? "Manga RTL" : "Western"}
        </button>
      </div>
      <div className="mt-3 flex items-center gap-2">
        <button type="button" onClick={() => setZoom(zoom - 0.1)} className="rounded-lg border border-[var(--r-border)] p-2 text-[var(--r-text)]" aria-label="Zoom out">
          <ZoomOut className="h-4 w-4" />
        </button>
        <button type="button" onClick={() => setZoom(1)} className="flex-1 rounded-lg border border-[var(--r-border)] py-2 font-mono text-[12px] text-[var(--r-text)]">
          {Math.round(zoom * 100)}%
        </button>
        <button type="button" onClick={() => setZoom(zoom + 0.1)} className="rounded-lg border border-[var(--r-border)] p-2 text-[var(--r-text)]" aria-label="Zoom in">
          <ZoomIn className="h-4 w-4" />
        </button>
        <button type="button" onClick={toggleTheme} className="rounded-lg border border-[var(--r-border)] p-2 text-[var(--r-text)]" aria-label="Toggle theme">
          {theme === "dark" ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
        </button>
      </div>
    </div>
  );
}
