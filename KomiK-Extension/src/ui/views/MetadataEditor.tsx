import { motion } from "framer-motion";
import { composeTitle } from "@/shared/identity";
import { planSave } from "@/shared/naming";
import type { ComicMeta, OutputFormat, Settings } from "@/shared/types";
import { ChipInput, Field, Select } from "../components/Controls";
import { Crest } from "../components/Brand";
import { Icon } from "../components/Icon";

export function MetadataEditor({ meta, onChange, settings, format, pages, compact }: { meta: ComicMeta; onChange: (m: ComicMeta) => void; settings: Settings; format: OutputFormat; pages: number; compact?: boolean }) {
  const set = <K extends keyof ComicMeta>(key: K, value: ComicMeta[K]) => {
    const next = { ...meta, [key]: value };
    // Keep the title in sync while it still matches the automatic one.
    const auto = composeTitle({ series: meta.series, volume: meta.volume, number: meta.number, kind: meta.numberKind, chapterTitle: meta.chapterTitle, includeChapterTitle: settings.includeChapterTitle });
    if (["series", "volume", "number", "numberKind", "chapterTitle"].includes(key) && (meta.title === auto || !meta.title)) {
      next.title = composeTitle({ series: next.series, volume: next.volume, number: next.number, kind: next.numberKind, chapterTitle: next.chapterTitle, includeChapterTitle: settings.includeChapterTitle }) || next.title;
    }
    onChange(next);
  };
  const plan = planSave(meta, format, settings, pages);
  const grid = compact ? "grid-cols-2" : "grid-cols-4";

  return (
    <div className="space-y-3">
      <motion.div initial={{ y: 6, opacity: 0 }} animate={{ y: 0, opacity: 1 }} className="panel-flat relative overflow-hidden bg-cyan/15 p-2.5">
        <div className="absolute inset-0 bg-halftone opacity-60" />
        <div className="relative flex items-start gap-2.5">
          <Crest size={30} />
          <div className="min-w-0 text-[12px] leading-snug">
            <div className="letter text-[15px] leading-none">KOMIK WILL READ IT AS</div>
            <div className="mt-1 truncate font-bold" title={meta.title}>{meta.title || "Untitled"}</div>
            <div className="text-muted">
              {[meta.series && `Series: ${meta.series}`, meta.volume && `Vol. ${meta.volume}`, meta.number && `#${meta.number}`, meta.writers[0] && `by ${meta.writers[0]}`].filter(Boolean).join(" · ") || "Add a series and number so Komik groups it"}
            </div>
            <div className="mt-1 flex items-center gap-1 truncate font-mono text-[11px] text-muted" title={plan.relativePath}>
              <Icon name="folder" size={12} /> {settings.folderMode === "custom" ? `${settings.customFolderName || "Custom folder"}/` : "Downloads/"}
              {plan.relativePath}
            </div>
          </div>
        </div>
      </motion.div>

      <Field label="Title (file name)">
        <input className="field" value={meta.title} onChange={(e) => onChange({ ...meta, title: e.target.value })} placeholder="Solo Adventure Ch. 12" />
      </Field>
      <div className={`grid ${grid} gap-2`}>
        <Field label="Series" className={compact ? "col-span-2" : "col-span-2"}>
          <input className="field" value={meta.series} onChange={(e) => set("series", e.target.value)} placeholder="Series name" />
        </Field>
        <Field label="Volume">
          <input className="field" inputMode="decimal" value={meta.volume} onChange={(e) => set("volume", e.target.value.replace(/[^\d.]/g, ""))} placeholder="—" />
        </Field>
        <Field label="Number">
          <div className="flex gap-1">
            <input className="field min-w-0" inputMode="decimal" value={meta.number} onChange={(e) => set("number", e.target.value.replace(/[^\d.]/g, ""))} placeholder="—" />
          </div>
        </Field>
        <Field label="Number is a" className={compact ? "" : ""}>
          <Select
            value={meta.numberKind === "none" ? "chapter" : meta.numberKind}
            onChange={(v) => set("numberKind", v)}
            options={[
              { id: "chapter", label: "Chapter" },
              { id: "issue", label: "Issue #" },
              { id: "episode", label: "Episode" },
              { id: "volume", label: "Volume" }
            ]}
          />
        </Field>
        <Field label="Chapter name" className={compact ? "" : "col-span-3"}>
          <input className="field" value={meta.chapterTitle} onChange={(e) => set("chapterTitle", e.target.value)} placeholder="Optional" />
        </Field>
      </div>

      <Field label="Writers">
        <ChipInput values={meta.writers} onChange={(v) => onChange({ ...meta, writers: v })} placeholder="Type a name, press Enter" colorful={false} />
      </Field>
      <Field label="Artists">
        <ChipInput values={meta.artists} onChange={(v) => onChange({ ...meta, artists: v })} placeholder="Type a name, press Enter" colorful={false} />
      </Field>
      <Field label={`Genres · ${meta.genres.length}`}>
        <ChipInput values={meta.genres} onChange={(v) => onChange({ ...meta, genres: v })} placeholder="Action, Fantasy…" />
      </Field>
      <Field label={`Tags · ${meta.tags.length}`}>
        <ChipInput values={meta.tags} onChange={(v) => onChange({ ...meta, tags: v })} placeholder="Add tags Komik will show" />
      </Field>

      <div className={`grid ${grid} gap-2`}>
        <Field label="Publisher" className="col-span-2">
          <input className="field" value={meta.publisher} onChange={(e) => onChange({ ...meta, publisher: e.target.value })} placeholder="—" />
        </Field>
        <Field label="Year">
          <input className="field" inputMode="numeric" maxLength={4} value={meta.year} onChange={(e) => onChange({ ...meta, year: e.target.value.replace(/\D/g, "") })} placeholder="YYYY" />
        </Field>
        <Field label="Language">
          <input className="field" maxLength={3} value={meta.language} onChange={(e) => onChange({ ...meta, language: e.target.value.toLowerCase().replace(/[^a-z]/g, "") })} placeholder="en" />
        </Field>
        <Field label="Reading" className="col-span-2">
          <Select
            value={(meta.manga || "No") as "No" | "Yes" | "YesAndRightToLeft"}
            onChange={(v) => onChange({ ...meta, manga: v === "No" ? "" : v })}
            options={[
              { id: "No", label: "Comic (left to right)" },
              { id: "Yes", label: "Manga / webtoon" },
              { id: "YesAndRightToLeft", label: "Manga (right to left)" }
            ]}
          />
        </Field>
        <Field label="Age rating" className="col-span-2">
          <input className="field" value={meta.ageRating} onChange={(e) => onChange({ ...meta, ageRating: e.target.value })} placeholder="Optional" />
        </Field>
      </div>

      <Field label="Summary">
        <textarea className="field min-h-[84px] resize-y leading-relaxed" value={meta.summary} onChange={(e) => onChange({ ...meta, summary: e.target.value })} placeholder="What's this one about?" />
      </Field>
      <Field label="Source page">
        <input className="field font-mono text-[11.5px]" value={meta.web} onChange={(e) => onChange({ ...meta, web: e.target.value })} />
      </Field>
    </div>
  );
}
