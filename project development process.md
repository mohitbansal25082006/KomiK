# Komik — Version 1 Development Process & File Manifest

This document records the complete manifest of files created for Version 1.0 of **Komik**, along with a summary of all features delivered in this release.

---

## 1. File Manifest (Version 1.0)

### Configuration, Manifests & Documentation
- `.gitignore`
- `app.manifest`
- `App.xaml`
- `App.xaml.cs`
- `Komik.csproj`
- `Komik.sln`
- `LICENSE`
- `Package.appxmanifest`
- `project development process.md`
- `README.md`
- `Properties\launchSettings.json`
- `Properties\PublishProfiles\win-arm64.pubxml`
- `Properties\PublishProfiles\win-x64.pubxml`
- `Properties\PublishProfiles\win-x86.pubxml`

### User Interface Views & Code-Behind
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
- `LibraryPage.xaml`
- `LibraryPage.xaml.cs`
- `MainPage.xaml`
- `MainPage.xaml.cs`
- `SettingsPage.xaml`
- `SettingsPage.xaml.cs`

### ViewModels (MVVM)
- `ViewModels\LibraryViewModel.cs`
- `ViewModels\MainViewModel.cs`
- `ViewModels\SettingsViewModel.cs`

### Data Models & Entities
- `Models\ApplicationFolder.cs`
- `Models\AppSettings.cs`
- `Models\BookmarkEntity.cs`
- `Models\ColorCorrectionSettings.cs`
- `Models\ComicBook.cs`
- `Models\ComicEntity.cs`
- `Models\ComicMetadataEntity.cs`
- `Models\ComicPageData.cs`
- `Models\ComicSourceType.cs`
- `Models\FitMode.cs`
- `Models\IComicPage.cs`
- `Models\LibraryFilterAndSort.cs`
- `Models\ReadingDirection.cs`
- `Models\TagAndCollection.cs`
- `Models\ViewMode.cs`
- `Models\WatchedFolder.cs`

### Services & Business Logic
- `Services\ComicLoaderService.cs`
- `Services\FilePickerService.cs`
- `Services\FolderComicLoader.cs`
- `Services\FormatConversionService.cs`
- `Services\IComicLoader.cs`
- `Services\ILibraryRepository.cs`
- `Services\ILibraryScannerService.cs`
- `Services\LibraryRepository.cs`
- `Services\LibraryScannerService.cs`
- `Services\PdfComicLoader.cs`
- `Services\RarComicLoader.cs`
- `Services\SevenZipComicLoader.cs`
- `Services\ThumbnailService.cs`
- `Services\ZipComicLoader.cs`

### Helpers & Utilities
- `Helpers\ColorCorrectionHelper.cs`
- `Helpers\ImageHelper.cs`
- `Helpers\InstantToolTipHelper.cs`
- `Helpers\NaturalSortComparer.cs`
- `Helpers\ValueConverters.cs`

### Assets & Visual Resources
- `Assets\AppIcon.ico`
- `Assets\LockScreenLogo.scale-200.png`
- `Assets\SplashScreen.scale-200.png`
- `Assets\Square150x150Logo.scale-200.png`
- `Assets\Square44x44Logo.scale-200.png`
- `Assets\Square44x44Logo.targetsize-24_altform-unplated.png`
- `Assets\Square44x44Logo.targetsize-48_altform-lightunplated.png`
- `Assets\StoreLogo.png`
- `Assets\Wide310x150Logo.scale-200.png`

### Installer
- `installer\Komik.iss`

### Automated Test Suite
- `Komik.Tests\Komik.Tests.csproj`
- `Komik.Tests\Program.cs`

### Marketing & Download Website (`komik-website/`)
- `komik-website\.gitignore`
- `komik-website\DESIGN.md`
- `komik-website\next.config.mjs`
- `komik-website\package.json`
- `komik-website\package-lock.json`
- `komik-website\postcss.config.mjs`
- `komik-website\tailwind.config.ts`
- `komik-website\tsconfig.json`
- `komik-website\app\globals.css`
- `komik-website\app\layout.tsx`
- `komik-website\app\page.tsx`
- `komik-website\components\DownloadSection.tsx`
- `komik-website\components\FeaturePanels.tsx`
- `komik-website\components\Footer.tsx`
- `komik-website\components\FormatShowcase.tsx`
- `komik-website\components\Hero.tsx`
- `komik-website\components\KeyboardSection.tsx`
- `komik-website\components\KomikLogo.tsx`
- `komik-website\components\ManifestoSection.tsx`
- `komik-website\components\MobileNotice.tsx`
- `komik-website\components\MockComicPages.tsx`
- `komik-website\components\Navbar.tsx`
- `komik-website\lib\config.ts`
- `komik-website\lib\utils.ts`
- `komik-website\public\app-icon.png`
- `komik-website\public\favicon.ico`
- `komik-website\public\og-image.png`

---

## 2. Features Added in Version 1.0 (Summary)

1. **High-Performance Reading Canvas**:
   - Three fit modes: Fit to Width (`W`), Fit to Height (`H`), and Actual Size 1:1 (`A`).
   - Smooth arbitrary zoom scaling (`Ctrl + +` / `Ctrl + -`, `Ctrl + 0` reset).
   - Single-page and two-page spread viewing modes (`D`).
   - Dual reading directions: Western Left-to-Right and Manga Right-to-Left (`Ctrl + R`) with intelligent spread page pairing.
   - Natural alphanumeric page ordering (`NaturalSortComparer`).
   - Full-screen reading mode (`F11` or double-click) with auto-hiding chrome.

2. **Broad Format Support & Pure .NET Loaders**:
   - Native support for `.cbz`, `.cbr`, `.cb7`, `.zip`, `.rar`, `.7z`, `.pdf`, and extracted image folders.
   - Pure managed RAR4, RAR5, and 7-Zip decompression without external DLLs.
   - PDF rasterization powered by Docnet.Core (PDFium).

3. **Universal CBZ Conversion Engine**:
   - Converts any comic format (folders, CBR, CB7, PDF) to standard `.cbz`.
   - Lossless zero-copy repacking and deterministic zero-padded page naming.
   - Asynchronous background execution with progress and cancellation.

4. **Local SQLite Library Management**:
   - Watched folder auto-monitoring and recursive directory indexing.
   - Quick-add button for single comic files.
   - Dual view modes: Cover Card Grid view and Detailed Table List view.
   - Real-time search and multi-criteria filtering (Favorites, In-Progress, Unread, tags, formats).
   - Rich sorting options (Title, Recently Read, Date Added, Page Count, File Size).
   - High-speed disk-backed cover thumbnail generator and cache.
   - Non-destructive operations (removing library entries never deletes disk files).

5. **Reading Progress & Bookmarks**:
   - Automatic session progress tracking (last page, completion state) in SQLite.
   - Non-intrusive resume reading toast notification on reopen.
   - Interactive seekbar scrubber with live page preview tooltip and counter.
   - Page bookmarking with optional custom notes (`Ctrl + D` / `Ctrl + B`).

6. **Hardware-Accelerated Color Correction & Night Mode**:
   - 256-entry channel Look-Up Table (LUT) color adjustments.
   - Sliders for Brightness (-100 to +100), Contrast (0.5 to 2.0), and Warmth (-100 to +100).
   - One-click amber-tone Night Mode preset with dimmed background.

7. **Consolidated Settings & Comic Details**:
   - Theme switching (System Default, Light, Dark) with Fluent Mica material.
   - Configurable reading defaults, library defaults, and initial color presets.
   - Watched folder management with deletion confirmation dialogs.
   - Cache tools: view cache size, clear cache with confirmation, and one-click library pre-caching.
   - Instant-hover information tooltips (`ℹ`) across all advanced features.
   - Local Comic Details dialog for viewing and editing local comic metadata (Title, Series, Issue, Writers, Artists, Publisher, Release Date, Summary).

8. **Windows Shell Integration & Standalone Installer**:
   - File associations for `.cbz`, `.cbr`, `.cb7`, `.zip`, `.rar`, `.7z`, and `.pdf`.
   - Direct command-line launch support (`Komik.exe "<path-to-file>"`).
   - Standalone Inno Setup installer (`Komik-Setup.exe`) with embedded application icon, Start Menu shortcut, optional Desktop shortcut, and clean uninstaller.
   - 100% offline, privacy-first architecture with zero telemetry or network calls.

9. **Official Marketing & Download Website (`komik-website`)**:
   - Built with Next.js 15 (App Router), TypeScript, Tailwind CSS, and Framer Motion.
   - Distinctive "Nocturnal Graphic Novel" visual aesthetic: authentic ink panel borders, custom halftone screentone patterns, editorial typography, and high-contrast comic color palette.
   - Interactive WinUI 3 Fluent reader canvas mockup:
     - Faithfully recreates the real desktop application with native-style Mica backdrop, custom title bar, and floating acrylic toolbar.
     - Live interactive controls for Fit Width (`W`), Fit Height (`H`), Single Page vs. Two-Page Spread (`D`) with realistic Page 1 cover isolation, Manga Right-to-Left reading direction (`Ctrl + R`), and warm Night Mode LUT color shader.
     - Interactive page scrubber slider with live page preview thumbnails and dual-page indicators.
     - Original illustrated vector comic artwork across all sample pages.
   - Custom high-contrast brand mark (`KomikLogo`) featuring an open comic book crest with amber/gold cover, deep ink spine, and story panels with dynamic shadow effects.
   - Interactive Format Showcase detailing all 8 supported archive and document types (.cbz, .cbr, .cb7, .zip, .rar, .7z, .pdf, image folders) and an interactive Lossless Archive Converter visualizer.
   - Four editorial Feature Panels (Local-First Architecture, Zero-Lag Pure .NET Engine, Smart Spread Pairing, Hardware LUT Night Mode).
   - Offline Manifesto section celebrating local-first software, zero telemetry, and user privacy.
   - Keyboard Shortcut Reference cheat sheet.
   - Responsive design with intelligent mobile preview notice advising visitors of the Windows desktop application focus.
   - Centralized configuration (`lib/config.ts`) linking directly to the latest GitHub Releases installer (`Komik-Setup.exe`) and changelog.

---

## 3. Version 1.1.0 — Comprehensive Release Specification & Manifest

Version 1.1.0 represents a major evolutionary leap for **Komik**, delivering deep library organization, high-throughput reader performance, advanced local analytics, official brand unification, and complete UI responsiveness across all screen sizes.

---

### 1. File Manifest & Architecture Additions (Version 1.1.0)

#### Data Models & Entities
- `Models\ComicSeriesGroup.cs`: Data model for multi-issue series runs, volume groupings, reading order tracking, and 1-click read-next resolution.
- `Models\ManualSeries.cs`: Entity and DTO models for custom user-created series and reading orders with SQLite relational persistence.
- `Models\DuplicateComicGroup.cs`: Data structures representing multi-copy comic sets across formats with resolution options (Keep All vs. Remove from Disk).
- `Models\ReadingSession.cs`: Reading session models tracking timestamps, active duration, page delta, reading streaks, and summary metrics.
- `Models\ReadingPreset.cs`: Look-Up Table (LUT) preset model powering the 6 hardware-accelerated color themes.
- `Models\WebtoonPageItem.cs`: Observable item model for continuous vertical Webtoon pages with dynamic aspect ratio calculation and off-thread decoding.

#### Services & Business Logic
- `Services\DuplicateDetectionService.cs`: Dual-strategy duplicate engine identifying matching file attributes (size + page count) and normalized string similarity $\ge 95\%$.
- `Services\OcrService.cs`: 100% local, offline optical character recognition powered by Windows' native `Windows.Media.Ocr.OcrEngine`.
- `Services\FormatConversionService.cs`: Multi-format comic archive converter transforming CBR, CB7, ZIP, RAR, 7Z, and image folders into standardized CBZ packages.

#### Helpers & Utilities
- `Helpers\SeriesParserHelper.cs`: Heuristic regular expression parser extracting series names, volume indicators, and issue numbers with Levenshtein title distance calculation.

#### Official Branding & Shell Assets
- `Assets\app-icon.png`: Official high-resolution 512×512 master vector crest branding asset.
- `Assets\AppIcon.ico`: Multi-layer Windows shell executable icon (16, 24, 32, 48, 64, 128, 256 px).
- `Assets\Square150x150Logo.scale-200.png`, `Square44x44Logo.png`, `Square44x44Logo.scale-200.png`, `StoreLogo.png`: Windows App SDK shell package assets.

#### Database Migrations & SQLite Schema Updates
- `ManualSeries` table (`id INTEGER PRIMARY KEY`, `title TEXT NOT NULL`, `description TEXT`, `created_at TEXT`): Persistent storage for manual comic series.
- `ManualSeriesComics` table (`series_id INTEGER`, `comic_id INTEGER`, `sort_order INTEGER`, `PRIMARY KEY (series_id, comic_id)`): Many-to-many relationship linking library comics to manual series.
- `ReadingSessions` table (`id INTEGER PRIMARY KEY`, `comic_id INTEGER`, `start_time TEXT`, `duration_seconds INTEGER`, `pages_read INTEGER`): Real-time tracking of reading sessions and daily streaks.
- `IgnoredDuplicatePairs` table (`comic_id_a INTEGER`, `comic_id_b INTEGER`): Safe storage for dismissed duplicate sets.

#### Companion Web Platform (`komik-website/`)
- Built with **Next.js 15**, **React 19**, **Tailwind CSS**, and **Framer Motion**.
- Interactive WinUI 3 Canvas Mockup, Format Showcase, Local-First Manifesto, and Keyboard Shortcut Cheat Sheet.
- Updated for v1.1.0 with dedicated Series & Volumes showcase, Webtoon parallel preloading engine panel, ~60MB self-contained installer specifications, and direct GitHub release links.

---

### 2. Flagship Features Delivered in Version 1.1.0

#### 1. Standalone Dedicated "Series & Volumes" Screen & Management
- **Decoupled from Comic Filters**: "Series & Volumes" is fully separated from comic filter chips into an independent, dedicated library mode accessible via a prominent toolbar button.
- **Dedicated Series Header**: Features "← Back to Comics", series counter badge, "+ New Series" button, series title search bar, and series sorting options (Title A-Z/Z-A, Recently Read, Date Added, Page Count, File Size).
- **Intelligent Auto-Clustering ($\ge 90\%$ Similarity)**: Evaluates all library comics using Levenshtein distance and structural normalization, grouping multi-issue runs and tankōbon volumes when titles match 90% or higher.
- **Custom Manual Series Builder**: Allows users to create custom reading orders, name series runs, search and select library issues via a visual candidate picker, and reorder comics.
- **Add Comics to Existing Series**: Interactive `+ Add Comics` action inside the Series Detail overlay allowing users to search candidates and add any comic from disk directly into existing series (auto-promoting auto-detected runs to permanent SQLite manual series).

#### 2. High-Throughput Parallel Webtoon Continuous Reading Engine
- **Parallel Worker Preloading**: Replaced sequential decoding with a high-throughput 6-worker pool (`SemaphoreSlim(6)`) decoding high-resolution pages concurrently off the UI thread.
- **Outward Decoding Priority**: Decodes outward from the active viewport index, ensuring instant responsiveness without blank page stutters.
- **Smooth Continuous Scroll Navigation**: Restricted navigation to smooth mouse wheel scrolling and keyboard navigation (`Up`, `Down`, `PageUp`, `PageDown`), eliminating erratic pointer panning jumps.
- **View Reset Bugfix**: Resolved the recursive scroll reset loop where `ViewChanged` fired `CurrentPageIndex` updates that snapped scroll position back to the top. Added `ScrollToWebtoonPage` for fluid animated page jumps when using the scrubber or next/previous buttons.

#### 3. Reading Statistics & Komik Wrapped Dashboard
- **Real-Time Tracking**: Every page turn automatically updates session progress and commits to SQLite.
- **Comprehensive Analytics**: Tracks total lifetime reading time (hours/minutes), total pages read across the entire library, completed comic count, active daily streak, longest streak, and 14-day reading velocity.
- **Top 20 Rankings**: Expanded rankings to display the Top 20 most-read comics and Top 20 series runs, with right-aligned progress percentages and duration statistics.
- **Komik Wrapped Yearly Card**: Shareable end-of-year style statistics card celebrating personal reading achievements.
- **Universal Light & Dark Mode Compatibility**: Semantic brushes (`ModalScrimBrush`, `ModalCardBackgroundBrush`, `WrappedCardBrush`, `WrappedCardTextBrush`) dynamically adapt to Windows system theme changes without bleed-through.

#### 4. Duplicate Comics Manager & "Keep All" Dismissal
- **Multi-Factor Detection ($\ge 95\%$ Match)**: Detects duplicate copies across different file formats (e.g. `.cbz` vs. `.pdf` vs. `.cbr`), matching byte counts, page numbers, and normalized title similarity $\ge 95\%$.
- **Clear Resolution Actions**: Users can inspect side-by-side file paths, page counts, and sizes, delete unwanted duplicates permanently from disk, or click **Keep All** to dismiss duplicate warnings and preserve both copies.

#### 5. 100% Offline Windows Native OCR Engine
- **Zero Cloud APIs**: Uses Windows' built-in `Windows.Media.Ocr.OcrEngine` shipping directly with Windows 10 and 11. Completely functional in air-gapped environments.
- **Dialogue Search Flyout**: Search comic dialogue across scanned pages with live occurrences and 1-click jump to page.
- **Selectable Text Overlay Layer**: Interactive card positioned cleanly below the toolbar enabling readers to select, highlight, and copy transcribed text directly from artwork panels.

#### 6. Multi-Format Archive-to-CBZ Lossless Converter
- Integrated standalone converter accessible from the Library toolbar dropdown.
- Converts single comic files (CBR, CB7, PDF, etc.) or raw extracted image folders into standard CBZ archives without recompression loss.

#### 7. Library Top Bar Usability & Window Responsiveness
- **Combined "ADD" DropDown Button**: Merged separate "Add Folder" and "Add Comic" buttons into a single high-contrast Accent "ADD" button with an interactive dropdown flyout, conserving over 100px of toolbar width.
- **Logo Cleanup in Library**: Removed the redundant logo image from the Library header next to the title, keeping the official vector crest cleanly featured in the Settings About section.
- **Horizontal Mouse Wheel Scrolling**: Added `PointerWheelChanged` handling on both the Library toolbar and Filter Pills `ScrollViewer` elements, allowing smooth horizontal scrolling with the standard mouse wheel on any window size.
- **Responsive Layout**: Re-proportioned search boxes, sort combos, and stack panels with dynamic `ActualWidth` binding so all library controls remain fully accessible on both maximized widescreen and compact windowed screens without clipping.

#### 8. Official Vector Crest Brand Synchronization
- Synchronized the official vector crest logo from `komik-website/public/app-icon.png` across:
  - All Windows App SDK package assets (`Assets/Square150x150Logo.scale-200.png`, `Square44x44Logo.png`, `Square44x44Logo.scale-200.png`, `StoreLogo.png`).
  - Windows executable and shell icons (`Assets/AppIcon.ico`).
  - Settings page About section (`ms-appx:///Assets/Square150x150Logo.scale-200.png` at 36×36).
  - Inno Setup installer branding and uninstaller registration.
  - Companion website (`komik-website/`) hero badges, navigation branding, and vector logo components.

#### 9. Window Size, Position & State Persistence
- Automatically persists window width, height, screen coordinates, and maximized state across restarts in SQLite `AppSettings`, seamlessly restoring upon launch.

#### 10. Portable JSON Library Backups (`.komikbackup`)
- Full export and restore of library metadata, reading history, bookmarks, notes, favorites, tags, and settings with conflict-free import logic.

---

### 3. Automated Verification, Build & Delivery

1. **Automated Unit Test Suite**:
   - Comprehensive test suite in `Komik.Tests/Program.cs` covering `SeriesParserHelper`, `DuplicateDetectionService`, `ColorCorrectionHelper` LUT presets, `LibraryRepository` geometry persistence, reading stats/sessions, backup export/import, and manual series creation/retrieval/deletion.
   - **All 27 automated unit tests PASSED (27 passed, 0 failed)**.

2. **Self-Contained Release Compilation**:
   - Built self-contained win-x64 binary package bundling .NET 8 runtime and Windows App SDK:
     `dotnet publish Komik.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:WindowsPackageType=None -o publish_selfcontained`
   - Preserves all assets in `publish_selfcontained\Assets`.

3. **Inno Setup Windows Installer Package**:
   - Script: `installer/Komik.iss` (Version 1.1.0, modern wizard style, LZMA2/ultra64 solid compression, admin privileges, file associations for `.cbz`, `.cbr`, `.cb7`, `.zip`, `.rar`, `.7z`, `.pdf`).
   - Output: `shipping/Komik-Setup.exe` (~60.0 MB).
   - Silent local installation executed and verified with exit code 0 (`/VERYSILENT /SUPPRESSMSGBOXES /NORESTART`).

4. **Next.js Companion Website Production Build**:
   - Built with `npm run build` in `komik-website/`:
   - 4/4 static pages generated with 0 errors and full TypeScript type validation.

