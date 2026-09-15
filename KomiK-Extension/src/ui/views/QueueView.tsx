import { AnimatePresence, motion } from "framer-motion";
import { useEffect, useMemo, useState } from "react";
import { send, type JobAction } from "@/shared/messages";
import type { Job, JobsSnapshot } from "@/shared/types";
import { formatBytes, formatEta, formatSpeed } from "@/shared/util";
import { Burst } from "../components/Brand";
import { Button, InkProgress, Sticker } from "../components/Controls";
import { Icon } from "../components/Icon";
import { RemoteImage } from "../components/RemoteImage";
import { usePrevious } from "../hooks";
import { sfx } from "../sound";

const STATUS: Record<Job["status"], { label: string; color: string }> = {
  queued: { label: "QUEUED", color: "#E4DCD3" },
  resolving: { label: "FINDING PAGES", color: "#A78BFA" },
  downloading: { label: "DOWNLOADING", color: "#00C2FF" },
  packing: { label: "PACKING", color: "#FF7A00" },
  saving: { label: "SAVING", color: "#FF7A00" },
  paused: { label: "PAUSED", color: "#FFFFFF" },
  done: { label: "DONE!", color: "#2FD17A" },
  error: { label: "OOPS", color: "#FF1F6D" },
  cancelled: { label: "CANCELLED", color: "#9EA4B0" }
};

function progressOf(job: Job): number {
  if (job.status === "done") return 1;
  if (job.file) return job.totalBytes ? job.doneBytes / job.totalBytes : job.status === "packing" || job.status === "saving" ? 0.95 : 0.05;
  if (!job.pages.length) return 0;
  const pages = job.pages.filter((p) => p.done).length / job.pages.length;
  if (job.status === "packing") return 0.97;
  if (job.status === "saving") return 0.99;
  return pages * 0.95;
}

export function JobCard({ job, compact }: { job: Job; compact?: boolean }) {
  const [open, setOpen] = useState(false);
  const act = (action: JobAction) => send("job-action", { id: job.id, action });
  const s = STATUS[job.status];
  const done = job.pages.filter((p) => p.done).length;
  const progress = progressOf(job);
  const active = ["resolving", "downloading", "packing", "saving"].includes(job.status);
  const remainingBytes = job.pages.length && done ? (job.doneBytes / done) * (job.pages.length - done) : job.totalBytes - job.doneBytes;
  const eta = active && job.speedBps > 0 ? formatEta(remainingBytes / job.speedBps) : "";
  const failed = job.pages.filter((p) => p.error).length;

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 14, scale: 0.97 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      exit={{ opacity: 0, x: 40, scale: 0.9 }}
      transition={{ type: "spring", stiffness: 420, damping: 30 }}
      className={`panel relative overflow-hidden ${job.status === "done" ? "!shadow-[5px_5px_0_#2FD17A]" : job.status === "error" ? "!shadow-[5px_5px_0_#FF1F6D]" : ""}`}
    >
      <div className="flex gap-2.5 p-2.5">
        <div className="w-[46px] shrink-0">
          <div className="aspect-[2/3] overflow-hidden rounded-[3px] border-2 border-gutter bg-panel-card">
            <RemoteImage src={job.meta.coverUrl || job.pages[0]?.url} referer={job.pages[0]?.referer ?? job.sourceUrl} className="h-full w-full object-cover" fallback={<Icon name={job.file ? "file" : "book"} size={18} className="text-muted" />} />
          </div>
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex items-start gap-2">
            <div className="min-w-0 flex-1">
              <div className="truncate text-[13.5px] font-extrabold leading-tight" title={job.meta.title}>{job.meta.title}</div>
              <div className="truncate text-[11.5px] text-muted">
                {job.file ? `${job.file.ext.toUpperCase()} file` : `${job.format.toUpperCase()}`}
                {job.pages.length ? ` · ${done}/${job.pages.length} pages` : ""}
                {job.doneBytes ? ` · ${formatBytes(job.doneBytes)}` : ""}
                {active && job.speedBps ? ` · ${formatSpeed(job.speedBps)}` : ""}
                {eta ? ` · ${eta}` : ""}
              </div>
            </div>
            <Sticker color={s.color} className="!text-[11.5px] !leading-[17px]">{s.label}</Sticker>
          </div>
          <div className="mt-2">
            <InkProgress value={progress} moving={active} color={job.status === "error" ? "#FF1F6D" : job.status === "done" ? "#2FD17A" : job.status === "paused" ? "#E4DCD3" : "#FFD700"} label={`${job.meta.title} progress`} />
          </div>
          {job.error && <p className="mt-1.5 line-clamp-2 text-[11.5px] font-semibold leading-snug text-magenta">{job.error}</p>}
          {job.savedPath && job.status === "done" && (
            <p className="mt-1.5 truncate font-mono text-[10.5px] text-muted" title={job.savedPath}>
              <Icon name="folder" size={11} className="mr-1 inline -translate-y-px" />
              {job.savedPath}
            </p>
          )}
          <div className="mt-2 flex flex-wrap items-center gap-1.5">
            {job.status === "done" && job.downloadId !== undefined && (
              <>
                <Button size="sm" tone="yellow" icon="open" onClick={() => chrome.downloads.open(job.downloadId!)}>Open in Komik</Button>
                {!compact && <Button size="sm" icon="folder" onClick={() => chrome.downloads.show(job.downloadId!)}>Folder</Button>}
              </>
            )}
            {active || job.status === "queued" ? <Button size="sm" icon="pause" title="Pause" onClick={() => act("pause")} /> : null}
            {job.status === "paused" && <Button size="sm" tone="cyan" icon="play" onClick={() => act("resume")}>Resume</Button>}
            {job.status === "error" && <Button size="sm" tone="cyan" icon="retry" onClick={() => act("retry")}>Retry{failed ? ` ${failed}` : ""}</Button>}
            {!["done", "cancelled", "error"].includes(job.status) && <Button size="sm" icon="x" title="Cancel" onClick={() => act("cancel")} />}
            {["done", "cancelled", "error", "paused"].includes(job.status) && <Button size="sm" icon="trash" title="Remove from list" onClick={() => act("remove")} />}
            {(job.warnings.length > 0 || failed > 0) && (
              <button onClick={() => setOpen((o) => !o)} className="ml-auto flex items-center gap-1 text-[11px] font-bold text-muted hover:text-text">
                <Icon name="info" size={13} /> {open ? "Hide" : "Details"}
              </button>
            )}
          </div>
          <AnimatePresence>
            {open && (
              <motion.ul initial={{ height: 0, opacity: 0 }} animate={{ height: "auto", opacity: 1 }} exit={{ height: 0, opacity: 0 }} className="mt-2 space-y-1 overflow-hidden text-[11.5px] leading-snug text-muted">
                {job.warnings.map((w) => <li key={w}>• {w}</li>)}
                {job.pages.filter((p) => p.error).slice(0, 6).map((p) => <li key={p.index} className="text-magenta">• Page {p.index + 1}: {p.error}</li>)}
              </motion.ul>
            )}
          </AnimatePresence>
        </div>
      </div>
    </motion.div>
  );
}

export function QueueView({ snapshot, onScan }: { snapshot: JobsSnapshot; onScan?: () => void }) {
  const jobs = snapshot.jobs;
  const previous = usePrevious(jobs);
  useEffect(() => {
    if (!previous) return;
    const before = new Map(previous.map((j) => [j.id, j.status]));
    if (jobs.some((j) => j.status === "done" && before.get(j.id) && before.get(j.id) !== "done")) sfx.ding();
  }, [jobs, previous]);

  const groups = useMemo(() => {
    const ordered = [...jobs].sort((a, b) => {
      const rank = (j: Job) => (["resolving", "downloading", "packing", "saving"].includes(j.status) ? 0 : j.status === "queued" ? 1 : j.status === "paused" || j.status === "error" ? 2 : 3);
      return rank(a) - rank(b) || b.createdAt - a.createdAt;
    });
    const out: Array<{ key: string; label?: string; jobs: Job[] }> = [];
    for (const job of ordered) {
      const key = job.batchId ?? job.id;
      const g = out.find((x) => x.key === key);
      if (g) g.jobs.push(job);
      else out.push({ key, label: job.batchId ? job.batchLabel : undefined, jobs: [job] });
    }
    return out;
  }, [jobs]);

  const finished = jobs.filter((j) => j.status === "done" || j.status === "cancelled").length;
  const allDone = jobs.filter((j) => j.status === "done").length;

  if (!jobs.length) {
    return (
      <div className="grid flex-1 place-items-center p-6">
        <div className="flex flex-col items-center gap-3 text-center">
          <Burst className="h-28 w-28 animate-wobble" fill="#00C2FF">
            <Icon name="download" size={36} stroke={3} className="text-white drop-shadow-[2px_2px_0_#000]" />
          </Burst>
          <h2 className="letter text-[28px] leading-none">QUIET PANEL…</h2>
          <p className="max-w-[260px] text-[13px] text-muted">Nothing downloading yet. Open a comic page and hit the big pink button.</p>
          {onScan && <Button tone="yellow" icon="search" onClick={onScan}>Scan this page</Button>}
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <div className="relative flex items-center gap-2 border-b-[3px] border-gutter bg-yellow px-3 py-2 text-gutter">
        <div className="absolute inset-0 bg-halftone-yellow" />
        <div className="relative flex flex-1 items-center gap-3">
          <Stat n={snapshot.active} l="active" />
          <Stat n={snapshot.queued} l="waiting" />
          <Stat n={allDone} l="done" />
          <div className="ml-auto text-right">
            <div className="letter text-[20px] leading-none">{formatSpeed(snapshot.speedBps)}</div>
            <div className="font-comic text-[10px] font-bold uppercase tracking-wider">total speed</div>
          </div>
        </div>
      </div>
      <div className="min-h-0 flex-1 space-y-3 overflow-y-auto p-3">
        {finished > 0 && (
          <div className="flex justify-end">
            <Button size="sm" icon="check" onClick={() => send("clear-finished", {})}>Clear finished ({finished})</Button>
          </div>
        )}
        <AnimatePresence initial={false}>
          {groups.map((g) =>
            g.label ? (
              <motion.div layout key={g.key} className="space-y-2 rounded-md border-[2.5px] border-dashed border-gutter/50 p-2">
                <div className="flex items-center gap-2">
                  <Sticker color="#FF7A00">BATCH</Sticker>
                  <span className="min-w-0 flex-1 truncate text-[12.5px] font-bold">{g.label}</span>
                  <span className="text-[11.5px] font-bold text-muted">{g.jobs.filter((j) => j.status === "done").length}/{g.jobs.length}</span>
                </div>
                {g.jobs.map((job) => <JobCard key={job.id} job={job} compact />)}
              </motion.div>
            ) : (
              <JobCard key={g.key} job={g.jobs[0]} />
            )
          )}
        </AnimatePresence>
      </div>
    </div>
  );
}

function Stat({ n, l }: { n: number; l: string }) {
  return (
    <div className="leading-none">
      <div className="letter text-[22px]">{n}</div>
      <div className="font-comic text-[10px] font-bold uppercase tracking-wider">{l}</div>
    </div>
  );
}
