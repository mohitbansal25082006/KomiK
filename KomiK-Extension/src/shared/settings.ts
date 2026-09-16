import type { Settings } from "./types";

export const DEFAULT_SETTINGS: Settings = {
  folderMode: "downloads",
  downloadsSubfolder: "KomiK",
  customFolderName: "",
  folderTemplate: "{series}",
  fileTemplate: "{title}",
  includeChapterTitle: false,
  padPages: 3,
  askWhereToSave: false,

  defaultFormat: "cbz",
  convertUnsupported: true,
  jpegQuality: 92,
  coverFirst: false,
  minPageWidth: 280,

  perHostConnections: 6,
  globalConnections: 16,
  parallelJobs: 2,
  retries: 4,
  timeoutSeconds: 45,
  autoResume: true,

  cleanTags: true,
  addSiteTag: false,
  maxTags: 60,
  mangaDirection: "auto",
  rememberSeriesEdits: true,

  autoScroll: true,
  floatingButton: true,
  showBadge: true,

  theme: "light",
  sounds: true,
  reducedMotion: false,
  notifyOnFinish: true,

  siteRules: [],
  onboarded: false
};

const KEY = "settings";

/** Clamps numbers into safe ranges and fills anything missing from older versions. */
export function normalizeSettings(raw: Partial<Settings> | undefined): Settings {
  const s: Settings = { ...DEFAULT_SETTINGS, ...(raw ?? {}) };
  const clamp = (v: number, min: number, max: number, fallback: number) =>
    Number.isFinite(v) ? Math.min(max, Math.max(min, Math.round(v))) : fallback;
  s.perHostConnections = clamp(s.perHostConnections, 1, 16, DEFAULT_SETTINGS.perHostConnections);
  s.globalConnections = clamp(s.globalConnections, 1, 32, DEFAULT_SETTINGS.globalConnections);
  s.parallelJobs = clamp(s.parallelJobs, 1, 6, DEFAULT_SETTINGS.parallelJobs);
  s.retries = clamp(s.retries, 0, 10, DEFAULT_SETTINGS.retries);
  s.timeoutSeconds = clamp(s.timeoutSeconds, 5, 300, DEFAULT_SETTINGS.timeoutSeconds);
  s.jpegQuality = clamp(s.jpegQuality, 50, 100, DEFAULT_SETTINGS.jpegQuality);
  s.padPages = clamp(s.padPages, 2, 5, DEFAULT_SETTINGS.padPages);
  s.maxTags = clamp(s.maxTags, 0, 100, DEFAULT_SETTINGS.maxTags);
  s.minPageWidth = clamp(s.minPageWidth, 0, 2000, DEFAULT_SETTINGS.minPageWidth);
  s.downloadsSubfolder = (s.downloadsSubfolder ?? "").replace(/[\\]+/g, "/").replace(/^\/+|\/+$/g, "");
  if (!Array.isArray(s.siteRules)) s.siteRules = [];
  return s;
}

export async function loadSettings(): Promise<Settings> {
  const data = await chrome.storage.local.get(KEY);
  return normalizeSettings(data[KEY] as Partial<Settings> | undefined);
}

export async function saveSettings(patch: Partial<Settings>): Promise<Settings> {
  const current = await loadSettings();
  const next = normalizeSettings({ ...current, ...patch });
  await chrome.storage.local.set({ [KEY]: next });
  return next;
}

export function onSettingsChanged(listener: (s: Settings) => void): () => void {
  const handler = (changes: Record<string, chrome.storage.StorageChange>, area: string) => {
    if (area === "local" && changes[KEY]) listener(normalizeSettings(changes[KEY].newValue as Partial<Settings>));
  };
  chrome.storage.onChanged.addListener(handler);
  return () => chrome.storage.onChanged.removeListener(handler);
}
