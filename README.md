<!-- ═══════════════════════════════════════════════════════════════════════
     KOMIK — README
     Animated artwork lives in komik-website/public/readme/ (pure SVG + CSS,
     no external services required). Honors prefers-reduced-motion.
     ═══════════════════════════════════════════════════════════════════════ -->

<div align="center">

<a href="#-download--install">
  <img src="komik-website/public/readme/banner.svg" alt="KOMIK: Your comics. Your PC. Zero cloud." width="100%" />
</a>

<br />

<a href="https://github.com/mohitbansal25082006/KomiK/releases/latest/download/Komik-Setup.exe"><img src="https://img.shields.io/badge/⬇_DOWNLOAD_FOR_WINDOWS-~60_MB-FFD700?style=for-the-badge&labelColor=0A0A0F" alt="Download for Windows" height="42" /></a>

<br /><br />

<img src="https://img.shields.io/badge/version-1.1.0-FF1F6D?style=flat-square&labelColor=0A0A0F" alt="Version 1.1.0" />
<img src="https://img.shields.io/badge/Windows-10_%7C_11-00C2FF?style=flat-square&logo=windows11&logoColor=white&labelColor=0A0A0F" alt="Windows 10 and 11" />
<img src="https://img.shields.io/badge/.NET-8-FFD700?style=flat-square&logo=dotnet&logoColor=white&labelColor=0A0A0F" alt=".NET 8" />
<img src="https://img.shields.io/badge/UI-WinUI_3-FF1F6D?style=flat-square&labelColor=0A0A0F" alt="WinUI 3" />
<img src="https://img.shields.io/badge/telemetry-none-00C2FF?style=flat-square&labelColor=0A0A0F" alt="No telemetry" />
<img src="https://img.shields.io/badge/license-MIT-FFD700?style=flat-square&labelColor=0A0A0F" alt="MIT License" />

<br /><br />

**Komik** is a lightweight, high-performance, **local-only** Windows desktop reader for comics, manga and webtoons,<br />
built with **WinUI 3 (Windows App SDK)** and **C# / .NET 8** and designed around Windows 11 Fluent Design.

<sub>

**[Screenshots](#-see-it-in-action)** · **[Features](#-the-reading-engine)** · **[What's new](#-brand-new-in-110)** · **[Formats](#-every-format-zero-codecs)** · **[Install](#-download--install)** · **[Shortcuts](#-keyboard-first-reading)** · **[Privacy](#-your-comics-belong-on-your-pc)** · **[Architecture](#-under-the-hood)** · **[Build](#-build-it-yourself)** · **[Website](#-the-komik-website)**

</sub>

<img src="komik-website/public/readme/stats.svg" alt="8 formats · 6 parallel decoders · 6 LUT presets · 0 network calls · 0 accounts · 27 automated test suites" width="100%" />

</div>

> [!NOTE]
> **Privacy & offline architecture.** Komik is strictly offline and local-first. There is **no cloud sync, no account system, no telemetry or tracking, and zero external network calls anywhere in the application**. All comic metadata, reading progress, cover thumbnails, bookmarks and preferences stay on your own machine.

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-see-it-in-action"></a>
<img src="komik-website/public/readme/section-try-it.svg" alt="Read the demo comic" width="100%" />

### 🖥️ The Komik desktop app

<div align="center">

<img src="komik-website/public/readme/app-reader.jpg" alt="Komik reader in two-page spread mode with the floating toolbar and page scrubber" width="100%" />

<sub><b>The reader.</b> A two-page spread with the floating Fluent toolbar and the page scrubber. The chrome auto-hides while you read.</sub>

<br /><br />

<table>
  <tr>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/app-library.jpg" alt="Komik library grid with cover thumbnails and filter chips" width="100%" /><br />
      <sub><b>Library.</b> Cover grid, filter chips, and the one-button <b>ADD</b> menu, <b>Rescan</b>, <b>Convert</b>, <b>Stats</b>, <b>Duplicates</b> and <b>Series &amp; Volumes</b>.</sub>
    </td>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/app-webtoon.jpg" alt="Komik webtoon continuous vertical scroll mode" width="100%" /><br />
      <sub><b>Webtoon mode.</b> Continuous vertical scrolling powered by the 6-worker parallel decoder.</sub>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/app-series.jpg" alt="Komik Series and Volumes screen" width="100%" /><br />
      <sub><b>Series &amp; Volumes.</b> Issues grouped automatically when titles match 90% or more, with one-click <b>Read Next</b>.</sub>
    </td>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/app-stats.jpg" alt="Komik Reading Insights and Statistics dashboard" width="100%" /><br />
      <sub><b>Reading Insights.</b> Time read, pages, streaks, Komik Wrapped highlights and your Top 20, all computed locally.</sub>
    </td>
  </tr>
</table>

<sub>The screenshots show <b>Cyberpunk Chronicles #01</b>, an original 12-page demo comic made for the Komik website and packed into real <code>.cbz</code> files.</sub>

</div>

<details>
<summary><b>🌐 Peek at the interactive website (click to expand)</b></summary>
<br />

The [Komik website](#-the-komik-website) contains a fully working replica of the reader. Visitors can read the whole demo comic in the browser, with spreads, manga RTL, webtoon mode, color presets, bookmarks, dialogue search and full screen.

<div align="center">
<img src="komik-website/public/readme/screenshot-hero.jpg" alt="Komik website hero" width="100%" />
<table>
  <tr>
    <td width="50%"><img src="komik-website/public/readme/screenshot-reader.jpg" alt="Website reader mockup in full screen" width="100%" /></td>
    <td width="50%"><img src="komik-website/public/readme/screenshot-formats.jpg" alt="Collectible format cards" width="100%" /></td>
  </tr>
  <tr>
    <td width="50%"><img src="komik-website/public/readme/screenshot-engine.jpg" alt="Pinned reading engine comic strip" width="100%" /></td>
    <td width="50%"><img src="komik-website/public/readme/screenshot-new.jpg" alt="New in 1.1.0 special edition" width="100%" /></td>
  </tr>
</table>
<img src="komik-website/public/readme/screenshot-mobile-hero.jpg" alt="Website on mobile" width="32%" />
&nbsp;
<img src="komik-website/public/readme/screenshot-mobile-reader.jpg" alt="Website reader on mobile" width="32%" />
</div>
</details>

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-the-reading-engine"></a>
<img src="komik-website/public/readme/section-features.svg" alt="The Reading Engine" width="100%" />

<table>
<tr>
<td width="50%" valign="top">

#### 📖 High-performance reading canvas
- **Fit modes:** Fit Width `W`, Fit Height `H`, Actual Size `A` (1:1 with drag panning)
- **Zoom:** `Ctrl` `+` / `Ctrl` `-` or mouse wheel, reset with `Ctrl` `0`
- **Two-page spreads** `D` with cover isolation and correct physical-book pairing
- **Western LTR ↔ Manga RTL** `Ctrl` `R` flips page order, pairing *and* arrow keys
- **Natural page ordering**, so `page2` comes before `page10`
- Full-screen reading `F11` with auto-hiding chrome
- Smooth animated scroll back to the top on every page flip

</td>
<td width="50%" valign="top">

#### ⚡ Parallel webtoon engine
- Continuous vertical mode `V`
- A **6-worker pool** (`SemaphoreSlim(6)`) decodes high-resolution pages off the UI thread
- **Outward priority** from the active viewport, so pages are never blank
- Smooth mouse wheel & keyboard (`↑` `↓` `PgUp` `PgDn`) navigation
- Animated jumps (`ScrollToWebtoonPage`) from the scrubber and next/previous buttons

</td>
</tr>
<tr>
<td width="50%" valign="top">

#### 🔖 Progress, bookmarks & scrubber
- Automatic progress tracking in SQLite (last page + completion)
- A non-intrusive **"Resumed at page X"** toast when you reopen a comic
- **Visual scrubber** with page numbers and thumbnail previews
- **Bookmarks with notes** via `Ctrl` `D` / `Ctrl` `B`

</td>
<td width="50%" valign="top">

#### 🌙 Hardware LUT color presets
| Preset | Effect |
| :-- | :-- |
| **Original** | Natural scan colors |
| **Night Mode** | Amber warmth, dimmed background |
| **Sepia Tone** | Warm parchment paper |
| **High Contrast** | Sharper ink, deeper blacks |
| **Grayscale** | Pure monochrome |
| **Inverted** | Dark-mode inversion |

Plus **Brightness** (-100…+100), **Contrast** (0.5…2.0) and **Warmth** (-100…+100) sliders on a 256-entry lookup table.

</td>
</tr>
<tr>
<td width="50%" valign="top">

#### 🗂️ A library that files itself
- **Watched folders** with recursive indexing into local SQLite
- **Cover grid** and **detailed list** views
- Instant search, plus filter chips: All · Continue Reading · Favorites · Completed · Unread · Tags
- Sort by title (A–Z / Z–A), recently read, date added, page count or file size
- Disk-backed thumbnail cache for near-instant covers
- **Non-destructive:** removing a library entry never deletes your file

</td>
<td width="50%" valign="top">

#### ⚙️ Settings, details & shell integration
- Theme override: System · Light · Dark (with Mica)
- Reading & library defaults, monitored folders, thumbnail cache tools
- **Comic Details** editor: title, series, issue #, writers, artists, publisher, release date, summary
- **File associations**, so double-clicking a comic in Explorer opens it in Komik
- Command-line launch: `Komik.exe "<path-to-comic>"`

</td>
</tr>
</table>

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-brand-new-in-110"></a>
<img src="komik-website/public/readme/section-new.svg" alt="Brand-new in 1.1.0" width="100%" />

| # | Feature | What it does |
| :-: | :-- | :-- |
| 1 | 📚 **Series & Volumes screen** | A dedicated screen that auto-groups runs and tankōbon volumes at **≥ 90% title similarity** (Levenshtein + normalization). It has its own search, sort, **+ New Series** builder and **+ Add Comics**. |
| 2 | ⚡ **Parallel Webtoon engine** | A 6-worker concurrent decoder with outward viewport priority and a fix for the old scroll-reset loop. |
| 3 | 📊 **Reading Stats & Komik Wrapped** | Lifetime time read, pages, completed comics, current and longest streak, 14-day velocity and the Top 20 comics and series. |
| 4 | 👥 **Duplicate Manager** | Finds cross-format copies (`.cbz` vs `.pdf` vs `.cbr`) by size, page count and **≥ 95%** title similarity. **Keep All** or remove from disk. |
| 5 | 🔍 **Offline OCR** | Windows' built-in `Windows.Media.Ocr` powers dialogue search with jump-to-page and a selectable text overlay. No cloud APIs. |
| 6 | 🗜️ **Archive → CBZ converter** | Lossless repacking of CBR / CB7 / ZIP / RAR / 7Z and image folders, plus PDF rasterization, into clean zero-padded CBZ files. |
| 7 | 🧭 **Responsive top bar** | A single accent **ADD** dropdown (Add Folder / Add Comic), horizontal mouse-wheel scrolling and a layout that adapts to any window width. |
| 8 | 🎨 **Brand crest** | One vector logo shared by the app icon, shell assets, settings page, installer and website. |
| 9 | 🪟 **Window memory** | Restores size, position and maximized state on every launch. |
| 10 | 💾 **Portable backups** | A `.komikbackup` JSON file with history, bookmarks, notes, favorites, tags and settings, imported without conflicts. |

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-every-format-zero-codecs"></a>
<img src="komik-website/public/readme/section-formats.svg" alt="Every format. Zero codecs." width="100%" />

| Format | Type | Engine | Notes |
| :-- | :-- | :-- | :-- |
| 🟨 **`.cbz` / `.zip`** | Comic Book ZIP | `System.IO.Compression` | Streams pages straight from the archive |
| 🟥 **`.cbr` / `.rar`** | RAR4 & RAR5 | SharpCompress (managed) | No `unrar.dll`; password-locked files are rejected cleanly |
| 🟦 **`.cb7` / `.7z`** | 7-Zip LZMA / LZMA2 | SharpCompress (managed) | Solid archives, no 7z CLI needed |
| 🟧 **`.pdf`** | Portable Document | Docnet.Core (PDFium) | High-quality rasterization |
| ⬜ **Image folders** | JPEG · PNG · WEBP · BMP · GIF | Natural sort comparer | Point Komik at any folder |

> [!TIP]
> **Zero external dependencies.** RAR and 7-Zip decoding are 100% managed .NET, with no command-line tools or native DLLs to install.

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-download--install"></a>
<img src="komik-website/public/readme/section-install.svg" alt="Download & Install" width="100%" />

<table>
<tr>
<td width="55%" valign="top">

### 🚀 In four panels

1. **Download** [`Komik-Setup.exe`](https://github.com/mohitbansal25082006/KomiK/releases/latest/download/Komik-Setup.exe) from [GitHub Releases](https://github.com/mohitbansal25082006/KomiK/releases).
2. **Run** the setup wizard.
3. **Choose** whether to add a Desktop shortcut (off by default), and keep file associations enabled so `.cbz` `.cbr` `.cb7` `.zip` `.rar` `.7z` `.pdf` open in Komik.
4. **Install.** Komik goes to `C:\Program Files\Komik` with a Start Menu shortcut and a clean uninstaller under *Settings → Installed Apps*.

</td>
<td width="45%" valign="top">

### 🖥️ System requirements

| | |
| :-- | :-- |
| **OS** | Windows 10 1809 (build 17763)+ or Windows 11 |
| **Architecture** | x64 (ARM64 via Windows 11 x64 emulation) |
| **Installer** | ~60 MB, fully self-contained |
| **Prerequisites** | **None.** .NET 8 and the Windows App SDK runtime are bundled |

<sub>Windows 11 is recommended for native Mica backdrops.</sub>

</td>
</tr>
</table>

> [!IMPORTANT]
> **"Windows protected your PC"?** Komik is free, open-source software distributed without a costly EV code-signing certificate, so SmartScreen may warn you the first time you run `Komik-Setup.exe`. Click **More info**, then **Run anyway**. Every line of code is public in this repository: zero telemetry, zero network activity.

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-keyboard-first-reading"></a>
<img src="komik-website/public/readme/section-shortcuts.svg" alt="Keyboard-first reading" width="100%" />

| Shortcut | Action | Where |
| :-- | :-- | :-: |
| <kbd>→</kbd> <kbd>Space</kbd> <kbd>PgDn</kbd> | Next page / next spread | 📖 |
| <kbd>←</kbd> <kbd>Shift</kbd>+<kbd>Space</kbd> <kbd>PgUp</kbd> | Previous page / previous spread | 📖 |
| <kbd>Home</kbd> / <kbd>End</kbd> | First / last page | 📖 |
| <kbd>Alt</kbd>+<kbd>←</kbd> / <kbd>Backspace</kbd> | Back to Library (saves progress) | 📖 |
| <kbd>Ctrl</kbd>+<kbd>D</kbd> / <kbd>Ctrl</kbd>+<kbd>B</kbd> | Toggle bookmark on the current page | 📖 |
| <kbd>Ctrl</kbd>+<kbd>R</kbd> | Western LTR ↔ Manga RTL | 📖 |
| <kbd>D</kbd> | Single page ↔ two-page spread | 📖 |
| <kbd>V</kbd> | Webtoon continuous vertical scroll | 📖 |
| <kbd>Ctrl</kbd>+<kbd>F</kbd> | Search comic dialogue (offline OCR) | 📖 |
| <kbd>W</kbd> · <kbd>H</kbd> · <kbd>A</kbd> | Fit width · fit height · actual size | 📖 |
| <kbd>Ctrl</kbd>+<kbd>+</kbd> / <kbd>Ctrl</kbd>+<kbd>-</kbd> / <kbd>Ctrl</kbd>+<kbd>0</kbd> | Zoom in / out / reset | 📖 |
| <kbd>Ctrl</kbd>+<kbd>O</kbd> | Open a comic file | 🌐 |
| <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>O</kbd> | Open an image folder | 🌐 |
| <kbd>F11</kbd> | Toggle full screen | 🌐 |
| <kbd>Esc</kbd> | Exit full screen / dismiss overlays | 🌐 |

<sub>📖 Reader · 🌐 Global</sub>

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-your-comics-belong-on-your-pc"></a>
<img src="komik-website/public/readme/section-privacy.svg" alt="Your comics belong on your PC" width="100%" />

<div align="center">

| 📴 **100% offline** | 🙅 **No accounts, ever** | 🔒 **Zero telemetry** | 🗃️ **Non-destructive** |
| :-: | :-: | :-: | :-: |
| No external network calls. It works on a plane, in a tunnel, anywhere. | No email, sign-in, tokens or passwords. Install and read. | No tracking SDKs, crash beacons or session recordings. | Progress lives in local SQLite. Your original files are never modified or deleted. |

</div>

> Most modern comic apps want an account, your archive on their servers, behavioral telemetry, or a monthly subscription just to turn a digital page.<br />
> **Komik is deliberately built as the opposite of that model.**

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-under-the-hood"></a>
<img src="komik-website/public/readme/section-under-the-hood.svg" alt="Under the hood" width="100%" />

### 🧩 Architecture

```mermaid
flowchart LR
    subgraph UI["🪟 WinUI 3 views"]
        MW["MainWindow"] --> LP["LibraryPage"]
        MW --> RP["MainPage (reader)"]
        MW --> SP["SettingsPage"]
    end

    subgraph VM["🧠 ViewModels · CommunityToolkit.Mvvm"]
        LVM["LibraryViewModel"]
        MVM["MainViewModel"]
        SVM["SettingsViewModel"]
    end

    subgraph SVC["⚙️ Services"]
        CLS["ComicLoaderService"] --> ZIP["ZipComicLoader"]
        CLS --> RAR["RarComicLoader"]
        CLS --> SZ["SevenZipComicLoader"]
        CLS --> PDF["PdfComicLoader"]
        CLS --> DIR["FolderComicLoader"]
        SCAN["LibraryScannerService"]
        THUMB["ThumbnailService"]
        CONV["FormatConversionService"]
        DUP["DuplicateDetectionService"]
        OCR["OcrService"]
    end

    DB[("🗄️ SQLite · WAL<br/>LibraryRepository")]

    LP --> LVM
    RP --> MVM
    SP --> SVM
    MVM --> CLS
    MVM --> OCR
    LVM --> SCAN
    LVM --> CONV
    LVM --> DUP
    SCAN --> THUMB
    LVM --> DB
    MVM --> DB
    SVM --> DB
```

### 🛠️ Tech stack

| Layer | Technology |
| :-- | :-- |
| UI | **WinUI 3** · Windows App SDK · Fluent Design · Mica |
| Language / runtime | **C# · .NET 8** (`net8.0-windows10.0.26100.0`) |
| MVVM | CommunityToolkit.Mvvm |
| Archives | SharpCompress (RAR4/RAR5, 7-Zip), System.IO.Compression |
| PDF | Docnet.Core (PDFium) |
| Rendering | Win2D |
| Storage | Microsoft.Data.Sqlite (WAL) |
| OCR | `Windows.Media.Ocr` (built into Windows) |
| Installer | Inno Setup 6 (LZMA2 / ultra64) |

<details>
<summary><b>🗄️ Local database schema (click to expand)</b></summary>
<br />

Komik keeps everything in one SQLite database with Write-Ahead Logging and foreign keys enabled. The durable location is `%USERPROFILE%\.komik\komik_library.db`; older copies in `%LocalAppData%\Komik` are migrated automatically. Cover thumbnails are cached next to it in `Thumbnails\`.

```sql
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;

CREATE TABLE IF NOT EXISTS WatchedFolders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    path TEXT UNIQUE NOT NULL,
    date_added TEXT NOT NULL,
    last_scanned TEXT
);

CREATE TABLE IF NOT EXISTS Comics (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    file_path TEXT UNIQUE NOT NULL,
    title TEXT NOT NULL,
    format TEXT NOT NULL,
    page_count INTEGER NOT NULL DEFAULT 0,
    thumbnail_path TEXT,
    date_added TEXT NOT NULL,
    last_modified TEXT NOT NULL,
    parent_folder TEXT,
    file_size INTEGER NOT NULL DEFAULT 0,
    is_favorite INTEGER NOT NULL DEFAULT 0,
    is_missing INTEGER NOT NULL DEFAULT 0,
    last_read_page INTEGER NOT NULL DEFAULT 0,
    last_read_at TEXT,
    is_completed INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS Tags (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT UNIQUE NOT NULL COLLATE NOCASE
);

CREATE TABLE IF NOT EXISTS ComicTags (
    comic_id INTEGER NOT NULL,
    tag_id INTEGER NOT NULL,
    PRIMARY KEY(comic_id, tag_id),
    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE,
    FOREIGN KEY(tag_id) REFERENCES Tags(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Collections (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT UNIQUE NOT NULL COLLATE NOCASE
);

CREATE TABLE IF NOT EXISTS ComicCollections (
    comic_id INTEGER NOT NULL,
    collection_id INTEGER NOT NULL,
    PRIMARY KEY(comic_id, collection_id),
    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE,
    FOREIGN KEY(collection_id) REFERENCES Collections(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Bookmarks (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    comic_id INTEGER NOT NULL,
    page_number INTEGER NOT NULL,
    user_note TEXT,
    created_at TEXT NOT NULL,
    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ComicMetadata (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    comic_id INTEGER UNIQUE NOT NULL,
    title TEXT,
    issue_number TEXT,
    series_name TEXT,
    writers TEXT,
    artists TEXT,
    publisher TEXT,
    release_date TEXT,
    summary TEXT,
    last_updated TEXT NOT NULL,
    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS AppSettings (
    key TEXT PRIMARY KEY NOT NULL,
    value TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ReadingSessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    comic_id INTEGER NOT NULL,
    start_time TEXT NOT NULL,
    end_time TEXT NOT NULL,
    duration_seconds INTEGER NOT NULL,
    pages_read INTEGER NOT NULL,
    session_date TEXT NOT NULL,
    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS IgnoredDuplicates (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    comic_id_1 INTEGER NOT NULL,
    comic_id_2 INTEGER NOT NULL,
    date_ignored TEXT NOT NULL,
    UNIQUE(comic_id_1, comic_id_2)
);

CREATE TABLE IF NOT EXISTS ManualSeries (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT UNIQUE NOT NULL COLLATE NOCASE,
    date_created TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ManualSeriesComics (
    series_id INTEGER NOT NULL,
    comic_id INTEGER NOT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY(series_id, comic_id),
    FOREIGN KEY(series_id) REFERENCES ManualSeries(id) ON DELETE CASCADE,
    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE
);

-- Indexes: Comics(file_path, title, is_favorite, last_read_at), ComicTags(comic_id, tag_id)
```

</details>

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-build-it-yourself"></a>
<img src="komik-website/public/readme/section-build.svg" alt="Build it yourself" width="100%" />

### 📋 Prerequisites
- Windows 10 (1809+) or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (8.0.400 or later)
- [Inno Setup 6](https://jrsoftware.org/isdl.php), only needed to build the installer (`winget install JRSoftware.InnoSetup`)

### 🔨 Build & run

```powershell
# Build the solution
dotnet build Komik.sln -c Release -p:Platform=x64

# Run the app
& ".\bin\x64\Release\net8.0-windows10.0.26100.0\win-x64\Komik.exe"
```

### 🧪 Run the automated test suite

```powershell
dotnet run --project Komik.Tests/Komik.Tests.csproj -c Debug
```

<details>
<summary><b>What the 27 test suites cover (click to expand)</b></summary>
<br />

| # | Suite |
| :-: | :-- |
| 1 | `NaturalSortComparer` orders numeric sequences |
| 2 | `FolderComicLoader` loads folder pages in order |
| 3 | `ZipComicLoader` loads CBZ/ZIP archives |
| 4 | `PdfComicLoader` loads and renders PDF documents |
| 5 | `ComicLoaderService` dispatches all formats correctly |
| 6 | Error handling rejects corrupted and empty files gracefully |
| 7 | `LibraryRepository` handles the SQLite schema, CRUD, tags & filters |
| 8 | `ThumbnailService` generates and caches thumbnails |
| 9 | `LibraryScannerService` indexes comics and handles rescans |
| 10 | Reading progress & In-Progress/Unread filters |
| 11 | Comic bookmarks CRUD and page queries |
| 12 | Spread pairing, RTL order, color LUT and progress helpers |
| 13 | `RarComicLoader` loads RAR4 & RAR5, filters junk, rejects passwords |
| 14 | `SevenZipComicLoader` loads 7Z/CB7 archives and `ComicLoaderService` dispatches them |
| 15 | `FormatConversionService` converts CBR, CB7 and folders to valid CBZ |
| 16 | Scanner discovers RAR/7Z comics and generates thumbnails |
| 17 | `AppSettings` persistence and retrieval |
| 18 | `ComicMetadata` CRUD and title updating |
| 19 | Folder collections & re-reading completed comics |
| 20 | Virtual in-app folders, soft delete & JSON backup cache |
| 21 | `SeriesParserHelper` parses titles and issues |
| 22 | `DuplicateDetectionService` finds cross-format & normalized duplicates |
| 23 | `ColorCorrectionHelper` applies color presets |
| 24 | Window geometry save & restore |
| 25 | Reading sessions & statistics |
| 26 | Portable backup JSON export & restore |
| 27 | Manual series creation, retrieval & deletion |

</details>

### 📦 Build the installer

```powershell
# 1. Publish a self-contained, unpackaged win-x64 build
dotnet publish Komik.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=false -p:WindowsAppSDKSelfContained=true -p:WindowsPackageType=None `
  -o "publish_selfcontained"

# 2. Compile the Inno Setup script
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\Komik.iss
#    …or, for a per-user Inno Setup install:
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\Komik.iss
```

The finished installer lands in **`shipping\Komik-Setup.exe`**.

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-the-komik-website"></a>
<img src="komik-website/public/readme/section-website.svg" alt="The Komik website" width="100%" />

The marketing and download site lives in [`komik-website/`](komik-website/). It's built as a **living comic book**, and its centerpiece is a working replica of the Komik reader that holds a complete, original 12-page comic.

| | |
| :-- | :-- |
| **Stack** | Next.js 15 (App Router) · React 19 · TypeScript · Tailwind CSS |
| **Motion** | GSAP 3 (ScrollTrigger + SplitText) · Lenis smooth scroll · Framer Motion |
| **Look** | Bangers & Comic Neue lettering · halftone screens · CMYK ink palette · speed lines |
| **Demo comic** | *Cyberpunk Chronicles #01, "The Last Local Archive"*: 12 vector SVG pages |
| **Reader replica** | Spreads · manga RTL · webtoon · fit & zoom · 6 color presets · bookmarks · dialogue search · animated full screen · swipe & pinch on mobile |
| **Design doc** | [`komik-website/DESIGN.md`](komik-website/DESIGN.md) |

```powershell
cd komik-website
npm install
npm run dev      # http://localhost:3000
npm run build    # production build
```

<details>
<summary><b>Configuration & deployment</b></summary>
<br />

- **Download links** are centralized in `komik-website/lib/config.ts`:
  - Installer: `https://github.com/mohitbansal25082006/KomiK/releases/latest/download/Komik-Setup.exe`
  - Releases: `https://github.com/mohitbansal25082006/KomiK/releases`
- **Social preview:** replace `komik-website/public/og-image.png` with any 1200×630 PNG.
- **Vercel:** import the repository, set **Root Directory** to `komik-website`, keep the Next.js preset, and deploy.
- **README artwork:** the animated banners and screenshots used on this page live in `komik-website/public/readme/`.

</details>

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

## ⚠️ Known limitations

- **Explorer in-folder thumbnails.** Cover previews inside File Explorer need an in-process, 64-bit native COM `IThumbnailProvider` DLL. WinUI 3 apps run as standalone executables, so that handler can't be written in managed WinUI C# without a separate C++/WinRT companion. Komik generates fast cover thumbnails inside its own UI, and file associations plus direct launching are handled through normal Windows shell integration.

## 📜 License

Komik is released under the **MIT License**. See [`LICENSE`](LICENSE).

## 🙏 Acknowledgements

- [WinUI 3](https://github.com/microsoft/microsoft-ui-xaml) and the [Windows App SDK](https://github.com/microsoft/WindowsAppSDK)
- [SharpCompress](https://github.com/adamhathcock/sharpcompress) for pure managed archive handling
- [Docnet.Core](https://github.com/GowenGit/docnet) (PDFium) for PDF rendering
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) and [Win2D](https://github.com/microsoft/Win2D)
- The [Windows 11 Fluent Design System](https://learn.microsoft.com/windows/apps/design/)

<div align="center">

<img src="komik-website/public/readme/the-end.svg" alt="The End... or is it?" width="100%" />

**Created & maintained by [Mohit Bansal](https://github.com/mohitbansal25082006)** · Built natively for Windows with WinUI 3 & .NET 8

<sub>If Komik made your reading better, drop a ⭐ on the repo. It helps other readers find it.</sub>

</div>
