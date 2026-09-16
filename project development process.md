# Komik — Development Process & File Manifest (Versions 1.0, 1.1.0 and 1.2.0)

This document records the complete manifest of files created for **Komik** Version 1.0, Version 1.1.0 and Version 1.2.0, along with a summary of the features delivered in each release. Version 1.2.0 also introduces the third part of the project: **KomiK Downloader 1.0.0**, a browser extension that lives in `KomiK-Extension/` in the same repository.

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
- `Properties\PublishProfiles\` (local publish profiles, git-ignored)

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
- `Models\ManualSeriesComicItem.cs`: Selectable comic item used when building custom user-created series and reading orders (persisted in the `ManualSeries` / `ManualSeriesComics` tables).
- `Models\DuplicateComicGroup.cs`: Data structures representing multi-copy comic sets across formats with resolution options (Keep All vs. Remove from Disk).
- `Models\ReadingStatsSummary.cs`: Reading statistics summary tracking time read, pages, reading streaks, Top Series and Komik Wrapped highlights.
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
- `ManualSeries` table (`id INTEGER PRIMARY KEY AUTOINCREMENT`, `name TEXT UNIQUE NOT NULL COLLATE NOCASE`, `date_created TEXT NOT NULL`): Persistent storage for manual comic series.
- `ManualSeriesComics` table (`series_id INTEGER`, `comic_id INTEGER`, `sort_order INTEGER NOT NULL DEFAULT 0`, `PRIMARY KEY (series_id, comic_id)`, cascading foreign keys): Many-to-many relationship linking library comics to manual series.
- `ReadingSessions` table (`id`, `comic_id`, `start_time`, `end_time`, `duration_seconds`, `pages_read`, `session_date`): Real-time tracking of reading sessions and daily streaks.
- `IgnoredDuplicates` table (`id`, `comic_id_1`, `comic_id_2`, `date_ignored`, `UNIQUE(comic_id_1, comic_id_2)`): Safe storage for dismissed ("Keep All") duplicate pairs.
- Full schema (13 tables including `Collections` / `ComicCollections`, WAL journal, foreign keys, 6 indexes) is documented in `README.md → Under the Hood`, taken directly from `Services/LibraryRepository.cs`. The durable database location is `%USERPROFILE%\.komik\komik_library.db`, with automatic migration from `%LocalAppData%\Komik`.

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


---

## 4. Version 1.1.0: Website "Living Comic Book" Redesign & README Overhaul

The companion website was rebuilt from the ground up as an interactive comic book, with a fully working replica of the Komik reader holding a complete original comic. The repository README was redesigned in the same visual language, with animated artwork and real app screenshots. No application (C#/WinUI) source files were changed in this pass.

---

### 1. File Manifest (Website Redesign)

#### New: Demo Comic Engine (`komik-website/components/comic/`)
- `kit.tsx`: 600×900 native-SVG page frame, panels, speech/shout/robot/thought balloons, captions, SFX lettering, bursts, speed and motion lines, rain, halftone patterns. Per-instance `useId` scoping lets pages render several times on one screen.
- `art.tsx`: pose-based character rig (Vex, Agent Stratus, Orrin, citizens), portrait close-ups, Byte the drone, enemy drones, skylines, neon signs, the Nimbus blimp, bookshelves, a retro PC and sync rings.
- `pages.tsx`: **Cyberpunk Chronicles #01, "The Last Local Archive"**, an original 12-page story (cover, 10 story pages, back cover). All lettering lives in `SCRIPT`, which also powers the reader's dialogue search. `ComicPage` is memoized.

#### New: Interactive Reader Replica (`komik-website/components/reader/`)
- `ReaderMockup.tsx`: the working WinUI 3 reader replica, covering:
  - page turns (keys, click zones, swipe), cover-isolated two-page spreads, Manga RTL, webtoon mode, fit width/height/1:1, zoom, pan and pinch
  - resume toast, bookmarks with notes, a scrubber with thumbnail previews, dark/light chrome, minimize/close/restore
  - a clip-path animated full-screen overlay with auto-hiding sliding chrome, and compact mobile layout with bottom sheets
- `ReaderPanels.tsx`: Color Correction (6 presets + brightness/contrast/warmth), Bookmarks, offline OCR dialogue search + selectable text overlay, mobile view menu.
- `readerModel.ts`: spread pairing, CSS-filter LUT stand-in, script search helpers.

#### New: Motion & Effects (`komik-website/components/fx/`, `komik-website/lib/`)
- `SmoothScroll.tsx`:
  - Lenis desktop wheel smoothing synced to GSAP ScrollTrigger (native scrolling on touch)
  - a site-wide eased, distance-timed in-page link glide with exact landing correction and menu-safe timing
  - offscreen CSS animation pausing
- `SectionHeading.tsx`: caption box + SplitText character-pop headings.
- `ClickBurst.tsx`: POW!/ZAP! bursts on click.
- `ComicBurst.tsx`: reusable starburst badge.
- `MotionProvider.tsx`: `MotionConfig reducedMotion="user"`.
- `lib/gsap.ts`: GSAP + ScrollTrigger + SplitText + `useGSAP` registration.

#### New Sections
- `StatsStrip.tsx`: "By the numbers" count-up panels (8 formats, 6 decoders, 6 LUT presets, 0 network calls, 0 accounts, 27 test suites).
- `ReadingEngine.tsx`: GSAP-pinned horizontal comic strip (desktop) with five live demos: spread pairing, 6-worker webtoon decoder, before/after LUT slider, self-typing library search, resume & bookmarks.
- `NewInV11.tsx`: paper bento "Special Edition" covering Series & Volumes, Stats & Komik Wrapped, Duplicate Manager, offline OCR, `.komikbackup`, and responsive library & window memory.

#### Rewritten
- `app/layout.tsx`: Bangers + Comic Neue fonts, a CSS-only once-per-session intro splash, updated metadata.
- `app/page.tsx`: new section order wrapped in `MotionProvider`.
- `app/globals.css`: comic design tokens, halftone and speed-line utilities, buttons, balloons, marquees, reader theme variables, Lenis rules, `scrollbar-gutter: stable`, reduced-motion overrides.
- `tailwind.config.ts`: CMYK palette (`#FFD700` / `#00C2FF` / `#FF1F6D`), `font-bangers`, `font-comic`.
- `components/Hero.tsx`: SplitText headline, draggable stickers, speed-line backdrop, reader entrance, crossing marquee tapes.
- `components/Navbar.tsx`: active-section pill, CMYK progress bar, comic-tile mobile menu (tiles close the menu then glide to their section; Esc closes).
- `components/FormatShowcase.tsx`: 8 collectible 3D-tilt, flippable format cards + animated "CBZ-O-Matic 3000" converter.
- `components/ManifestoSection.tsx`: torn-newsprint editorial with GSAP stamp slams and drawn underline.
- `components/KeyboardSection.tsx`: press real keys or tap keycaps to trigger SFX + shortcut descriptions.
- `components/DownloadSection.tsx`: sunburst final panel, burst CTA, spec cards, SmartScreen note.
- `components/Footer.tsx`: "THE END" footer with a floating Byte.
- `DESIGN.md`: full design, motion, reader, performance and README-artwork documentation.

#### Removed
- `components/FeaturePanels.tsx`, `components/MobileNotice.tsx`, `components/MockComicPages.tsx` (replaced by the sections above).

#### New Dependencies (`komik-website/package.json`)
- `gsap` ^3.15.0, `@gsap/react` ^2.1.2, `lenis` ^1.3.26 (Framer Motion 12 was already present).

#### README Artwork (`komik-website/public/readme/`)
- Animated SVG (pure SVG + CSS keyframes, reduced-motion aware): `banner.svg`, `stats.svg`, `divider.svg`, `the-end.svg`, and ten chapter headers `section-*.svg`.
- Desktop app screenshots: `app-reader.jpg`, `app-library.jpg`, `app-webtoon.jpg`, `app-series.jpg`, `app-stats.jpg` (re-captured for the redesign in section 5, which also adds `app-series-detail.jpg`, `app-tags.jpg` and `app-settings.jpg`). Captured from the installed Komik 1.1.0 build reading the demo comic packed into real `.cbz` files, using a temporary isolated library so the developer's own library was never modified.
- Website screenshots: `screenshot-hero.jpg`, `screenshot-reader.jpg`, `screenshot-formats.jpg`, `screenshot-engine.jpg`, `screenshot-new.jpg`, `mobile-screen-hero-section.PNG`, `screenshot-mobile-reader.jpg`.

---

### 2. Experience Highlights

1. **Readable demo comic in the hero:** the complete 12-page story reads end to end inside the mockup, with every toolbar feature working and vector pages sharp at any zoom.
2. **Mobile-ready reader:**
   - compact toolbar with bottom sheets
   - swipe to turn, pinch to zoom
   - one-page fit sizing so vertical swipes still scroll the site
   - animated full screen
3. **Smooth motion:**
   - reader fade-in after measuring (no layout jump) and a one-shot entrance
   - clip-path grow/shrink full screen with sliding chrome, and minimize/close crossfades without remounting
4. **Smooth navigation:** eased, distance-timed glides for every navbar link, mobile menu tile and CTA, landing exactly below the header.
5. **Performance:**
   - offscreen animation pausing and `useInView`-gated demo loops
   - memoized SVG pages, gradient glows instead of CSS blur, and GPU-layered sunbursts
6. **Accessibility:** reduced-motion support across GSAP, Lenis, CSS and Framer Motion; labelled controls; decorative layers hidden from assistive tech.

---

### 3. Verification

- `npx tsc --noEmit`: no type errors.
- `npm run build` (Next.js 15.5): compiled successfully, and all static pages were generated.
- Headless Chrome (puppeteer-core) interaction checks:
  - desktop: keyboard navigation, spreads, RTL, webtoon, zoom, bookmarks, search, color presets, full-screen enter/exit animation, minimize/restore, scrubber drag + hover preview, wheel hand-off between reader and page
  - mobile (390×844 touch): full-page touch scrolling, horizontal swipe page turns, tap zones, bottom sheets, menu tile navigation
  - all navbar links and the "finish reading" link land within a few pixels of their targets, with no console errors
- `README.md`:
  - rewritten with animated artwork and real app screenshots
  - corrected SQLite schema (13 tables) and test inventory (27 suites in `Komik.Tests/Program.cs`)
  - fixed the Docnet.Core acknowledgement link

---

## 5. Version 1.1.0: Comic-Style App Redesign, Library Intelligence & Polish

The desktop app was brought into the same comic-book visual language as the website, and the library learned to understand what each comic *is*: its series, its creators and its number. This pass then polished every screen touched by that work (series, tags, reader, settings, duplicates and stats) based on hands-on testing. It is still **version 1.1.0**: no data migration is needed and existing libraries upgrade in place.

---

### 1. File Manifest (Redesign & Polish)

#### New: Views, Dialogs & Controls
- `Controls\ComicToast.xaml` / `.xaml.cs`: floating comic-style notification that pops in, counts down, pauses on hover and restarts when a new message arrives.
- `LibraryPage.Dialogs.cs`: series, tag and comic dialogs of the library page (edit series, change cover, add comics, manage tags, comic details).
- `Themes\ComicControls.xaml`: shared comic styles: panels, stickers, pills, section titles, buttons and toggle tiles.
- `Assets\Fonts\Bangers-Regular.ttf` + `Bangers-OFL.txt`: display lettering (SIL Open Font License).
- `Assets\Textures\halftone.png`, `halftone-corner.png`, `speedlines.png`: print textures for panels, headers and empty screens.

#### New: Helpers
- `Helpers\ComicIdentityParser.cs`: reads a comic's series, credited creators, volume, issue, chapter, part and year from its name or metadata. Series grouping, duplicate detection and reading statistics all share it, so they always agree.
- `Helpers\MotionHelper.cs`: Composition animations (hover lift and tilt, pop-in reveal, press squash) that respect the Windows *Animation effects* setting.
- `Helpers\ComicDialogXaml.cs`: builds comic-styled dialog content from XAML so `{ThemeResource}` brushes follow light and dark exactly like the pages.
- `Helpers\WrapPanel.cs`: wrapping layout for action bars on narrow windows.
- `Helpers\ValueConverters.cs`: adds `HexToBrushConverter` for per-filter accent colours.

#### New: Services
- `Services\SeriesDetectionService.cs`: builds both views of a library: **Continuation Series** (issues, volumes, sequels, typo-tolerant title merging, gap detection) and **By Creator** collections, then layers manual series on top.
- `Services\ReadingSessionTracker.cs`: counts only active reading time (idle gaps are capped) and the distinct pages actually viewed.
- `Services\ReadingStatsCalculator.cs`: pure, time-zone-aware statistics: local calendar days, streaks, periods and page-accurate series progress.
- `Services\FileDeletion.cs`: deletes comic files without Windows prompts: Recycle Bin first, retried while a lingering file handle closes, then a permanent delete as the last resort.
- `Services\OcrLayout.cs`: groups recognized lines into speech bubbles in reading order (left to right, or right to left for manga), tiles tall webtoon pages, and filters artwork noise while keeping real sound effects.

#### New: Models & ViewModel Partials
- `Models\EditSeriesComicItem.cs`: one comic in the Edit Series screen (position, cover flag) plus `TagChipItem` (a tag with its usage count).
- `Models\UnindexedComicItem.cs`: a comic found in a watched folder that is not in the library (never added, or removed earlier).
- `ViewModels\LibraryViewModel.SeriesEdit.cs`: Edit Series state: rename, section, auto-update, reading order, cover, removals and save.
- `ViewModels\LibraryViewModel.Tags.cs`: tag filter, a comic's tags and tag management, built to stay quick with hundreds of tags.

#### Significantly Updated
- `App.xaml`, `MainWindow.xaml(.cs)`: comic theme resources for light and dark; the app now opens in the **light theme by default**.
- `LibraryPage.xaml(.cs)`: comic cover grid, Series screen, series detail banner, empty screens, active filter sticker.
- `MainPage.xaml(.cs)`: reader chrome, animated webtoon toolbar, live zoom percentage, saved-page anchoring.
- `SettingsPage.xaml(.cs)`, `ViewModels\SettingsViewModel.cs`: instant-apply settings, page colours, *How Comics Open*, *Add Back Removed Comics*.
- `Services\DuplicateDetectionService.cs`, `Models\DuplicateComicGroup.cs`: confidence levels, reasons and a recommended copy.
- `Services\LibraryRepository.cs`, `Services\LibraryScannerService.cs`, `Services\OcrService.cs`, `Models\ReadingStatsSummary.cs`, `Models\ComicSeriesGroup.cs`, `Models\AppSettings.cs`, `ViewModels\LibraryViewModel.cs`, `ViewModels\MainViewModel.cs`.

#### Database Updates (automatic, in place)
- `ManualSeries` gains `kind` (`story` / `creator`), `auto_update` and `source_key` columns.
- New `ManualSeriesExclusions` table: comics the user removed from an auto-updating series stay out.
- New `RemovedComics` table: comics removed from the library are remembered, skipped by rescans and can be added back from Settings.
- New settings keys: `DefaultReaderViewMode` (`SinglePage`, `DoublePage`, `Webtoon`) and a one-time light-theme migration flag.

---

### 2. Features & Fixes

#### Comic-style interface
- Halftone panels, ink borders, stickers and Bangers lettering across the library, reader, settings, dialogs and toasts, in both light and dark themes.
- Cards lift and tilt toward the pointer; panels pop in; buttons squash on press. Dropdowns open instantly (no press delay on buttons that open a menu).
- Flyouts stay inside the window, and the white halftone circles on comic cards were removed.

#### Series & Volumes
- Two sections: **Continuation Series** and **By Creator**. Detection understands issues, volumes, chapters, sequels (*II*, *2*, *Zenpen / Kouhen*), typos and missing numbers.
- Right-click menu on every comic inside a series (read, mark read / unread, set as cover, details, tags, remove from series).
- **Change series cover** from any comic in the series.
- **Add Comics** picker with search and filters. Creator collections only offer comics that are not already in another creator collection.
- A larger **New Series** screen and a full **Edit Series** screen: rename, choose the section, auto-update, drag-and-drop reading order, automatic ordering, cover and removals.
- **Section rule:** a *By Creator* collection can be moved to *Continuation Series*, but a continuation series can never be moved to *By Creator*. The By Creator option is locked with an explanation, and moving a creator collection warns that it can't be moved back.
- Fixed the series detail banner: the blurred cover is painted as a background so it can no longer stretch the layout at any window size.

#### Tags
- Manage Tags screen with search, sort (name, most used, least used, newest), usage counts, rename, merge and delete, sized for libraries with many tags.
- A comic's tags screen shows chips with instant toggle, search and "press Enter to create".

#### Library filters & empty screens
- Every empty filter, tag, search and series view now shows a comic panel with a rocking starburst in that filter's colour, a sticker (*EMPTY TAG*, *NO MATCH!*, *ALL READ!*…), a clear title and two actions: *Show All Comics* and a context action (Manage Tags, Clear Search, Browse Unread, New Series…).
- The active filter or tag appears as a coloured sticker with its kind, name, matching comic count and a clear button, and the Tags button turns yellow while a tag is selected.

#### Reader
- **How Comics Open** (Settings → Reading): page by page, two-page spread or webtoon, combined with comic (left to right) or manga (right to left). A summary line describes the choice, and it applies to every comic you open.
- Comics that open straight into webtoon mode now land on their saved page instead of the first page.
- Zooming in or out in webtoon mode keeps the page being read in place (it used to slide far down the strip).
- Webtoon toolbar hides and shows with an animation while scrolling; webtoon keys and buttons work consistently; the zoom percentage updates live.
- **Advanced offline OCR:** speech bubbles are grouped and ordered the way a reader's eye moves, tall pages are tiled for accuracy, and texture noise (e.g. `rrrwruur`) is filtered while sound effects like `BZZT` are kept.

#### Duplicates, stats & settings
- Redesigned duplicate dialogs with confidence, reasons, a recommended copy and space saved.
- Fixed "file is in use" failures when deleting a duplicate from disk.
- **Top Series** redesign with book counts, last read time and an exact page-based percentage (for example 44% instead of 0% or 100%). Hovering the bar shows "X of Y pages".
- Settings apply instantly, including page background colours, and a new **Add Back Removed Comics** screen lists comics in watched folders that are not in the library.

#### Versioning & installer
- Removed the unused `Assets\LockScreenLogo.scale-200.png` template asset (not referenced by the app or `Package.appxmanifest`).
- `Komik.csproj` (`Version`, `AssemblyVersion`, `FileVersion`), `Package.appxmanifest`, the backup format, the in-app badges and `installer\Komik.iss` all report **1.1.0**.
- `Komik-Setup.exe` now carries Windows version information (**1.1.0.0**, product name, publisher, description and copyright) instead of showing 0.0.0.0 under *Properties → Details*.

---

### 3. Verification

- Automated tests: **41 passed, 0 failed** (`cd Komik.Tests; dotnet run -c Release`), including the new suites:
  - `ComicIdentityParser` parses series, volumes, issues, chapters and years
  - `SeriesDetectionService` groups, orders, merges typos, finds gaps, continuations and creator collections
  - `DuplicateDetectionService` scores copies, avoids false positives and recommends the best copy
  - `ReadingSessionTracker` active time with idle cap and distinct pages
  - `ReadingStatsCalculator` local days, streaks, periods, real series totals and page-accurate Top Series percentages
  - manual series: unique names, creator section, auto-update, exclusions, rename and going back to automatic
  - removed comics remembered, skipped by rescans and added back
  - tags: usage counts, rename, merge and delete
  - OCR layout: bubble grouping, reading order, tall-page tiling and noise filtering
- UI checks on an isolated demo library (`KOMIK_DATA_DIR`), never the real library: series detail at 1440×900 and 960×680, edit series section lock, empty tag / filter / search screens, active filter sticker, How Comics Open tiles, webtoon opening at the saved page, Top Series percentages.
- Release build: self-contained win-x64 publish, then `ISCC installer\Komik.iss` → `shipping\Komik-Setup.exe`, installed and launched successfully.
- README screenshots re-captured from this build with the original mock comics (see `komik-website/public/readme/app-*.jpg`).

---

## 6. Version 1.2.0: Embedded Metadata, Every Tag & Reader Polish

Komik 1.2.0 teams the desktop app up with the new KomiK Downloader extension (section 7). The library now reads the details file that comics carry inside them, keeps every tag, refreshes comics that are saved again, and fills itself from watched folders as downloads finish. Existing libraries upgrade in place; a one-time pass imports embedded details for comics that were indexed before.

---

### 1. File Manifest (Version 1.2.0)

#### New: Services
- `Services\ComicInfoReader.cs`: finds and parses `ComicInfo.xml` (the ComicRack / Anansi schema) inside CBZ/ZIP, CBR/RAR, CB7/7Z archives and image folders. Only that one entry is read, never the pages, with DTD processing prohibited and a 2 MB size cap. Returns `EmbeddedComicInfo`: title, series, number, volume, summary, year/month/day, writers, artists (penciller, artist, inker, colorist, cover artist), publisher, web, language, manga direction and tags (genres and tags together, de-duplicated, **no count limit**). `ResolveLibraryTitle` keeps the file name when an embedded title is only a chapter name that doesn't mention its series.
- `Services\WatchedFolderMonitor.cs`: a debounced `FileSystemWatcher` over every watched folder. Created, renamed and changed files are queued, waited on until their size is stable and the file can be opened (so partial `.crdownload` files are never indexed), then indexed or refreshed. Raises `ComicsAdded` so the library reloads.

#### Updated: Services & Helpers
- `Services\LibraryScannerService.cs`:
  - `ApplyEmbeddedInfoAsync`: saves embedded metadata only when the comic has none yet, and only ever **adds** tags.
  - `ImportEmbeddedMetadataAsync(force)`: one-time back-fill for libraries indexed before 1.2.0, guarded by the `EmbeddedComicInfoImported` setting.
  - `IndexNewSourcesAsync` / `IndexOrRefreshSourcesAsync`: quietly index finished downloads, skip removed comics, and **re-read a known comic whose file changed** (size or modified time) so tags it gained are added. Reading progress, favourites and user edits are untouched.
  - `RefreshChangedComicsAsync`: the same refresh across the whole library at start-up, for files replaced while Komik was closed.
  - `SyncWatchedFoldersQuietlyAsync`: indexes comics added while Komik was closed, runs the one-time import, then refreshes changed files.
- `Services\ILibraryScannerService.cs`: adds `IndexOrRefreshSourcesAsync` returning both added and refreshed counts.
- `Helpers\ComicIdentityParser.cs`: new `StripLabelPrefix` drops a leading category label (`original - [Circle (Artist)] Title`) before titles are compared or creators read. The label is kept when it could be the title itself (more than three words, a year or release tag after the dash, or nothing after the credit).
- `Helpers\MotionHelper.cs`: `SwapContentAsync(element, change)` fades, scales and slides content out (150 ms), applies the change, waits for layout, then rises the new layout in (320 ms), honouring the Windows *Animation effects* setting.

#### Updated: Views
- `MainPage.xaml.cs`: `SwitchReadingModeAsync` runs spread and webtoon switches (toolbar buttons and the `D` / `V` keys) through `SwapContentAsync`, guarded so rapid presses can't overlap.
- `SettingsPage.xaml.cs`: *Cache All Covers*, *Clear Cache* and *Remove Watched Folder* dialogs rebuilt with `ComicDialogXaml` as comic panels, with path and "your files stay safe" notes.
- `MainWindow.xaml`: title-bar crest enlarged from 18 px to 30 px, title text 13 px semibold.

#### Updated: Assets, Manifest & Installer
- `Assets\AppIcon.ico`: rebuilt as a multi-size PNG icon (16, 24, 32, 48, 64, 128, 256 px) with the crest filling 96% of each size, so the taskbar icon is no longer tiny. `Square44x44Logo*.png`, `StoreLogo.png` and `Square150x150Logo.scale-200.png` regenerated to match.
- `Komik.csproj`, `Package.appxmanifest`, `Models\LibraryBackupData.cs` and the in-app version badges report **1.2.0**. `Package.appxmanifest` publisher display name set to *Mohit Bansal* (was the template's *AppPublisher*).
- `installer\Komik.iss`: version 1.2.0, shows the MIT `LICENSE` page, and `CloseApplications=force` closes a running Komik before files are replaced.

#### Removed
- `Models\TagAndCollection.cs`: `TagItem` and `CollectionItem` were never referenced anywhere.

#### Updated: Tests
- `Komik.Tests\EmbeddedMetadataTests.cs` (new in this release) and `Komik.Tests\LibraryIntelligenceTests.cs`.

---

### 2. Features & Fixes

1. **Reads what's inside:** ComicInfo.xml details and tags are imported when a comic is added, and back-filled once for older libraries. The embedded title becomes the library title. Metadata typed in Comic Details or restored from a backup is never replaced.
2. **Every tag, no limit:** the reader used to keep at most 60 tags, and a comic saved by an early extension build carried 30. All tags in the file are now imported (only values longer than 128 characters are skipped as not being real tags).
3. **Watched folders fill the shelf:** finished downloads are indexed within seconds, and comics added while Komik was closed are picked up on the next start, with details and tags.
4. **Downloaded again? Refreshed.** Diagnosed from a real library: a comic first saved with 30 tags and later re-downloaded with 34 kept its 30 tags, because a known path was never read again. Komik now compares the stored size and modified time with the file on disk and re-reads changed comics, both when a watched-folder event arrives and at start-up.
5. **Sharper duplicate finder:** `original - [Circle (Artist)] Title [English] [Digital]` and the same name without the label are now grouped as duplicates, while different parts of a series by the same circle (for example *Zenpen* and *Kouhen*) stay separate.
6. **Silky mode switch:** switching between two-page spreads and webtoon mode animates instead of jumping.
7. **Comic-style Settings dialogs** and a **larger, sharper app icon** in the taskbar and title bar.

---

### 3. Verification

- Automated tests: **48 passed, 0 failed** (`dotnet run --project Komik.Tests/Komik.Tests.csproj`), including:
  - `ComicInfoReader` parses metadata, credits, dates and tags
  - ComicInfo title resolution keeps the series in the library title
  - scanner indexes embedded metadata and tags from CBZ, CB7 and folders
  - embedded metadata import never overwrites edits and runs once
  - new downloads in watched folders are indexed, removed comics stay out
  - a comic downloaded again under the same name gets all its new tags (29 + genre → 80 + genre, unchanged files are not re-read, and the start-up sync catches files replaced while closed)
  - CBZ conversion keeps the embedded ComicInfo.xml
  - duplicate detection groups label-prefixed copies and keeps different parts apart; `StripLabelPrefix` guards (year, release tag, long title, nothing after the credit)
- Tag-limit diagnosis against real files: the downloaded CBZ held 34 tags and Komik's own `ComicInfoReader` (run in a scratch console against that file) returned all 34, while the library database held the 30 from the earlier download, which pinned the cause on the missing refresh.
- Release build: self-contained win-x64 publish, then `ISCC installer\Komik.iss` → `shipping\Komik-Setup.exe` (72.7 MB, version 1.2.0).
- Clean-up for release: removed build output and test leftovers (`bin`, `obj`, `publish_selfcontained`, `Samples`, `Komik.Tests\bin`, `Komik.Tests\obj`, `KomiK-Extension\test-results`, `KomiK-Extension\tests\.tmp`), all of which are regenerated by building or testing.

---

## 7. KomiK Downloader 1.0.0: The Browser Extension

KomiK Downloader is a Manifest V3 extension for Chrome, Edge and Brave. It finds the comic, manga or webtoon on the current page, downloads every page, and saves a CBZ, ZIP, PDF or folder with a `ComicInfo.xml` that Komik 1.2.0 reads, into `Downloads/KomiK/<Series>/` by default. It lives in `KomiK-Extension/` in this repository and is versioned **1.0.0**, paired with Komik **1.2.0**.

---

### 1. File Manifest (`KomiK-Extension/`)

#### Build, Configuration & Tooling
- `.gitignore`: ignores `node_modules`, `dist`, `release`, zip/crx files, signing keys and test output.
- `package.json` / `package-lock.json`: scripts `build`, `watch`, `typecheck`, `test`, `test:e2e`, `package`, `store`, `icons`.
- `tsconfig.json`, `vitest.config.ts`, `playwright.config.ts`, `tailwind.config.cjs`, `postcss.config.cjs`.
- `scripts\build.mjs`: Vite (Rolldown) multi-build: the extension pages as ES modules, the service worker and content scripts as self-contained IIFE bundles, then writes `manifest.json` from `src\manifest.mjs`.
- `scripts\package.mjs`: zips `dist\` into `release\komik-downloader-<version>.zip` (no source maps) and adds `THIRD-PARTY-NOTICES.txt`, generated by walking the runtime dependency tree and collecting each package's license (fonts OFL-1.1; React, Framer Motion, fflate, pdf-lib and their dependencies MIT / 0BSD / Zlib).
- `scripts\store-assets.mjs`: builds the Chrome Web Store images into `release\store\` from the real extension: `icon-128.png` (96 px artwork in 16 px padding), five 1280×800 screenshots, `promo-small-440x280.png` and `promo-marquee-1400x560.png`. It serves a made-up showcase comic with generated art, drives the popup, side panel and options page in Chromium, and composes the frames.
- `scripts\icons.mjs`: renders the toolbar icons from the app crest (`Assets\app-icon.png`), trimmed so the crest fills 96% of each icon.
- `src\manifest.mjs` / `src\manifest.d.mts`: the manifest (name, 132-character description, `minimum_chrome_version` 116, action, side panel, options page, service worker, the always-on sentinel content script, commands and web-accessible fonts). Permissions: `downloads`, `downloads.open`, `storage`, `unlimitedStorage`, `scripting`, `tabs`, `contextMenus`, `notifications`, `offscreen`, `sidePanel`, `declarativeNetRequestWithHostAccess`, `webRequest`, `alarms`, plus `<all_urls>` host access. Every permission was verified to be used.
- `public\icons\icon-16/32/48/128.png`, `public\fonts\Bangers-Regular.woff2`, `public\fonts\Bangers-OFL.txt`.

#### Background (service worker)
- `src\background\index.ts`: message routing, install/start-up (settings migration, context menus, auto-resume), context menu and keyboard commands (`Alt+K`, `Alt+Shift+K`), toolbar badge, notifications with Open / Show in folder, heartbeat alarm.
- `src\background\scan.ts`: injects the on-demand engine, scans tabs with a one-minute cache per tab, remembers picked pages, clears a tab's cache when it closes, and scans chapter pages in hidden background tabs.
- `src\background\jobs.ts`: turns requests into persisted jobs (`queueJobs`, `queueChapters`, `quickDownload`).
- `src\background\downloads.ts`: `chrome.downloads` save and completion wait.
- `src\background\offscreen.ts`: creates and talks to the offscreen engine.
- `src\background\network.ts`: `webRequest` observer that remembers images a tab loaded, for script-driven readers.
- `src\background\referer.ts`: `declarativeNetRequest` session rules that send the page as Referer for hotlink-protected images.

#### Content scripts
- `src\content\sentinel.ts`: always-on and lightweight: counts likely comic images for the badge and floating crest button, shows toasts. Stands down cleanly when the extension is reloaded or updated.
- `src\content\alive.ts`: `extensionAlive()` and `sendSafely()`, so scripts left in open tabs after an update never throw *Extension context invalidated*.
- `src\content\engine.ts`: injected on demand: runs the scanner (with auto-scroll for lazy readers), fetches images from inside the page when needed, and the click-to-pick page picker.
- `src\content\ui.ts`: closed Shadow-DOM UI for the floating button, toasts and picker bar.

#### Detection & Metadata Engine (`src\engine\`)
- `scan.ts`: runs every detector and merges one `DetectResult` (pages, chapters, metadata, pagination, confidence).
- `adapters\index.ts`: reader-theme adapters (WP Manga, TS reader, app-data readers) and user **site rules** with CSS selectors.
- `detect\harvest.ts`: collects candidate images from `img`/`srcset`/lazy attributes, backgrounds, links and inline scripts.
- `detect\cluster.ts`: picks the reader column by container and size, fills numbered sequence gaps.
- `detect\gallery.ts`: gallery grids (numbered page links with previews), keeping each preview and its reader page.
- `detect\quality.ts`: full-size candidates for preview images and the learn-once preview → full-size rule.
- `detect\chapters.ts`: chapter/episode/issue lists with numbers, volumes and dates, and one-page-per-URL pagination.
- `metadata\scrape.ts`: JSON-LD, Open Graph, labelled fields (definition lists, tables, inline labels and label/value grids), tag links across Tags, Genres, Categories, Parodies, Characters and Groups rows, and ages such as "5 years 6 months ago" turned into a date.

#### Offscreen Engine (`src\offscreen\`)
- `engine.ts`: the job pipeline: resolves chapters (fetched HTML, then a hidden tab for script-rendered readers), learns full-size URLs from one reader page, downloads pages in parallel, packs and saves, writes history, and pauses, resumes, retries and cancels. Jobs and pages persist in IndexedDB so work resumes after a restart.
- `fetcher.ts`: page fetching with per-host/global connection limits, retries, timeouts, quick candidate probes, reader-page fallback, in-page fetch fallback and image conversion (AVIF/JXL/HEIC → JPEG).
- `limiter.ts`: connection limiter and speed meter.
- `pack.ts`: CBZ/ZIP with pages stored and `ComicInfo.xml`, PDF with document info, cover thumbnails.
- `save.ts`: saves through the downloads API or a chosen folder.

#### Shared (`src\shared\`)
- `types.ts`, `messages.ts` (typed messaging), `settings.ts` (defaults, clamping, versioned migration), `db.ts` (IndexedDB), `comicinfo.ts` (ComicInfo.xml writer), `identity.ts` (port of Komik's `ComicIdentityParser` rules), `naming.ts` (folder/file templates and Windows-safe paths), `tags.ts` (tag and credit cleaning), `dates.ts`, `enrich.ts` (final metadata and series memory), `html.ts` (parses fetched pages without loading their scripts), `imageinfo.ts`, `util.ts`.

#### Pages & UI (`src\pages\`, `src\ui\`)
- `pages\popup.html/.tsx`, `pages\sidepanel.html/.tsx`, `pages\options.html/.tsx`, `pages\offscreen.html/.ts`.
- `ui\views\ScanView.tsx` (Pages, Chapters and Details tabs, format picker, download button), `ScanParts.tsx` (page grid and chapter list), `MetadataEditor.tsx`, `QueueView.tsx`, `HistoryView.tsx`.
- `ui\components\Brand.tsx`, `Controls.tsx`, `Icon.tsx`, `RemoteImage.tsx` (loads previews through the background when a site refuses them); `ui\hooks\index.ts`; `ui\sound.ts`; `ui\styles.css`.

#### Tests
- `tests\unit\engine.test.ts`, `tests\unit\shared.test.ts` (Vitest, happy-dom).
- `tests\e2e\extension.spec.ts`: launches real Chromium with the built extension over the DevTools protocol and drives it end to end.
- `tests\e2e\fixture-server.mjs`: a local comic site with lazy readers, a series page, hotlink protection, galleries whose full-size pages can or can't be guessed, and a script-heavy chapter. All comics and names are made up and pages are generated images.

---

### 2. Features

1. **Finds the comic on any site:** universal scanner, lazy-load auto-scroll with scroll restore, webtoon strips, one-page-per-URL readers, script-rendered readers (hidden tab), network-observed images, reader-theme adapters, user site rules and a click-to-pick fallback.
2. **Gallery sites at full size:** each page's preview is shown in the grid, but the saved file gets the full-size image. The naming rule is learned from a single reader page and applied to all pages, with guesses and each page's reader page as fallbacks.
3. **Whole series:** chapter lists with numbers, volumes and dates, select all / range / search, each chapter queued as its own comic in one batch.
4. **Details Komik reads:** ComicInfo.xml with title, series, number, volume, chapter title, writers, artists, publisher, year/month/day, summary, language, manga direction, age rating, web link, tags and per-page info. Tags are cleaned (menu words dropped, counts such as `1,234` / `12.4K` stripped without breaking names such as *Iron Orchard 2*, capitals fixed, duplicates merged) and **no tag limit** by default. Credits drop counts (`rio pen 48` → `Rio Pen`). Series memory re-applies a user's corrections to later chapters.
5. **Downloads:** parallel per host and globally, retries, timeouts, hotlink Referer, page conversion for formats Komik can't open, resume after restart, pause / resume / retry / cancel, notifications.
6. **Output:** CBZ (stored pages), ZIP, PDF or folder; folder and file templates with `{series}`, `{title}`, `{volume}`, `{chapter}`, `{number}`, `{issue}`, `{chaptertitle}`, `{year}`, `{site}`, `{publisher}`, `{author}`, `{writer}`, `{artist}`, `{language}`, `{pages}`; page-number padding; optional Save As dialog.
7. **Interface:** comic-style popup, side panel (queue, this page, history) and options page (Folder & names, Formats & pages, Speed, Details & tags, Page scanning, Site rules, Look & feel, Data, Komik app); light paper theme by default, dark theme, sounds, reduced motion; floating crest, badge, right-click menu and shortcuts.
8. **Privacy:** no accounts, analytics, remote code or servers. Settings, queue and history stay in the browser.

---

### 3. Hardening During Development

- **Content Security Policy violations:** parsing fetched chapter HTML with `DOMParser` let Chrome's preload scanner request the page's scripts and stylesheets from the offscreen document. `src\shared\html.ts` strips external scripts, stylesheets and frames before parsing; an end-to-end test watches the engine's console over DevTools and asserts zero violations.
- **Gallery previews and blank pages:** previews now come from the site's own thumbnails, full-size URLs are learned once, and `RemoteImage` falls back to fetching through the background when a site refuses an image on an extension page.
- **Tag cap:** settings saved by early builds (a limit of 30, shown as 29 tags plus one genre) are migrated to "no limit" by a versioned settings migration, and settings keys from removed features are dropped.
- **Label/value grids:** a page laying out `<span>Author</span><span>…</span>` pairs in one box made every field swallow the whole box (credits appeared as tags). An inline label now owns only what follows it, up to the next label.
- **Extension context invalidated:** the sentinel called into the extension from tabs left open across an update. `alive.ts` guards every call and the sentinel shuts itself down; a test reloads the extension under an open page and reproduced the original error before the fix.
- **Site file downloads:** a *Files* tab that saved and re-tagged a site's own CBZ/PDF downloads was built, then removed at the user's request along with its settings, leaving Pages, Chapters and Details untouched.

---

### 4. Verification

- `npm run typecheck`: no errors.
- `npm test`: **58 passed** (universal detection, adapters, metadata scraping incl. grids and ages, chapters and pagination, galleries and quality rules, identity, naming, tags and counts, tag limits and settings migration, ComicInfo.xml, HTML parsing, images and utilities).
- `npm run test:e2e`: **9 passed** in real Chromium: lazy reader to tagged CBZ, background batch chapters, hotlink Referer, full-size galleries (guessable and learned), settings changing the saved file, zero CSP violations, options and history pages, and no errors in tabs left open while the extension reloads.
- `npm run package`: `release\komik-downloader-1.0.0.zip` (41 files, ~700 KB) with `THIRD-PARTY-NOTICES.txt`.
- `npm run store`: store icon, five 1280×800 screenshots and two promo tiles, all 24-bit without alpha where the store requires it.

---

## 8. Version 1.2.0: Website, Privacy Policy & README

The website was updated for Komik 1.2.0 and now showcases KomiK Downloader as its headline feature. A privacy policy page was added (the URL given to the Chrome Web Store), and the README gained a full chapter for the extension.

---

### 1. File Manifest (Website 1.2.0)

#### New
- `components\extension\ExtensionSection.tsx`: the *Special feature · KomiK Downloader 1.0.0* chapter: cyan marquee tape, SplitText heading, starburst sticker, the live demo, feature panels, and the install block (Chrome Web Store *coming soon* until `chromeStoreUrl` is set, zip download, formats, pressable `Alt+K` keycaps that also light up on the real key press, the Save → Watch → Read strip, install-in-three-panels steps and a *Collects nothing* stamp linking to the privacy policy).
- `components\extension\BrowserDemo.tsx`: a working miniature of the extension on the site's own demo comic. A browser tab with a mock comic site, a toolbar crest with badge and a floating crest; clicking (or scrolling into view) runs **scan** (popup with a spinning SCAN! burst, lazy placeholders loading as a scan beam sweeps, pages numbered), **found** (PERFECT MATCH hero, tags lighting up on the page while junk words are struck through, Pages / Details tabs), **download** (progress, per-page ticks, speed), **pack** (thumbnails collapse into a PACKED! burst) and **saved** (a CBZ flies along an arc onto a Komik library shelf, lands with a NEW! sticker, and `ComicInfo.xml` types itself out). Steps indicator, replay, reduced-motion support and a resize-safe flight path.
- `components\extension\ExtensionPanels.tsx`: six live panels: **Every tag, cleaned** (raw chips explode into clean tags, credit counts stripped, age → date), **Lazy pages? Handled.**, **Full-size galleries** (previews sharpen once the rule is learned), **Whole series in one go** (checkbox chapters, range, two-at-a-time progress), **Survives a restart** (close the browser mid-download and resume at the same page) and **Pick pages yourself** (click pages, numbered in reading order).
- `components\NewInV12.tsx`: *Special edition · Issue 1.2.0* paper bento: **Reads what's inside** (XML fields fill the Comic Details card), **Every tag. No cap.** (34 tags rain in, old limit 30 struck out), **Folders fill the shelf**, **Saved it again? Refreshed.** (file size changes, new tags pop in, reading progress kept), **Sharper duplicate finder** (the `original -` label peels off and the copies stack) and **Silky mode switch** (spread ↔ webtoon with the app's timing), plus a ribbon of smaller changes.
- `app\privacy\page.tsx`: `/privacy`, a static comic-styled privacy policy covering the app and the extension: four zero promises, on-page navigation, what the extension reads and when, network requests, what stays in the browser, a table of every permission and why, Chrome Web Store Limited Use statement, what the app keeps on the PC (`%USERPROFILE%\.komik`, crash log in `%LOCALAPPDATA%\Komik`), sharing, children, your choices, changes and contact.

#### Updated
- `lib\config.ts`: version 1.2.0, `siteUrl`, `privacyUrl`, `issuesUrl`, and a new `EXTENSION_CONFIG` (name, version, browsers, zip name/URL/size, `chromeStoreUrl`, shortcuts).
- `app\page.tsx`: order is Hero → Stats → **Extension** → Formats → Engine → **New in 1.2** → New in 1.1 (back issue) → Manifesto → Keyboard → Download.
- `app\layout.tsx`: `metadataBase` set to the live site, extension keywords and descriptions for search and social cards.
- `components\Hero.tsx`: a *NEW · KomiK Downloader* announcement link, a *NEW! Browser extension* sticker and extension words in the marquee tapes (the announcement's entrance animation runs on a wrapper so it never fights its hover transform).
- `components\Navbar.tsx`: Demo · Extension · Formats · Engine · New in 1.2 · Shortcuts.
- `components\StatsStrip.tsx`: 115 automated tests (app + extension).
- `components\DownloadSection.tsx`: a *Plus: KomiK Downloader* card with the zip download.
- `components\Footer.tsx`: *KOMIK 1.2.0 + Downloader 1.0.0*, Extension and Privacy links.
- `components\NewInV11.tsx`: now the *Back issue · 1.1.0* section (`#issue-1-1`).
- `components\ReadingEngine.tsx`, `components\ManifestoSection.tsx`: 1.2 wording.
- `package.json`: version 1.2.0.

#### README Artwork (`komik-website\public\readme\`)
- New animated headers: `section-extension.svg`, `section-new-1-2.svg`; `stats.svg` updated to 115 automated tests.
- Extension screenshots from the store image generator: `ext-banner.jpg`, `ext-pages.jpg`, `ext-details.jpg`, `ext-chapters.jpg`, `ext-history.jpg`, `ext-settings.jpg`.
- Website screenshots from the production build: `screenshot-hero.jpg` (re-captured), `screenshot-extension-demo.jpg`, `screenshot-extension-panels.jpg`, `screenshot-extension-install.jpg`, `screenshot-new-1-2.jpg`, `screenshot-privacy.jpg`.

#### README (`README.md`)
- Version 1.2.0 and extension 1.0.0 badges, an extension download button, and privacy policy links.
- A new **KomiK Downloader** chapter: banner, five screenshots, four feature panels, a *from a website to your shelf* flow diagram, the watched-folder tip, install steps and privacy note.
- **New in 1.2.0** table, with the 1.1.0 table kept in a collapsible block.
- Library, shortcuts (`Alt+K`, `Alt+Shift+K`), privacy, architecture (`ComicInfoReader`, `WatchedFolderMonitor`, extension architecture diagram), tech stack, the 48 app test inventory, extension build and test commands with their coverage, website pages and configuration, known limitations, license and acknowledgements.

---

### 2. Verification

- `npx tsc --noEmit`: no type errors.
- `npm run build` (Next.js 15.5): compiled successfully; `/` and `/privacy` generated as static pages.
- Production server checked with Playwright at 1440×900 and 390×844 (mobile): the demo runs scan → download → saved with the CBZ landing and ComicInfo.xml typed out, all six panels animate and respond, New in 1.2 panels animate, the privacy page renders, and there were no page or console errors. Layout issues found this way were fixed: an over-tall library shelf, an empty popup after packing, a file animation overlapping text, and a hero sticker and announcement overlapping.
