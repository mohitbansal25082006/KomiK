import { motion } from "framer-motion";
import { useMemo, useState } from "react";
import type { ChapterRef, FileLink, PageRef } from "@/shared/types";
import { formatBytes } from "@/shared/util";
import { Button, Sticker } from "../components/Controls";
import { Icon } from "../components/Icon";
import { RemoteImage } from "../components/RemoteImage";

export function PagesGrid({ pages, excluded, coverIndex, onToggle, onCover, onSelectAll, onSelectNone, onReverse, columns = 4 }: {
  pages: PageRef[];
  excluded: Set<number>;
  coverIndex: number;
  onToggle: (i: number) => void;
  onCover: (i: number) => void;
  onSelectAll: () => void;
  onSelectNone: () => void;
  onReverse: () => void;
  columns?: number;
}) {
  const [showAll, setShowAll] = useState(false);
  const limit = showAll ? pages.length : Math.min(pages.length, columns * 6);
  const selected = pages.length - excluded.size;
  return (
    <div>
      <div className="mb-2 flex flex-wrap items-center gap-1.5">
        <Sticker color="#00C2FF">{selected} / {pages.length} PAGES</Sticker>
        <div className="ml-auto flex gap-1.5">
          <Button size="sm" onClick={onSelectAll}>All</Button>
          <Button size="sm" onClick={onSelectNone}>None</Button>
          <Button size="sm" icon="reverse" title="Reverse page order" onClick={onReverse} />
        </div>
      </div>
      <div className="grid gap-2" style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}>
        {pages.slice(0, limit).map((page, i) => {
          const off = excluded.has(i);
          const cover = i === coverIndex;
          return (
            <motion.div
              key={page.url}
              initial={{ opacity: 0, scale: 0.85, rotate: i % 2 ? 2 : -2 }}
              animate={{ opacity: 1, scale: 1, rotate: 0 }}
              transition={{ delay: Math.min(i, 24) * 0.018, type: "spring", stiffness: 420, damping: 24 }}
              className={`group relative aspect-[2/3] overflow-hidden rounded-[4px] border-[2.5px] border-gutter bg-panel-card shadow-comic-xs ${off ? "opacity-35 grayscale" : ""}`}
            >
              <button className="absolute inset-0 z-[1]" title={off ? "Include this page" : "Skip this page"} onClick={() => onToggle(i)} aria-pressed={!off} />
              <RemoteImage src={page.url} referer={page.referer} className="h-full w-full object-cover object-top" fallback={<Icon name="pages" className="text-muted" />} />
              <span className="absolute bottom-0 left-0 z-[2] rounded-tr-[4px] border-r-2 border-t-2 border-gutter bg-yellow px-1.5 font-bangers text-[12px] leading-[16px] text-gutter">{i + 1}</span>
              {off && <span className="absolute inset-0 z-[2] grid place-items-center"><Icon name="x" size={34} stroke={4} className="text-magenta drop-shadow-[2px_2px_0_#000]" /></span>}
              <button
                title={cover ? "Cover page" : "Use as cover"}
                onClick={() => onCover(i)}
                className={`absolute right-1 top-1 z-[3] grid h-6 w-6 place-items-center rounded-full border-2 border-gutter transition ${cover ? "bg-yellow text-gutter" : "bg-white/90 text-gutter opacity-0 group-hover:opacity-100"}`}
              >
                <Icon name="star" size={13} stroke={2.6} />
              </button>
              {page.width && page.height ? (
                <span className="absolute right-0 bottom-0 z-[2] hidden rounded-tl-[4px] bg-black/70 px-1 font-mono text-[9px] text-white group-hover:block">{page.width}×{page.height}</span>
              ) : null}
            </motion.div>
          );
        })}
      </div>
      {pages.length > limit && (
        <button onClick={() => setShowAll(true)} className="mt-2 w-full rounded-md border-[2.5px] border-dashed border-gutter/60 py-1.5 font-bangers tracking-wider text-muted hover:bg-cyan/15">
          SHOW ALL {pages.length} PAGES
        </button>
      )}
    </div>
  );
}

export function ChapterList({ chapters, selected, onChange }: { chapters: ChapterRef[]; selected: Set<string>; onChange: (s: Set<string>) => void }) {
  const [query, setQuery] = useState("");
  const [newestFirst, setNewestFirst] = useState(true);
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const list = useMemo(() => {
    const q = query.trim().toLowerCase();
    const filtered = chapters.filter((c) => !q || c.title.toLowerCase().includes(q) || String(c.number) === q);
    return newestFirst ? [...filtered].reverse() : filtered;
  }, [chapters, query, newestFirst]);

  const applyRange = () => {
    const lo = Number.parseFloat(from);
    const hi = Number.parseFloat(to);
    const next = new Set<string>();
    for (const c of chapters) {
      if (c.number === null) continue;
      if ((Number.isNaN(lo) || c.number >= lo) && (Number.isNaN(hi) || c.number <= hi)) next.add(c.url);
    }
    onChange(next);
  };

  return (
    <div>
      <div className="mb-2 flex flex-wrap items-center gap-1.5">
        <Sticker color="#FF7A00">{selected.size} / {chapters.length} CHAPTERS</Sticker>
        <div className="ml-auto flex gap-1.5">
          <Button size="sm" onClick={() => onChange(new Set(chapters.map((c) => c.url)))}>All</Button>
          <Button size="sm" onClick={() => onChange(new Set())}>None</Button>
          <Button size="sm" icon="sort" title={newestFirst ? "Newest first" : "Oldest first"} onClick={() => setNewestFirst((v) => !v)} />
        </div>
      </div>
      <div className="mb-2 flex items-center gap-1.5">
        <div className="relative flex-1">
          <Icon name="search" size={14} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted" />
          <input className="field !pl-8" value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Find a chapter" />
        </div>
        <input className="field !w-[58px] text-center" inputMode="decimal" placeholder="From" value={from} onChange={(e) => setFrom(e.target.value)} />
        <input className="field !w-[58px] text-center" inputMode="decimal" placeholder="To" value={to} onChange={(e) => setTo(e.target.value)} />
        <Button size="sm" tone="cyan" onClick={applyRange}>Range</Button>
      </div>
      <div className="max-h-[300px] space-y-1 overflow-y-auto pr-1">
        {list.map((c) => {
          const on = selected.has(c.url);
          return (
            <button
              key={c.url}
              onClick={() => {
                const next = new Set(selected);
                if (on) next.delete(c.url);
                else next.add(c.url);
                onChange(next);
              }}
              className={`flex w-full items-center gap-2 rounded-[5px] border-2 border-gutter px-2 py-1.5 text-left transition-colors ${on ? "bg-yellow text-gutter" : "bg-panel-card hover:bg-cyan/20"}`}
            >
              <span className={`grid h-[18px] w-[18px] shrink-0 place-items-center rounded-[4px] border-2 border-gutter ${on ? "bg-gutter text-yellow" : "bg-white"}`}>{on && <Icon name="check" size={12} stroke={4} />}</span>
              <span className="w-12 shrink-0 font-bangers text-[15px] leading-none tracking-wider">{c.volume !== null ? `V${c.volume}·` : ""}{c.number ?? "?"}</span>
              <span className="min-w-0 flex-1 truncate text-[12.5px] font-semibold">{c.title}</span>
              {c.date && <span className="shrink-0 text-[11px] text-muted">{c.date}</span>}
            </button>
          );
        })}
        {!list.length && <p className="py-6 text-center text-[13px] text-muted">No chapters match.</p>}
      </div>
    </div>
  );
}

export function FileList({ files, onSave }: { files: FileLink[]; onSave: (f: FileLink) => void }) {
  return (
    <div className="space-y-2">
      <p className="text-[12.5px] leading-snug text-muted">This site offers its own comic files. KomiK saves them into your KomiK folder with a proper name, and adds the details to CBZ, ZIP and PDF files.</p>
      {files.map((f) => (
        <div key={f.url} className="panel-flat flex items-center gap-2.5 p-2">
          <span className="grid h-10 w-10 shrink-0 place-items-center rounded-[5px] border-2 border-gutter bg-orange font-bangers text-[13px] text-gutter">{f.ext.toUpperCase()}</span>
          <div className="min-w-0 flex-1">
            <div className="truncate text-[13px] font-bold" title={f.name}>{f.name}</div>
            <div className="truncate text-[11px] text-muted">{[f.label, f.size ? formatBytes(f.size) : ""].filter(Boolean).join(" · ")}</div>
          </div>
          <Button size="sm" tone="yellow" icon="download" onClick={() => onSave(f)}>Save</Button>
        </div>
      ))}
    </div>
  );
}
