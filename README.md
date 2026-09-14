# Komik — Modern Fluent Comic Viewer & Reader

**Komik** is a lightweight, high-performance, local-only Windows desktop application built with **WinUI 3 (Windows App SDK)** and **C# (.NET 8)**. Designed specifically for comic and manga enthusiasts, it adheres closely to Windows 11 Fluent Design principles—featuring a native Mica backdrop, subtle acrylic controls, smooth animations, and full dark and light mode support.

> [!NOTE]
> **Privacy & Offline Architecture**:
> Komik is strictly an offline, local-first application. There is **no cloud synchronization, no user account system, no telemetry or tracking, and zero external network calls anywhere in the application**. All comic metadata, reading progress, cover thumbnails, bookmarks, and preferences reside entirely on your local machine.

---

## Screenshots

*(Screenshots of the Library, Reader Canvas, Double-Page Spread, and Settings will be added here shortly after release)*

---

## Download & Install

### For Users
1. Download the latest Windows installer **`Komik-Setup.exe`** from the [GitHub Releases](https://github.com/mohitbansal25082006/KomiK/releases) page.
2. Double-click `Komik-Setup.exe` to run the setup wizard.
3. Follow the setup steps:
   - Choose whether to create a Desktop shortcut (optional, unchecked by default).
   - Keep file associations enabled to open comic formats (`.cbz`, `.cbr`, `.cb7`, `.zip`, `.rar`, `.7z`, `.pdf`) directly in Komik by double-clicking them in File Explorer.
4. Click **Install**. Komik installs cleanly into standard `C:\Program Files\Komik` with a Start Menu shortcut and a clean uninstaller accessible via Windows Settings -> Installed Apps.

> [!TIP]
> **Windows SmartScreen Note ("Windows protected your PC")**:
> Komik is completely free, open-source software distributed without an expensive corporate EV (Extended Validation) code-signing certificate. Because of this, Windows Defender SmartScreen may display an unrecognized app alert when you first run `Komik-Setup.exe`.
> 
> To proceed:
> 1. Click **More info**.
> 2. Click **Run anyway**.
> 
> Komik is 100% open-source for full transparency, containing zero telemetry and zero network activity.

### Supported File Formats
Komik natively supports all standard digital comic book archives, containers, and documents:
- **`.cbz`** / **`.zip`** (Comic Book ZIP archive)
- **`.cbr`** / **`.rar`** (Comic Book RAR4 & RAR5 archive)
- **`.cb7`** / **`.7z`** (Comic Book 7-Zip LZMA/LZMA2 archive)
- **`.pdf`** (Portable Document Format)
- Extracted image folders (JPEG, PNG, WEBP, BMP, GIF)

### System Requirements
- **Operating System**: Windows 10 version 1809 (Build 17763) or higher, or Windows 11 (64-bit). Windows 11 is recommended for native Mica backdrop rendering.
- **Architecture**: x64 (AMD64/Intel 64) or ARM64 (via Windows 11 x64 emulation).
- **Zero Pre-requisites**: The installer is completely self-contained and bundles both the .NET runtime and Windows App SDK runtime directly — no separate .NET or Windows App SDK installations required.

---

---

## Key Features (Version 1.1.0)

### 📖 High-Performance Reading Canvas & Parallel Webtoon Engine
- **Ultra-Fast Parallel Webtoon Preloader**: Continuous vertical reading mode (`V`) powered by a 6-worker pool (`SemaphoreSlim(6)`) decoding high-resolution pages concurrently off-thread outward from the active viewport index for stutter-free reading.
- **Smooth Continuous Scroll Navigation**: Restricted navigation to smooth mouse wheel scrolling and keyboard keys (`Up`, `Down`, `PageUp`, `PageDown`), completely eliminating erratic pointer panning jumps.
- **Seamless Page Jumps**: Animated vertical scroll targeting (`ScrollToWebtoonPage`) with `EntranceThemeTransition` when jumping via the scrubber or next/previous buttons.
- **Smooth Animated Scroll to Top on Page Flip**: Viewport automatically and smoothly animates scroll position back to the top of the new page when flipping pages in standard and spread modes.
- **Versatile Fit Modes**:
  - **Fit to Width** (`W`): Fits page width to the viewport for comfortable reading.
  - **Fit to Height** (`H`): Fits the full page height to the viewport for single-screen reading.
  - **Actual Size** (`A`): 1:1 native pixel dimensions with smooth drag panning.
- **Arbitrary Zoom**: Zoom dynamically with `Ctrl + +` / `Ctrl + -` or mouse scroll, and reset with `Ctrl + 0`.
- **Reading Modes & Direction**: Effortlessly toggle Single-Page and Two-Page Spread views (`D`), Western LTR and Manga RTL (`Ctrl + R`) with intelligent cover isolation.

### 📚 Standalone Series & Volume Management & Custom Builder
- **Dedicated Standalone Series Screen**: Fully decoupled from comic filter chips with its own dedicated toolbar, series search bar, series sort options, and real-time badge counts.
- **Intelligent Auto-Clustering ($\ge 90\%$ Similarity)**: Evaluates all library comics using Levenshtein distance and structural normalization, grouping multi-issue runs and tankōbon volumes when titles match 90% or higher.
- **Custom Manual Series Builder**: Create custom reading orders, name series runs, search and select library issues via a visual candidate picker, and reorder comics.
- **Add Comics to Existing Series**: Interactive `+ Add Comics` action inside the Series Detail overlay allowing readers to search and add any comic from the library directly into existing series.

### 🗂️ Responsive Library Controls & Top Bar
- **Combined "ADD" DropDown Button**: Merged separate "Add Folder" and "Add Comic" buttons into a single high-contrast Accent "ADD" button with an interactive dropdown flyout, conserving over 100px of toolbar width.
- **Clean Library Title**: Removed redundant logo image from the Library header next to the title, keeping the official vector crest cleanly featured in the Settings About section.
- **Horizontal Mouse Wheel Scrolling**: Standard mouse wheel rolling over the Library toolbar and filter pills smoothly scrolls horizontally on compact screens without clipping.
- **Multiple Layout Views**: Switch between cover card grid view, series grouping view, and detailed table list view.
- **Instant Search & Real-Time Filtering**: Search titles instantly, or filter by Favorites, In-Progress, Unread, tags, or custom collections.
- **Rich Sorting Options**: Sort by Title (A-Z / Z-A), Recently Read, Date Added (Newest/Oldest), Page Count, and File Size.
- **Thumbnail Cache**: Disk-backed cover thumbnail generator and cache in `%LocalAppData%\Komik\Thumbnails` for near-instant rendering.

### 📊 Reading Statistics & Komik Wrapped
- **Real-Time Tracking**: Every page turn automatically logs active session duration and page progress in SQLite.
- **Top 20 Rankings**: Displays the Top 20 most-read comics and Top 20 series runs, with right-aligned progress percentages and duration statistics.
- **Komik Wrapped Dashboard**: View total reading time, total pages read, comics completed, current & longest daily reading streaks, and annual shareable Wrapped card.
- **Universal Light & Dark Mode Compatibility**: Semantic modal brushes dynamically adapt to Windows system theme changes without background bleed-through.

### 👥 Duplicate Comic Detection & Management
- **Multi-Factor Detection ($\ge 95\%$ Match)**: Detects duplicate copies across different file formats (e.g. `.cbz` vs. `.pdf` vs. `.cbr`), matching byte counts, page numbers, and normalized title similarity $\ge 95\%$.
- **Clear Resolution Actions**: Inspect side-by-side file paths, page counts, and sizes; delete unwanted duplicates permanently from disk, or click **Keep All** to dismiss duplicate warnings and preserve both copies.

### 🔍 100% Offline Windows OCR (Text Search & Selectable Text)
- **Zero Cloud, 100% Local**: Powered directly by Windows' built-in `Windows.Media.Ocr.OcrEngine` API shipping with Windows 10/11. Zero external network calls or cloud APIs.
- **Dialogue Search**: Search in-comic spoken dialogue and text, view matching word occurrences, and jump directly to the target page with a single click.
- **Selectable Text Overlay Layer**: View, highlight, and copy transcribed comic dialogue directly from scanned artwork pages.

### 🔖 Reading Progress, Bookmarks & Hardware LUT Color Presets
- **Automatic Progress Tracking**: Remembers your last read page and completion status in SQLite with zero lag.
- **Resume Reading Toast**: Non-intrusive notification ("Resumed at page X") when reopening any comic.
- **Visual Scrubber Bar**: Interactive seekbar with page numbers and thumbnail tooltips for easy navigation.
- **Bookmark System**: Bookmark any page with optional custom notes (`Ctrl + D` / `Ctrl + B`).
- **6 Hardware-Accelerated Reading Presets**:
  - **Original**: Natural scan colors.
  - **Night Mode**: Amber warmth with dimmed background.
  - **Sepia Tone**: Classic warm parchment paper effect.
  - **High Contrast**: Enhanced ink line sharpness and deep blacks.
  - **Grayscale**: Pure monochrome rendering.
  - **Inverted**: Dark mode inversion for white backgrounds.
  - Fine-grained Brightness (-100 to +100), Contrast (0.5 to 2.0), and Warmth (-100 to +100) sliders.

### 💾 Portable Library Backups (`.komikbackup`)
- **JSON Export & Import**: Safely export all reading history, bookmarks, user notes, favorites, tags, monitored folders, and app settings into a single portable `.komikbackup` file.
- **Portability**: Move your library progress between PCs without cloud syncing or vendor lock-in.

### 📦 Pure .NET Archive Support & Universal CBZ Conversion
- **Zero External Dependencies**: Pure managed RAR4/RAR5 and 7Z/CB7 archive extraction powered by SharpCompress—no external command-line tools or native `unrar.dll` needed.
- **Universal CBZ Converter**: Convert any comic format (image folder, CBR, CB7, or PDF) to a standard `.cbz` archive with zero-copy lossless stream repacking for archives and high-fidelity rasterization for PDFs.

### ⚙️ Settings, Window Geometry & Shell Integration
- **Window Geometry & Ratio Persistence**: Remembers window dimensions, monitor position, and maximized state across application launches.
- **Official Vector Crest Identity**: Fresh branding across the application icon, title bar, taskbar, splash screen, settings page, and installer.
- **Consolidated Settings Screen**:
  - Theme Override: System Default, Force Light, or Force Dark mode.
  - Reading Defaults: Default Fit Mode, Default Reading Direction, and Reading Theme Presets.
  - Library Defaults: Default View Mode and Default Sort Option.
  - Monitored Folders: Manage watched library directories with confirmation prompts.
  - Thumbnail Cache Management: View cache size, clear cache, or pre-cache library covers with one click.
  - Library Backup & Restore: One-click export and import of `.komikbackup` files with conflict resolution.
- **Local Comic Details Dialog**:
  - Inspect cover thumbnails, page counts, file sizes, reading status, and file paths.
  - Edit extended local metadata: Title, Series Name, Issue #, Writer(s), Artist(s), Publisher, Release Date, and Summary.
- **Windows Shell Integration**:
  - Double-clicking any supported comic in File Explorer opens directly into the reader.
  - Command-line argument support (`Komik.exe "<path-to-comic>"`).

---

## Keyboard Shortcut Reference

| Shortcut | Action | Context |
| :--- | :--- | :--- |
| **Right Arrow / Space / Page Down** | Next Page (or Next Spread) | Reader |
| **Left Arrow / Page Up / Shift+Space** | Previous Page (or Previous Spread) | Reader |
| **Home / End** | Jump to First / Last Page | Reader |
| **Alt + Left / Backspace** | Return to Library (flushes reading progress) | Reader |
| **Ctrl + D / Ctrl + B** | Toggle Bookmark for Current Page | Reader |
| **Ctrl + R** | Toggle Reading Direction (LTR Western vs. RTL Manga) | Reader |
| **D** | Toggle Single Page / Two-Page Spread | Reader |
| **V** | Toggle Webtoon Continuous Vertical Scroll Mode | Reader |
| **Ctrl + F** | Search In-Comic Dialogue (Offline Windows OCR) | Reader |
| **W** | Fit to Width | Reader |
| **H** | Fit to Height | Reader |
| **A** | Actual Size (100% native pixels) | Reader |
| **Ctrl + + / Ctrl + -** | Zoom In / Zoom Out | Reader |
| **Ctrl + 0** | Reset Zoom (100%) | Reader |
| **Ctrl + O** | Open Single Comic File | Global |
| **Ctrl + Shift + O** | Open Image Folder | Global |
| **F11** | Toggle Full-Screen Mode | Global |
| **Escape** | Exit Full-Screen / Dismiss Error Overlays | Global |

---

## Local Database Schema

Komik stores all library data, progress, and metadata in a lightweight local SQLite database located at `%LocalAppData%\Komik\komik_library.db` with Write-Ahead Logging (`WAL`) enabled:

```sql
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
    comic_id_1 INTEGER NOT NULL,
    comic_id_2 INTEGER NOT NULL,
    date_ignored TEXT NOT NULL,
    PRIMARY KEY(comic_id_1, comic_id_2)
);
```

---

## Building from Source

### Prerequisites
- Windows 10 (version 1809+) or Windows 11 (Windows 11 recommended for native Mica material)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (v8.0.400 or later)

### Build the Solution
```powershell
dotnet build Komik.sln -c Release -p:Platform=x64
```

### Run the Application
```powershell
& ".\bin\x64\Release\net8.0-windows10.0.26100.0\win-x64\Komik.exe"
```

### Run the Automated Test Suite (20 Suites Passing)
```powershell
dotnet run --project Komik.Tests/Komik.Tests.csproj -c Debug
```

The automated test suite verifies:
1. `NaturalSortComparer` alphanumeric page ordering
2. `FolderComicLoader` image directory discovery and sequencing
3. `ZipComicLoader` CBZ/ZIP archive decompression
4. `PdfComicLoader` vector and raster PDF document rendering
5. `ComicLoaderService` multi-format dispatching
6. Graceful error handling for corrupted, truncated, and empty archives
7. `LibraryRepository` SQLite schema migrations, CRUD, and filter queries
8. `ThumbnailService` disk cache generation and cleanup
9. `LibraryScannerService` recursive directory indexing and missing file detection
10. Reading progress persistence and In-Progress/Unread state filters
11. `Comic Bookmarks` CRUD and page query helpers
12. Spread pairing, Manga RTL reading order, and Color LUT calculations
13. `RarComicLoader` RAR4/RAR5 decompression and junk-file filtering
14. `SevenZipComicLoader` 7Z/CB7 decompression
15. `FormatConversionService` conversion of Folders, CBR, CB7, and PDF to valid CBZ archives
16. Discovery and thumbnailing for RAR and 7Z comic archives
17. `AppSettings` persistence, theme switching, and default preferences
18. `ComicMetadata` CRUD operations and title synchronization
19. Folder organization, collections, and completed comic re-reading workflows
20. Virtual in-app folders, soft-delete safety, and JSON cache integrity

---

## Building the Installer Yourself

To compile the standalone Windows installer from source:

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (v8.0.400 or later)
- [Inno Setup 6](https://jrsoftware.org/isdl.php) (v6.2 or later, e.g. via `winget install JRSoftware.InnoSetup`)

### 1. Publish Self-Contained Output
Publish the self-contained unpackaged win-x64 binary bundle:
```powershell
dotnet publish Komik.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:WindowsAppSDKSelfContained=true -p:WindowsPackageType=None -o "publish_selfcontained"
```

### 2. Compile Installer with Inno Setup
Run the Inno Setup compiler on `installer/Komik.iss`:
```powershell
# Using default Inno Setup 6 installation path:
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\Komik.iss

# Or if installed in user profile:
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\Komik.iss
```

The completed setup binary will be generated at:
```text
shipping\Komik-Setup.exe
```

---

## Website (`komik-website`)

A fast, static/hybrid marketing and download landing page for Komik is included directly in this repository under the [`komik-website/`](komik-website/) subfolder. It presents Komik's feature set and directs visitors straight to the latest Windows installer on GitHub Releases.

- **Design System & Motion Plan**: Fully documented in [`komik-website/DESIGN.md`](komik-website/DESIGN.md).
- **Tech Stack**: Next.js 15 (App Router), TypeScript, Tailwind CSS, Framer Motion, and Lucide React.
- **Visual Style**: "The Nocturnal Graphic Novel" — dark-theme-first, authentic comic panel gutters, screen-tone halftones, and an interactive WinUI 3 reading canvas simulator.

### Running the Website Locally
```powershell
cd komik-website
npm install
npm run dev
```
Open [http://localhost:3000](http://localhost:3000) in your browser.

### Building for Production
```powershell
cd komik-website
npm run build
```

### Download Link Configuration
All download buttons point directly to the GitHub Releases latest asset URL. The repository coordinates and asset paths are centralized in a single configuration file:
```text
komik-website/lib/config.ts
```
Download targets:
- **Direct Installer Download**: `https://github.com/mohitbansal25082006/KomiK/releases/latest/download/Komik-Setup.exe`
- **Releases & Changelog Page**: `https://github.com/mohitbansal25082006/KomiK/releases`

### Social Preview (Open Graph) Image
The Open Graph social share image is stored at:
```text
komik-website/public/og-image.png
```
To swap in a live application screenshot or new artwork, replace this file with any standard `1200×630` PNG image.

### Deploying to Vercel
The website is ready for immediate deployment on [Vercel](https://vercel.com) from this monorepo:
1. Import the `KomiK` repository into your Vercel account.
2. In the project setup screen, expand **Root Directory**, click **Edit**, and select **`komik-website`**.
3. Leave the **Framework Preset** as `Next.js` and keep all build and output settings at their defaults (`npm run build`).
4. Click **Deploy**. Vercel will build and serve the subfolder automatically.

---

## Known Limitations

- **Windows Explorer In-Folder Thumbnail Provider**:
  Windows Explorer in-folder cover thumbnails require an in-process, 64-bit native COM server DLL implementing `IThumbnailProvider`. Because WinUI 3 desktop applications execute as out-of-process standalone executables, an in-process COM handler cannot be implemented purely in standard managed WinUI C# without a separate C++/WinRT companion DLL. Komik maintains high-speed cover thumbnail generation inside its own Fluent UI, while file associations and direct launching are handled natively via Windows shell integration.

---

## License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## Acknowledgements

- Built with [WinUI 3](https://github.com/microsoft/microsoft-ui-xaml) and the [Windows App SDK](https://github.com/microsoft/WindowsAppSDK).
- PDF rendering powered by [Docnet.Core](https://github.com/Goweed/Docnet) (PDFium).
- Pure managed archive handling powered by [SharpCompress](https://github.com/adamhathcock/sharpcompress).
- Icons and styling follow the [Windows 11 Fluent Design System](https://www.microsoft.com/design/fluent/).
