import { AnimatePresence, MotionConfig, motion } from "framer-motion";
import { StrictMode, useEffect, useMemo, useState, type ReactNode } from "react";
import { createRoot } from "react-dom/client";
import "@/ui/styles.css";
import { EXTENSION_VERSION } from "@/manifest.mjs";
import { clearAllPages, clearHistory, estimateCacheBytes, kvDelete, kvGet, kvSet } from "@/shared/db";
import { planSave } from "@/shared/naming";
import { DEFAULT_SETTINGS, normalizeSettings } from "@/shared/settings";
import type { ComicMeta, Settings, SiteRule } from "@/shared/types";
import { emptyMeta, formatBytes, uid } from "@/shared/util";
import { CUSTOM_FOLDER_KEY } from "@/offscreen/save";
import { Burst, Crest, Stamp, Wordmark } from "@/ui/components/Brand";
import { Button, Caption, Field, Segmented, Select, Sticker, Toggle } from "@/ui/components/Controls";
import { Icon } from "@/ui/components/Icon";
import { useSettings, useTheme } from "@/ui/hooks";
import { setSoundEnabled, sfx } from "@/ui/sound";

const SAMPLE: ComicMeta = { ...emptyMeta(), title: "Starlight Courier Vol. 2 Ch. 15", series: "Starlight Courier", volume: "2", number: "15", numberKind: "chapter", year: "2024", site: "example-comics.com", writers: ["Ada Ink"], artists: ["Rio Pen"] };

const SECTIONS = [
  { id: "folder", n: "01", title: "Folder & names", icon: "folder" },
  { id: "output", n: "02", title: "Formats & pages", icon: "pages" },
  { id: "speed", n: "03", title: "Speed", icon: "bolt" },
  { id: "metadata", n: "04", title: "Details & tags", icon: "tag" },
  { id: "scanning", n: "05", title: "Page scanning", icon: "search" },
  { id: "rules", n: "06", title: "Site rules", icon: "cursor" },
  { id: "look", n: "07", title: "Look & feel", icon: "sparkle" },
  { id: "data", n: "08", title: "Data", icon: "shield" },
  { id: "about", n: "09", title: "Komik app", icon: "book" }
] as const;

function Chapter({ id, n, title, color, children, intro }: { id: string; n: string; title: string; color: string; children: ReactNode; intro?: ReactNode }) {
  return (
    <motion.section id={id} initial={{ opacity: 0, y: 24 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true, margin: "-60px" }} transition={{ type: "spring", stiffness: 260, damping: 26 }} className="scroll-mt-6">
      <div className="mb-3 flex items-end gap-3">
        <Caption color={color}>Chapter {n}</Caption>
        <h2 className="letter-outline text-[34px] leading-none">{title.toUpperCase()}</h2>
      </div>
      {intro && <p className="mb-3 max-w-[640px] text-[13.5px] leading-relaxed text-muted">{intro}</p>}
      <div className="panel relative overflow-hidden p-5">
        <div className="pointer-events-none absolute -right-10 -top-10 h-40 w-40 rounded-full bg-halftone-strong opacity-70" />
        <div className="relative space-y-4">{children}</div>
      </div>
    </motion.section>
  );
}

function Row({ label, hint, children }: { label: string; hint?: ReactNode; children: ReactNode }) {
  return (
    <div className="grid gap-2 sm:grid-cols-[230px_1fr] sm:items-start">
      <div>
        <div className="text-[13.5px] font-extrabold">{label}</div>
        {hint && <div className="mt-0.5 text-[12px] leading-snug text-muted">{hint}</div>}
      </div>
      <div className="min-w-0">{children}</div>
    </div>
  );
}

function Range({ value, min, max, onChange, suffix = "" }: { value: number; min: number; max: number; onChange: (v: number) => void; suffix?: string }) {
  return (
    <div className="flex items-center gap-3">
      <input type="range" min={min} max={max} value={value} onChange={(e) => onChange(Number(e.target.value))} className="h-2 flex-1 cursor-pointer accent-[#FF1F6D]" />
      <span className="sticker min-w-[64px] justify-center bg-yellow">{value}{suffix}</span>
    </div>
  );
}

function FolderPicker({ settings, update }: { settings: Settings; update: (p: Partial<Settings>) => Promise<void> }) {
  const [handle, setHandle] = useState<FileSystemDirectoryHandle | null>(null);
  const [permission, setPermission] = useState<PermissionState | "none">("none");
  const supported = "showDirectoryPicker" in window;

  const refresh = async () => {
    const h = await kvGet<FileSystemDirectoryHandle>(CUSTOM_FOLDER_KEY).catch(() => undefined);
    setHandle(h ?? null);
    if (h) {
      const q = (h as unknown as { queryPermission(o: { mode: string }): Promise<PermissionState> }).queryPermission;
      setPermission(await q.call(h, { mode: "readwrite" }).catch(() => "prompt" as PermissionState));
    } else setPermission("none");
  };
  useEffect(() => {
    void refresh();
  }, []);

  const choose = async () => {
    try {
      const picker = (window as unknown as { showDirectoryPicker(o: object): Promise<FileSystemDirectoryHandle> }).showDirectoryPicker;
      const h = await picker.call(window, { id: "komik-downloads", mode: "readwrite", startIn: "downloads" });
      await kvSet(CUSTOM_FOLDER_KEY, h);
      await update({ folderMode: "custom", customFolderName: h.name });
      sfx.pop();
      await refresh();
    } catch {
      /* cancelled */
    }
  };
  const reallow = async () => {
    if (!handle) return;
    const req = (handle as unknown as { requestPermission(o: { mode: string }): Promise<PermissionState> }).requestPermission;
    setPermission(await req.call(handle, { mode: "readwrite" }).catch(() => "denied" as PermissionState));
  };

  return (
    <div className="space-y-3">
      <Segmented
        value={settings.folderMode}
        onChange={(v) => (v === "custom" && !handle ? choose() : update({ folderMode: v }))}
        options={[
          { id: "downloads", label: "INSIDE DOWNLOADS" },
          { id: "custom", label: "ANY FOLDER" }
        ]}
      />
      {settings.folderMode === "downloads" ? (
        <Field label="Folder inside Downloads">
          <div className="flex items-center gap-2">
            <span className="font-mono text-[13px] text-muted">Downloads/</span>
            <input className="field font-mono" value={settings.downloadsSubfolder} onChange={(e) => update({ downloadsSubfolder: e.target.value })} placeholder="KomiK" />
          </div>
        </Field>
      ) : (
        <div className="panel-flat flex flex-wrap items-center gap-3 p-3">
          <Icon name="folder" size={22} />
          <div className="min-w-0 flex-1">
            <div className="truncate font-bold">{handle?.name ?? "No folder chosen"}</div>
            <div className="text-[12px] text-muted">
              {permission === "granted" ? "Access allowed. Comics save here directly." : permission === "none" ? "Pick a folder to save comics anywhere on your PC." : "Access needs to be allowed again (browsers ask after a restart). Until then comics go to Downloads."}
            </div>
          </div>
          {handle && permission !== "granted" && <Button size="sm" tone="cyan" onClick={reallow}>Allow access</Button>}
          <Button size="sm" tone="yellow" icon="folder" onClick={choose} disabled={!supported}>{handle ? "Change" : "Choose folder"}</Button>
          {handle && (
            <Button size="sm" icon="x" title="Forget this folder" onClick={async () => { await kvDelete(CUSTOM_FOLDER_KEY); await update({ folderMode: "downloads", customFolderName: "" }); await refresh(); }} />
          )}
        </div>
      )}
    </div>
  );
}

function RulesEditor({ rules, onChange }: { rules: SiteRule[]; onChange: (r: SiteRule[]) => void }) {
  const set = (id: string, patch: Partial<SiteRule>) => onChange(rules.map((r) => (r.id === id ? { ...r, ...patch } : r)));
  return (
    <div className="space-y-3">
      {rules.map((r) => (
        <div key={r.id} className="panel-flat space-y-2 p-3">
          <div className="flex items-center gap-2">
            <input className="field font-mono" placeholder="example.com" value={r.host} onChange={(e) => set(r.id, { host: e.target.value.trim() })} />
            <Toggle checked={r.enabled} onChange={(v) => set(r.id, { enabled: v })} label="On" />
            <Button size="sm" icon="trash" title="Delete rule" onClick={() => onChange(rules.filter((x) => x.id !== r.id))} />
          </div>
          <div className="grid gap-2 sm:grid-cols-2">
            <Field label="Page images (CSS selector)"><input className="field font-mono text-[12px]" placeholder=".reader img" value={r.pageSelector} onChange={(e) => set(r.id, { pageSelector: e.target.value })} /></Field>
            <Field label="Chapter links"><input className="field font-mono text-[12px]" placeholder=".chapters a" value={r.chapterSelector} onChange={(e) => set(r.id, { chapterSelector: e.target.value })} /></Field>
            <Field label="Title"><input className="field font-mono text-[12px]" placeholder="h1.title" value={r.titleSelector} onChange={(e) => set(r.id, { titleSelector: e.target.value })} /></Field>
            <Field label="Series"><input className="field font-mono text-[12px]" placeholder=".breadcrumb a:last-child" value={r.seriesSelector} onChange={(e) => set(r.id, { seriesSelector: e.target.value })} /></Field>
            <Field label="Tags" className="sm:col-span-2"><input className="field font-mono text-[12px]" placeholder=".genres a" value={r.tagSelector} onChange={(e) => set(r.id, { tagSelector: e.target.value })} /></Field>
          </div>
        </div>
      ))}
      <Button tone="cyan" icon="plus" onClick={() => onChange([...rules, { id: uid("rule-"), host: "", pageSelector: "", chapterSelector: "", titleSelector: "", seriesSelector: "", tagSelector: "", enabled: true }])}>Add site rule</Button>
    </div>
  );
}

function Welcome({ onDone }: { onDone: () => void }) {
  const steps = [
    { n: "1", title: "OPEN A COMIC", text: "Go to any comic, manga or webtoon chapter, or a series page with a chapter list.", color: "#00C2FF", icon: "globe" },
    { n: "2", title: "HIT KOMIK", text: "Click the crest in your toolbar (Alt+K). Check the pages, chapters, title and tags.", color: "#FFD700", icon: "download" },
    { n: "3", title: "READ IN KOMIK", text: "In the Komik app, add Downloads\\KomiK as a watched folder. New comics appear with details and tags.", color: "#FF1F6D", icon: "book" }
  ];
  return (
    <motion.div initial={{ opacity: 0, scale: 0.96 }} animate={{ opacity: 1, scale: 1 }} className="panel relative mb-10 overflow-hidden bg-yellow p-6 text-gutter">
      <div className="absolute inset-0 bg-halftone-yellow" />
      <div className="absolute -right-24 -top-24 h-72 w-72 rounded-full bg-sunburst opacity-80 animate-spin-slow" />
      <div className="relative">
        <Caption color="#fff">Issue #1 · Welcome</Caption>
        <h1 className="letter-outline mt-3 text-[52px] leading-[0.95]">YOUR COMICS.<br />ANY SITE. STRAIGHT TO KOMIK.</h1>
        <p className="mt-3 max-w-[560px] text-[15px] font-semibold leading-relaxed">KomiK Downloader saves comics as CBZ files named by their title, with the series, numbers, credits, summary and the site's tags built in, so the Komik app files them for you.</p>
        <div className="mt-6 grid gap-4 md:grid-cols-3">
          {steps.map((s, i) => (
            <motion.div key={s.n} initial={{ y: 30, rotate: i % 2 ? 2 : -2, opacity: 0 }} animate={{ y: 0, rotate: i === 1 ? 1 : -1, opacity: 1 }} transition={{ delay: 0.15 + i * 0.12, type: "spring", stiffness: 300, damping: 20 }} className="panel relative p-4 text-text">
              <Burst className="absolute -left-4 -top-5 h-12 w-12" fill={s.color}><span className="letter text-[22px] text-gutter">{s.n}</span></Burst>
              <div className="ml-6 flex items-center gap-2"><Icon name={s.icon} size={18} /><h3 className="letter text-[22px] leading-none">{s.title}</h3></div>
              <p className="mt-2 text-[13px] leading-snug text-muted">{s.text}</p>
            </motion.div>
          ))}
        </div>
        <div className="mt-6 flex flex-wrap items-center gap-3">
          <Button tone="magenta" icon="check" onClick={onDone}>Let's go!</Button>
          <span className="text-[12.5px] font-bold">Everything below saves instantly. Defaults are ready to use.</span>
        </div>
      </div>
    </motion.div>
  );
}

function Options() {
  const [settings, update, ready] = useSettings();
  useTheme(settings, ready);
  useEffect(() => setSoundEnabled(settings.sounds), [settings.sounds]);
  const [welcome, setWelcome] = useState(location.hash === "#welcome");
  const [active, setActive] = useState<string>("folder");
  const [cache, setCache] = useState(0);
  const [stamp, setStamp] = useState<string | null>(null);
  const [importError, setImportError] = useState("");

  useEffect(() => {
    if (ready && !settings.onboarded && !welcome) setWelcome(true);
  }, [ready]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    estimateCacheBytes().then(setCache);
    const observer = new IntersectionObserver((entries) => {
      const visible = entries.filter((e) => e.isIntersecting).sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top)[0];
      if (visible) setActive(visible.target.id);
    }, { rootMargin: "-20% 0px -65% 0px" });
    SECTIONS.forEach((s) => {
      const el = document.getElementById(s.id);
      if (el) observer.observe(el);
    });
    return () => observer.disconnect();
  }, [ready]);

  const preview = useMemo(() => planSave(SAMPLE, settings.defaultFormat, settings, 24), [settings]);

  const exportSettings = () => {
    const blob = new Blob([JSON.stringify({ app: "KomiK Downloader", version: EXTENSION_VERSION, settings }, null, 2)], { type: "application/json" });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = "komik-downloader-settings.json";
    a.click();
    setTimeout(() => URL.revokeObjectURL(a.href), 5000);
  };
  const importSettings = async (file: File) => {
    try {
      const data = JSON.parse(await file.text());
      const next = normalizeSettings({ ...(data.settings ?? data), onboarded: true });
      await update(next);
      setImportError("");
      setStamp("LOADED!");
    } catch {
      setImportError("That file isn't a KomiK Downloader settings file.");
    }
  };

  const finishWelcome = async () => {
    await update({ onboarded: true });
    setWelcome(false);
    history.replaceState(null, "", location.pathname);
    sfx.whoosh();
  };

  return (
    <MotionConfig reducedMotion={settings.reducedMotion ? "always" : "user"}>
      {stamp && <Stamp text={stamp} color="#2FD17A" onDone={() => setStamp(null)} />}
      <div className="min-h-screen bg-ink bg-halftone">
        <header className="sticky top-0 z-20 border-b-[3px] border-gutter bg-panel/95 backdrop-blur">
          <div className="mx-auto flex max-w-[1180px] items-center gap-3 px-6 py-3">
            <Wordmark sub={`Downloader · v${EXTENSION_VERSION}`} />
            <Sticker color="#2FD17A" rotate={-2} className="ml-2 hidden md:inline-flex">SAVES INSTANTLY</Sticker>
            <div className="ml-auto flex gap-2">
              <Button size="sm" icon={document.documentElement.classList.contains("dark") ? "sun" : "moon"} onClick={() => update({ theme: document.documentElement.classList.contains("dark") ? "light" : "dark" })}>Theme</Button>
              <Button size="sm" icon="keyboard" onClick={() => chrome.tabs.create({ url: "chrome://extensions/shortcuts" })}>Shortcuts</Button>
            </div>
          </div>
        </header>

        <div className="mx-auto grid max-w-[1180px] gap-8 px-6 py-8 lg:grid-cols-[220px_1fr]">
          <nav className="hidden lg:block">
            <div className="sticky top-24 space-y-1.5">
              {SECTIONS.map((s) => (
                <a
                  key={s.id}
                  href={`#${s.id}`}
                  onClick={(e) => {
                    e.preventDefault();
                    document.getElementById(s.id)?.scrollIntoView({ behavior: settings.reducedMotion ? "auto" : "smooth" });
                  }}
                  className={`relative flex items-center gap-2 rounded-md border-[2.5px] px-2.5 py-1.5 transition-all ${active === s.id ? "border-gutter bg-yellow text-gutter shadow-comic-sm" : "border-transparent hover:border-gutter hover:bg-panel"}`}
                >
                  <span className="font-bangers text-[15px] text-muted">{s.n}</span>
                  <Icon name={s.icon} size={15} />
                  <span className="text-[13px] font-extrabold">{s.title}</span>
                </a>
              ))}
            </div>
          </nav>

          <main className="min-w-0 space-y-12">
            <AnimatePresence>{welcome && <Welcome onDone={finishWelcome} />}</AnimatePresence>

            <Chapter id="folder" n="01" title="Folder & names" color="#FFD700" intro="Where comics go and what they're called. The title becomes the file name, so Komik shows the same name in your library.">
              <Row label="Save comics to" hint="Browsers only let extensions save inside Downloads, unless you pick a folder.">
                <FolderPicker settings={settings} update={update} />
              </Row>
              <Row label="Folder per comic" hint={<>Tokens: <code>{"{series} {title} {volume} {chapter} {year} {site} {author}"}</code></>}>
                <input className="field font-mono" value={settings.folderTemplate} onChange={(e) => update({ folderTemplate: e.target.value })} placeholder="{series}" />
              </Row>
              <Row label="File name" hint="Default {title} is the name Komik will show.">
                <input className="field font-mono" value={settings.fileTemplate} onChange={(e) => update({ fileTemplate: e.target.value })} placeholder="{title}" />
              </Row>
              <Row label="Preview">
                <div className="panel-flat flex items-center gap-2 bg-cyan/15 p-3 font-mono text-[13px]">
                  <Icon name="file" size={16} />
                  <span className="truncate">{settings.folderMode === "custom" ? `${settings.customFolderName || "Folder"}/` : "Downloads/"}{preview.relativePath}</span>
                </div>
              </Row>
              <Row label="Chapter names">
                <Toggle checked={settings.includeChapterTitle} onChange={(v) => update({ includeChapterTitle: v })} label="Add the chapter's own name to the title" hint={'"Starlight Courier Ch. 15 - The Long Night"'} />
              </Row>
              <Row label="Ask every time">
                <Toggle checked={settings.askWhereToSave} onChange={(v) => update({ askWhereToSave: v })} label="Show the browser's Save As dialog" hint="Off keeps downloads fast and automatic." />
              </Row>
            </Chapter>

            <Chapter id="output" n="02" title="Formats & pages" color="#00C2FF">
              <Row label="Default format" hint="CBZ is best for Komik: pages stay untouched and the details ride along inside.">
                <Segmented value={settings.defaultFormat} onChange={(v) => update({ defaultFormat: v })} options={[{ id: "cbz", label: "CBZ" }, { id: "zip", label: "ZIP" }, { id: "pdf", label: "PDF" }, { id: "folder", label: "FOLDER" }]} />
              </Row>
              <Row label="Unsupported images" hint="Komik reads JPG, PNG, WebP, GIF, BMP and TIFF.">
                <Toggle checked={settings.convertUnsupported} onChange={(v) => update({ convertUnsupported: v })} label="Convert AVIF / JPEG XL / HEIC pages to JPEG" />
              </Row>
              <Row label="JPEG quality" hint="Only used when converting or building PDFs.">
                <Range value={settings.jpegQuality} min={60} max={100} onChange={(v) => update({ jpegQuality: v })} suffix="%" />
              </Row>
              <Row label="Page numbering" hint="001.jpg, 002.jpg… (grows automatically for long chapters).">
                <Range value={settings.padPages} min={2} max={5} onChange={(v) => update({ padPages: v })} suffix=" digits" />
              </Row>
              <Row label="Cover">
                <Toggle checked={settings.coverFirst} onChange={(v) => update({ coverFirst: v })} label="Move the chosen cover to the front" hint="Komik uses the first page as the library cover." />
              </Row>
            </Chapter>

            <Chapter id="speed" n="03" title="Speed" color="#FF7A00" intro="KomiK downloads many pages at once and backs off automatically when a site asks it to slow down.">
              <Row label="Connections per site"><Range value={settings.perHostConnections} min={1} max={16} onChange={(v) => update({ perHostConnections: v })} /></Row>
              <Row label="Connections in total"><Range value={settings.globalConnections} min={1} max={32} onChange={(v) => update({ globalConnections: v })} /></Row>
              <Row label="Comics at the same time"><Range value={settings.parallelJobs} min={1} max={6} onChange={(v) => update({ parallelJobs: v })} /></Row>
              <Row label="Retries per page"><Range value={settings.retries} min={0} max={10} onChange={(v) => update({ retries: v })} /></Row>
              <Row label="Page timeout"><Range value={settings.timeoutSeconds} min={10} max={180} onChange={(v) => update({ timeoutSeconds: v })} suffix="s" /></Row>
              <Row label="After a restart">
                <Toggle checked={settings.autoResume} onChange={(v) => update({ autoResume: v })} label="Resume unfinished downloads automatically" hint="Pages already downloaded are kept, so nothing starts over." />
              </Row>
            </Chapter>

            <Chapter id="metadata" n="04" title="Details & tags" color="#2FD17A" intro="What goes into ComicInfo.xml, the details file Komik 1.2.0 reads when it adds a comic.">
              <Row label="Tag clean-up">
                <Toggle checked={settings.cleanTags} onChange={(v) => update({ cleanTags: v })} label="Tidy the site's tags" hint={'Drops menu words like "Home" or "Read now", fixes capitals and merges duplicates.'} />
              </Row>
              <Row label="Maximum tags" hint="Genres and tags together. 0 means no limit."><Range value={settings.maxTags} min={0} max={60} onChange={(v) => update({ maxTags: v })} /></Row>
              <Row label="Site tag"><Toggle checked={settings.addSiteTag} onChange={(v) => update({ addSiteTag: v })} label="Add the website's name as a tag" /></Row>
              <Row label="Reading direction">
                <Select value={settings.mangaDirection} onChange={(v) => update({ mangaDirection: v })} options={[{ id: "auto", label: "Detect from the site (manga → right to left)" }, { id: "rtl", label: "Always right to left" }, { id: "ltr", label: "Always left to right" }]} />
              </Row>
              <Row label="Series memory">
                <Toggle checked={settings.rememberSeriesEdits} onChange={(v) => update({ rememberSeriesEdits: v })} label="Remember my edits for each series" hint="Fix the tags or credits once; the next chapters fill in the same way." />
              </Row>
            </Chapter>

            <Chapter id="scanning" n="05" title="Page scanning" color="#A78BFA">
              <Row label="Lazy readers"><Toggle checked={settings.autoScroll} onChange={(v) => update({ autoScroll: v })} label="Scroll through pages that load images as you scroll" hint="Your place on the page is restored afterwards." /></Row>
              <Row label="Smallest page width" hint="Images narrower than this are treated as icons or ads."><Range value={settings.minPageWidth} min={0} max={900} onChange={(v) => update({ minPageWidth: v })} suffix="px" /></Row>
              <Row label="Floating button"><Toggle checked={settings.floatingButton} onChange={(v) => update({ floatingButton: v })} label="Show the KomiK crest on pages with a comic" /></Row>
              <Row label="Toolbar badge"><Toggle checked={settings.showBadge} onChange={(v) => update({ showBadge: v })} label="Show how many pages were spotted" /></Row>
              <Row label="Site download buttons" hint="When a site gives you its own CBZ / CBR / PDF.">
                <div>
                  <Toggle checked={settings.catchComicDownloads} onChange={(v) => update({ catchComicDownloads: v })} label="Put comic files into the KomiK folder with a proper name" />
                  <Toggle checked={settings.embedIntoCaughtDownloads} onChange={(v) => update({ embedIntoCaughtDownloads: v })} label="Also add details to CBZ, ZIP and PDF files by downloading them through KomiK" hint="Leave off for links that only work once." />
                </div>
              </Row>
            </Chapter>

            <Chapter id="rules" n="06" title="Site rules" color="#FF1F6D" intro="KomiK finds pages on its own. For a site with an unusual layout, point it at the right elements with CSS selectors.">
              <RulesEditor rules={settings.siteRules} onChange={(r) => update({ siteRules: r })} />
            </Chapter>

            <Chapter id="look" n="07" title="Look & feel" color="#FFD700">
              <Row label="Theme"><Segmented value={settings.theme} onChange={(v) => update({ theme: v })} options={[{ id: "system", label: "WINDOWS" }, { id: "light", label: "PAPER" }, { id: "dark", label: "INK" }]} /></Row>
              <Row label="Sound effects"><Toggle checked={settings.sounds} onChange={(v) => { update({ sounds: v }); if (v) sfx.ding(); }} label="Pops, whooshes and dings" /></Row>
              <Row label="Motion"><Toggle checked={settings.reducedMotion} onChange={(v) => update({ reducedMotion: v })} label="Reduce animations" /></Row>
              <Row label="Notifications"><Toggle checked={settings.notifyOnFinish} onChange={(v) => update({ notifyOnFinish: v })} label="Tell me when a comic finishes" /></Row>
            </Chapter>

            <Chapter id="data" n="08" title="Data" color="#00C2FF" intro="Everything stays in your browser. KomiK Downloader has no account, no server and no tracking.">
              <Row label="Page cache" hint="Pages kept so interrupted downloads can resume.">
                <div className="flex flex-wrap items-center gap-2">
                  <Sticker color="#FFFDF6">{formatBytes(cache)} used</Sticker>
                  <Button size="sm" icon="trash" onClick={async () => { await clearAllPages(); setCache(await estimateCacheBytes()); setStamp("CLEARED!"); }}>Clear cache</Button>
                </div>
              </Row>
              <Row label="History" hint="Clearing never deletes downloaded files.">
                <Button size="sm" icon="trash" onClick={async () => { await clearHistory(); setStamp("CLEARED!"); }}>Clear history</Button>
              </Row>
              <Row label="Settings file">
                <div className="flex flex-wrap items-center gap-2">
                  <Button size="sm" icon="download" onClick={exportSettings}>Export</Button>
                  <label className="btn btn-sm cursor-pointer">
                    <Icon name="upload" size={15} /> Import
                    <input type="file" accept="application/json,.json" className="hidden" onChange={(e) => e.target.files?.[0] && importSettings(e.target.files[0])} />
                  </label>
                  <Button size="sm" tone="magenta" icon="retry" onClick={async () => { await update({ ...DEFAULT_SETTINGS, onboarded: true }); setStamp("RESET!"); }}>Reset all</Button>
                  {importError && <span className="text-[12px] font-bold text-magenta">{importError}</span>}
                </div>
              </Row>
            </Chapter>

            <Chapter id="about" n="09" title="Komik app" color="#FF7A00">
              <div className="flex flex-wrap items-center gap-5">
                <Burst className="h-28 w-28" fill="#FFD700" spin><Crest size={58} /></Burst>
                <div className="min-w-0 flex-1 space-y-2 text-[13.5px] leading-relaxed">
                  <p><b>KomiK Downloader {EXTENSION_VERSION}</b> works best with the <b>Komik 1.2.0</b> Windows app, which reads the title, series, issue, credits, summary, dates and tags inside every CBZ this extension saves.</p>
                  <ol className="list-inside list-decimal space-y-1 text-muted">
                    <li>In Komik, open <b>Settings → Watched library folders → Add Folder</b> (or <b>ADD → Add Folder</b> in the library).</li>
                    <li>Add <code className="rounded bg-panel-card px-1">Downloads\{settings.downloadsSubfolder || "KomiK"}</code>{settings.folderMode === "custom" ? " or your custom folder" : ""}.</li>
                    <li>New downloads show up in the library by themselves, fully tagged.</li>
                  </ol>
                  <div className="flex flex-wrap gap-2 pt-1">
                    <Button size="sm" tone="yellow" icon="download" onClick={() => chrome.tabs.create({ url: "https://github.com/mohitbansal25082006/KomiK/releases/latest" })}>Get Komik</Button>
                    <Button size="sm" icon="globe" onClick={() => chrome.tabs.create({ url: "https://komik-website-taupe.vercel.app/" })}>Komik website</Button>
                    <Button size="sm" icon="folder" onClick={() => chrome.downloads.showDefaultFolder()}>Open Downloads</Button>
                  </div>
                </div>
              </div>
              <p className="border-t-2 border-dashed border-gutter/30 pt-3 text-[12px] leading-relaxed text-muted">
                Please download only comics you have the right to save. KomiK Downloader saves what your browser can already show you; it doesn't bypass paywalls, logins or DRM.
              </p>
            </Chapter>

            <footer className="pb-10 text-center">
              <span className="letter-outline text-[40px]">THE END…</span>
              <p className="text-[12px] font-bold text-muted">…of the settings. Happy reading!</p>
            </footer>
          </main>
        </div>
      </div>
    </MotionConfig>
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <Options />
  </StrictMode>
);
