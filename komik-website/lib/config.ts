export const APP_CONFIG = {
  name: "Komik",
  version: "1.2.0",
  tagline: "The Modern Native Windows Comic & Manga Reader",
  oneLiner: "A fast, local-first Windows 11 reader built with WinUI 3 and .NET 8. Zero cloud sync, zero telemetry, zero accounts.",
  author: "Mohit Bansal",
  githubUser: "mohitbansal25082006",
  githubRepo: "KomiK",
  siteUrl: "https://komik-website-taupe.vercel.app",
  privacyUrl: "https://komik-website-taupe.vercel.app/privacy",
  repoUrl: "https://github.com/mohitbansal25082006/KomiK",
  releasesUrl: "https://github.com/mohitbansal25082006/KomiK/releases",
  issuesUrl: "https://github.com/mohitbansal25082006/KomiK/issues",
  downloadUrl: "https://github.com/mohitbansal25082006/KomiK/releases/latest/download/Komik-Setup.exe",
  installerName: "Komik-Setup.exe",
  installerSize: "~75 MB",
  systemRequirements: "Windows 10 version 1809+ or Windows 11 (64-bit)",
  supportedFormats: [
    { ext: ".CBZ", desc: "Comic Book ZIP", badge: "Universal" },
    { ext: ".CBR", desc: "RAR4 & RAR5 (SharpCompress)", badge: "Pure .NET" },
    { ext: ".CB7", desc: "7-Zip LZMA/LZMA2", badge: "High Compression" },
    { ext: ".PDF", desc: "Raster & Vector (Docnet/PDFium)", badge: "HQ Rendering" },
    { ext: ".ZIP", desc: "Standard ZIP Archives", badge: "Archive" },
    { ext: ".RAR", desc: "Standard RAR Archives", badge: "Archive" },
    { ext: ".7Z", desc: "Standard 7-Zip Archives", badge: "Archive" },
    { ext: "Folders", desc: "Raw Extracted Image Folders", badge: "Direct Read" },
  ],
} as const;

/** KomiK Downloader, the companion browser extension that ships alongside Komik 1.2.0. */
export const EXTENSION_CONFIG = {
  name: "KomiK Downloader",
  version: "1.0.0",
  pairsWith: "1.2.0",
  browsers: ["Chrome", "Edge", "Brave"],
  zipName: "komik-downloader-1.0.0.zip",
  zipUrl: "https://github.com/mohitbansal25082006/KomiK/releases/latest/download/komik-downloader-1.0.0.zip",
  zipSize: "~700 KB",
  /** Set once the Chrome Web Store listing is live; until then the site offers the zip. */
  chromeStoreUrl: "",
  shortcuts: [
    { keys: ["Alt", "K"], label: "Open KomiK on the current page" },
    { keys: ["Alt", "Shift", "K"], label: "Download this comic right away" },
  ],
} as const;
