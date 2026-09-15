// Shared data shapes used by the content engine, background worker, offscreen engine and UI.

export type OutputFormat = "cbz" | "zip" | "pdf" | "folder";
export type Confidence = "high" | "medium" | "low" | "none";
export type ThemePreference = "system" | "light" | "dark";
export type MangaDirection = "auto" | "ltr" | "rtl";

/** Metadata for one comic, mapped 1:1 onto ComicInfo.xml and then onto Komik's library fields. */
export interface ComicMeta {
  title: string;
  series: string;
  /** Chapter / issue number as written ("15", "150.5"). */
  number: string;
  volume: string;
  /** The chapter's own name ("The Fight"), kept out of the file name by default. */
  chapterTitle: string;
  summary: string;
  year: string;
  month: string;
  day: string;
  writers: string[];
  artists: string[];
  publisher: string;
  genres: string[];
  tags: string[];
  language: string;
  /** "", "Yes", "YesAndRightToLeft" or "No". */
  manga: string;
  ageRating: string;
  status: string;
  web: string;
  site: string;
  coverUrl: string;
  /** Kind of number, drives how the title is written: Ch. for chapters, # for issues. */
  numberKind: "chapter" | "issue" | "episode" | "volume" | "none";
}

export interface PageRef {
  url: string;
  width?: number;
  height?: number;
  /** Where the page was found: an adapter, the DOM, a script, network traffic or the user's picker. */
  source: "adapter" | "img" | "background" | "link" | "script" | "network" | "picker" | "sequence" | "pagination";
  /** Page that referenced it: sent as Referer for hotlink-protected hosts. */
  referer?: string;
}

export interface ChapterRef {
  url: string;
  title: string;
  number: number | null;
  volume: number | null;
  date?: string;
}

export interface FileLink {
  url: string;
  name: string;
  ext: string;
  /** Text of the link or button that pointed at it. */
  label: string;
  size?: number;
}

export interface DetectResult {
  url: string;
  pageTitle: string;
  site: string;
  adapter: string;
  confidence: Confidence;
  pages: PageRef[];
  chapters: ChapterRef[];
  files: FileLink[];
  meta: ComicMeta;
  /** Reader pages that each show one image ("page 1 of 20" readers). */
  pagination: string[];
  /** Images still waiting to lazy-load when the scan ran. */
  lazyPending: number;
  scannedAt: number;
  isSeriesPage: boolean;
}

export type JobStatus = "queued" | "resolving" | "downloading" | "packing" | "saving" | "paused" | "done" | "error" | "cancelled";

export interface JobPage extends PageRef {
  index: number;
  done: boolean;
  bytes?: number;
  mime?: string;
  error?: string;
}

export interface Job {
  id: string;
  createdAt: number;
  updatedAt: number;
  status: JobStatus;
  format: OutputFormat;
  meta: ComicMeta;
  /** Page to open the chapter from when pages still have to be found (batch chapter downloads). */
  chapterUrl?: string;
  sourceUrl: string;
  tabId?: number;
  pages: JobPage[];
  coverIndex: number;
  /** For direct file downloads (a site's own CBZ/PDF): the file to fetch. */
  file?: FileLink;
  totalBytes: number;
  doneBytes: number;
  speedBps: number;
  error?: string;
  /** Relative path inside Downloads, or the custom folder, once saved. */
  savedPath?: string;
  downloadId?: number;
  batchId?: string;
  batchLabel?: string;
  warnings: string[];
}

export interface HistoryEntry {
  id: string;
  title: string;
  series: string;
  format: OutputFormat;
  pages: number;
  bytes: number;
  sourceUrl: string;
  savedPath: string;
  downloadId?: number;
  coverThumb?: string;
  tags: string[];
  finishedAt: number;
  meta: ComicMeta;
}

export interface SiteRule {
  id: string;
  /** Hostname it applies to ("example.com" also covers subdomains). */
  host: string;
  pageSelector: string;
  chapterSelector: string;
  titleSelector: string;
  seriesSelector: string;
  tagSelector: string;
  enabled: boolean;
}

export interface Settings {
  // Folder & naming
  folderMode: "downloads" | "custom";
  downloadsSubfolder: string;
  customFolderName: string;
  folderTemplate: string;
  fileTemplate: string;
  includeChapterTitle: boolean;
  padPages: number;
  askWhereToSave: boolean;
  // Output
  defaultFormat: OutputFormat;
  convertUnsupported: boolean;
  jpegQuality: number;
  coverFirst: boolean;
  minPageWidth: number;
  // Speed
  perHostConnections: number;
  globalConnections: number;
  parallelJobs: number;
  retries: number;
  timeoutSeconds: number;
  autoResume: boolean;
  // Metadata
  cleanTags: boolean;
  addSiteTag: boolean;
  maxTags: number;
  mangaDirection: MangaDirection;
  rememberSeriesEdits: boolean;
  // Page scanning
  autoScroll: boolean;
  floatingButton: boolean;
  showBadge: boolean;
  // Site downloads
  catchComicDownloads: boolean;
  embedIntoCaughtDownloads: boolean;
  // Appearance
  theme: ThemePreference;
  sounds: boolean;
  reducedMotion: boolean;
  notifyOnFinish: boolean;
  // Site rules
  siteRules: SiteRule[];
  onboarded: boolean;
}

export interface JobsSnapshot {
  jobs: Job[];
  active: number;
  queued: number;
  speedBps: number;
}
