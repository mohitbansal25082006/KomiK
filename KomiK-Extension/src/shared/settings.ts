import type { Settings } from "./types";

/**
 * 1 → 2: the tag limit stopped being a small fixed number. Settings saved with the old default (30 or
 * 60) are brought forward to "keep every tag", which is what those users expected all along.
 */
export const SETTINGS_VERSION = 2;

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
  // 0 = keep every tag the site lists. Gallery pages often print 40-80 of them and Komik shows them all.
  maxTags: 0,
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
  onboarded: false,
  settingsVersion: SETTINGS_VERSION
};

const KEY = "settings";

/** Clamps numbers into safe ranges, fills anything missing from older versions and drops keys no version uses. */
export function normalizeSettings(raw: Partial<Settings> | undefined): Settings {
  const merged: Record<string, unknown> = { ...DEFAULT_SETTINGS, ...(raw ?? {}) };
  const s = Object.fromEntries(Object.keys(DEFAULT_SETTINGS).map((key) => [key, merged[key]])) as unknown as Settings;
  const clamp = (v: number, min: number, max: number, fallback: number) =>
    Number.isFinite(v) ? Math.min(max, Math.max(min, Math.round(v))) : fallback;
  s.perHostConnections = clamp(s.perHostConnections, 1, 16, DEFAULT_SETTINGS.perHostConnections);
  s.globalConnections = clamp(s.globalConnections, 1, 32, DEFAULT_SETTINGS.globalConnections);
  s.parallelJobs = clamp(s.parallelJobs, 1, 6, DEFAULT_SETTINGS.parallelJobs);
  s.retries = clamp(s.retries, 0, 10, DEFAULT_SETTINGS.retries);
  s.timeoutSeconds = clamp(s.timeoutSeconds, 5, 300, DEFAULT_SETTINGS.timeoutSeconds);
  s.jpegQuality = clamp(s.jpegQuality, 50, 100, DEFAULT_SETTINGS.jpegQuality);
  s.padPages = clamp(s.padPages, 2, 5, DEFAULT_SETTINGS.padPages);
  // Old saved settings kept a small tag cap (the 30 or 60 that used to be the default), which quietly cut
  // long gallery tag lists short. Those are brought up to "no limit"; a number the user chose is kept.
  const version = Number(raw?.settingsVersion ?? 1);
  if (version < 2 && (s.maxTags === 30 || s.maxTags === 60)) s.maxTags = DEFAULT_SETTINGS.maxTags;
  s.settingsVersion = SETTINGS_VERSION;
  s.maxTags = clamp(s.maxTags, 0, 300, DEFAULT_SETTINGS.maxTags);
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
