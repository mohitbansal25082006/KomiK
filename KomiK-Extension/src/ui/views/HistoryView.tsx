import { AnimatePresence, motion } from "framer-motion";
import { useMemo, useState } from "react";
import { clearHistory, deleteHistory } from "@/shared/db";
import { send } from "@/shared/messages";
import type { HistoryEntry } from "@/shared/types";
import { formatBytes, timeAgo } from "@/shared/util";
import { Burst } from "../components/Brand";
import { Button, chipColor, Sticker } from "../components/Controls";
import { Icon } from "../components/Icon";

export function HistoryView({ items, onChanged }: { items: HistoryEntry[]; onChanged: () => void }) {
  const [query, setQuery] = useState("");
  const [confirmClear, setConfirmClear] = useState(false);
  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return items;
    return items.filter((h) => [h.title, h.series, h.sourceUrl, ...h.tags].some((v) => v.toLowerCase().includes(q)));
  }, [items, query]);
  const totalBytes = items.reduce((sum, h) => sum + h.bytes, 0);

  const redownload = async (h: HistoryEntry) => {
    await send("queue-jobs", { jobs: [{ meta: h.meta, format: h.format === "pdf" || h.format === "zip" || h.format === "folder" ? h.format : "cbz", pages: [], sourceUrl: h.sourceUrl, chapterUrl: h.sourceUrl }] });
  };

  if (!items.length) {
    return (
      <div className="grid flex-1 place-items-center p-6">
        <div className="flex flex-col items-center gap-3 text-center">
          <Burst className="h-28 w-28 animate-wobble" fill="#A78BFA">
            <Icon name="book" size={36} stroke={3} className="text-white drop-shadow-[2px_2px_0_#000]" />
          </Burst>
          <h2 className="letter text-[28px] leading-none">NO BACK ISSUES</h2>
          <p className="max-w-[260px] text-[13px] text-muted">Every comic you download shows up here, ready to open in Komik.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="flex items-center gap-2 border-b-[3px] border-gutter bg-panel px-3 py-2">
        <div className="relative flex-1">
          <Icon name="search" size={14} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted" />
          <input className="field !pl-8" placeholder={`Search ${items.length} downloads, series or tags`} value={query} onChange={(e) => setQuery(e.target.value)} />
        </div>
        {confirmClear ? (
          <>
            <Button size="sm" tone="magenta" onClick={async () => { await clearHistory(); setConfirmClear(false); onChanged(); }}>Clear all</Button>
            <Button size="sm" icon="x" title="Keep history" onClick={() => setConfirmClear(false)} />
          </>
        ) : (
          <Button size="sm" icon="trash" title="Clear history (files stay on disk)" onClick={() => setConfirmClear(true)} />
        )}
      </div>
      <div className="px-3 pt-2 text-[11.5px] font-semibold text-muted">{items.length} comics · {formatBytes(totalBytes)} downloaded</div>
      <div className="min-h-0 flex-1 space-y-2 overflow-y-auto p-3">
        <AnimatePresence initial={false}>
          {filtered.map((h, i) => (
            <motion.div
              layout
              key={h.id}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0, transition: { delay: Math.min(i, 12) * 0.02 } }}
              exit={{ opacity: 0, scale: 0.9 }}
              className="panel-flat group flex gap-2.5 p-2 shadow-comic-xs"
            >
              <div className="w-[44px] shrink-0">
                <div className="aspect-[2/3] overflow-hidden rounded-[3px] border-2 border-gutter bg-halftone-strong">
                  {h.coverThumb ? <img src={h.coverThumb} alt="" className="h-full w-full object-cover" /> : <div className="grid h-full place-items-center"><Icon name="book" size={16} className="text-muted" /></div>}
                </div>
              </div>
              <div className="min-w-0 flex-1">
                <div className="flex items-start gap-1.5">
                  <div className="min-w-0 flex-1 truncate text-[13px] font-extrabold leading-tight" title={h.title}>{h.title}</div>
                  <Sticker color="#FFFDF6" className="!text-[10.5px] !leading-[15px]">{h.format.toUpperCase()}</Sticker>
                </div>
                <div className="truncate text-[11px] text-muted">
                  {[h.pages ? `${h.pages} pages` : "", formatBytes(h.bytes), timeAgo(h.finishedAt)].filter(Boolean).join(" · ")}
                </div>
                {h.tags.length > 0 && (
                  <div className="mt-1 flex flex-wrap gap-1">
                    {h.tags.slice(0, 4).map((t) => (
                      <span key={t} className="rounded-full border-[1.5px] border-gutter px-1.5 text-[10px] font-bold leading-[14px] text-gutter" style={{ background: chipColor(t) }}>{t}</span>
                    ))}
                    {h.tags.length > 4 && <span className="text-[10px] font-bold text-muted">+{h.tags.length - 4}</span>}
                  </div>
                )}
                <div className="mt-1.5 flex flex-wrap gap-1.5">
                  {h.downloadId !== undefined && <Button size="sm" tone="yellow" icon="open" onClick={() => chrome.downloads.open(h.downloadId!)}>Open</Button>}
                  {h.downloadId !== undefined && <Button size="sm" icon="folder" title="Show in folder" onClick={() => chrome.downloads.show(h.downloadId!)} />}
                  <Button size="sm" icon="retry" title="Download again from the source page" onClick={() => redownload(h)} />
                  <Button size="sm" icon="link" title="Open the source page" onClick={() => chrome.tabs.create({ url: h.sourceUrl })} />
                  <Button size="sm" icon="trash" title="Remove from history (the file stays)" onClick={async () => { await deleteHistory(h.id); onChanged(); }} />
                </div>
              </div>
            </motion.div>
          ))}
        </AnimatePresence>
        {!filtered.length && <p className="py-8 text-center text-[13px] text-muted">Nothing matches "{query}".</p>}
      </div>
    </div>
  );
}
