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

## 3. Version 1.1.0 Development & Features Delivered

### New Files Created in Version 1.1.0:
- `Models\ReadingPreset.cs` (6-preset reading themes: Original, Night, Sepia, High Contrast, Grayscale, Inverted)
- `Models\ComicSeriesGroup.cs` (Data model for series & volume groupings, progress tracking, and read-next resolution)
- `Models\DuplicateComicGroup.cs` (Cross-format duplicate group model with quality tags and file attributes)
- `Models\ReadingSession.cs` (Reading session models, daily streaks, wrapped statistics summary)
- `Models\WebtoonPageItem.cs` (Observable continuous vertical scroll page item with dynamic aspect ratios)
- `Services\DuplicateDetectionService.cs` (Cross-format duplicate detection engine with title normalization and ignored pair filtering)
- `Services\OcrService.cs` (100% offline Windows Native OCR engine via `Windows.Media.Ocr.OcrEngine`)
- `Helpers\SeriesParserHelper.cs` (Intelligent regex heuristic parser for series names, volume numbers, and issue numbers)

### 10 Major Features Implemented:

1. **Smooth Animated Scroll to Top on Page Flip**:
   - When flipping to a previous or next page in single-page, spread, or zoomed modes, the viewport smoothly animates and resets scroll position back to the top (`ChangeView(null, 0, null, disableAnimation: false)`).
   - Page slide fade-in transition animation applied smoothly on each turn.

2. **Window Size, Position & Aspect Ratio Persistence**:
   - Remembers exact window dimensions (`WindowWidth`, `WindowHeight`), monitor screen coordinates (`WindowX`, `WindowY`), and maximized state (`IsMaximized`) across application restarts via SQLite `AppSettings`.
   - Seamless restoration on startup without flickering or resetting window proportions.

3. **Duplicate Comic Detection & Management Screen**:
   - Detects identical or duplicate comics across different formats (e.g., matching `.cbz` vs. `.pdf` or `.cbr`), matching file sizes and page counts, and quality release tags (e.g. `(Digital)`, `(Webrip)`, `(c2c)`).
   - Dedicated interactive Duplicate Manager modal overlay in the Library.
   - Allows users to either permanently delete an unwanted redundant copy from disk, or dismiss the duplicate flag ("Ignore Pair") to keep both without future warning prompts.

4. **Brand New Vector Crest Logo Replacement**:
   - Replaced all application icons with the modern high-contrast vector crest brand identity.
   - Rebuilt `Assets/AppIcon.ico` with multi-resolution layers (16px, 24px, 32px, 48px, 64px, 128px, 256px).
   - Updated `Square44x44Logo`, `Square150x150Logo`, `StoreLogo`, `Wide310x150Logo`, `SplashScreen`, and `LockScreenLogo` across Windows shell taskbar, window title bar, and installer.

5. **Series & Volume Grouping**:
   - Automatic parsing and clustering of comic runs, chapters, and volumes using `SeriesParserHelper`.
   - Dedicated "Series & Volumes" view toggle chip in the library filter bar.
   - Visual series cards featuring overall progress bars, issue counters, format badges, and a 1-click "Read Next" launch button.
   - Comprehensive Series Detail modal with interactive issue list, individual reading statuses, and issue cover thumbnails.

6. **Vertical / Webtoon Continuous Scroll Mode**:
   - Dedicated Webtoon continuous vertical reader mode triggered via keyboard shortcut `V` or toolbar button.
   - Renders entire comic as an uninterrupted vertical strip with zero gaps between panels.
   - Smooth mouse wheel scrolling, touch drag, and dynamic aspect-ratio sizing.

7. **100% Local Offline Windows OCR (Text Recognition & Dialogue Search)**:
   - Fully offline text extraction using Windows' built-in `Windows.Media.Ocr.OcrEngine` API (ships with Windows 10/11).
   - Zero internet access, zero cloud calls, and zero external binary dependencies.
   - In-comic dialogue search flyout with live search query input, matching words counter, and jump-to-page navigation.
   - Selectable text overlay layer card allowing readers to highlight, read, and copy dialogue directly from scanned comic panels.

8. **Reading Statistics & Komik Wrapped**:
   - Automatic reading session tracking (`ReadingSessions` table) recording session timestamp, duration in seconds, and pages read.
   - Comprehensive statistics dashboard featuring:
     - Total reading time (hours/minutes)
     - Total pages read across library
     - Completed comics count
     - Current daily reading streak and longest reading streak
     - Top 5 most-read comic series
     - 14-day reading activity breakdown

9. **Expanded Color Reading Presets**:
   - Six high-performance 256-entry Look-Up Table (LUT) presets:
     - **Original**: Natural scan colors.
     - **Night Mode**: Amber warmth with dimmed background.
     - **Sepia Tone**: Classic warm parchment paper effect.
     - **High Contrast**: Enhanced ink line sharpness and deep blacks.
     - **Grayscale**: Pure monochrome rendering.
     - **Inverted**: Dark mode inversion for white backgrounds.
   - Accessible directly via Reader canvas flyout and configurable in App Settings.

10. **Portable JSON Library Backups (`.komikbackup`)**:
    - Export complete library database state (reading progress, bookmarks, notes, favorites, tags, monitored folders, and app settings) into a portable JSON backup file (`.komikbackup`).
    - Safe, non-destructive import with overwrite confirmation.
    - Zero cloud dependency for true local backup portability between machines.

### Automated Verification & Tests:
- Added comprehensive unit tests in `Komik.Tests/Program.cs` covering `SeriesParserHelper`, `DuplicateDetectionService`, `ColorCorrectionHelper` presets, `LibraryRepository` window geometry, reading stats/sessions, and library backup export/import.
- Full test suite passed with **26 passed, 0 failed**.
- Built self-contained win-x64 Release (`publish_selfcontained`) and compiled Inno Setup installer `shipping/Komik-Setup.exe` (v1.1.0).

---

## 4. Version 1.1.0 Refinements & User Feedback Polish Pass

Following the initial v1.1.0 implementation, a comprehensive polish pass was executed addressing 9 specific user-experience items:

1. **Exact Website Vector Crest Brand Integration**:
   - Re-rasterized all Windows shell, application, window title bar, and installer icons directly from `komik-website/public/app-icon.png` using PIL Lanczos filtering.
   - Generated multi-resolution `Assets/AppIcon.ico` (16, 24, 32, 48, 64, 128, 256 px) and all Windows App SDK tile assets (`Square44x44Logo`, `Square150x150Logo`, `StoreLogo`, `Wide310x150Logo`, `SplashScreen`, `LockScreenLogo`).
   - Replaced empty-state font glyphs with the official crest logo image across both Reader and Library views.

2. **Series Grouping (>= 90%) & Duplicate Management (>= 95%) Precision**:
   - Upgraded `SeriesParserHelper` with Levenshtein similarity distance and structural normalization (replacing numeric tokens with `#` placeholders).
   - **Series Grouping**: Requires full-title evaluation with $\ge 90\%$ similarity and at least 2 issues to prevent false-positive grouping.
   - **Duplicate Detection**: Requires $\ge 95\%$ normalized title similarity or identical file size and page count across formats (CBZ, CBR, CB7, PDF).
   - Renamed action button to `"Keep All"` to clearly reflect dismissing flags across multi-copy duplicate sets.
   - Resolved mutual exclusivity between Comics Grid, Comics List, and Series Grid views, completely eliminating overlapping cards.

3. **Smooth Webtoon Continuous Mode Navigation**:
   - Disabled cursor movement / drag panning (`_isPanning`) in Webtoon mode to prevent inadvertent view jumping while moving the pointer.
   - Restricted navigation strictly to smooth mouse wheel scrolling and keyboard keys (`Up`, `Down`, `PageUp`, `PageDown`).
   - Integrated compositor-driven animated scroll (`ChangeView(..., disableAnimation: false)`).

4. **OCR Dialogue Search & Text Layer Dialog Positioning**:
   - Adjusted `OcrTextOverlayCard` margin to `16,76,20,16` to position the floating transcribed dialogue card cleanly below the reader top toolbar.

5. **Reading Statistics Modal Contrast & Historical Fallbacks**:
   - Completely resolved background bleed-through by applying a deep opaque scrim (`#F20B0C0E`) and solid layer card brush (`LayerFillColorDefaultBrush`).
   - Implemented SQLite fallback aggregation for `TotalPagesRead` (`SELECT COALESCE(SUM(last_read_page), 0) FROM Comics WHERE last_read_page > 0;`) and series progress so reading numbers never display 0 for active libraries.
   - Added clear column headers ("TOP SERIES", "READ PROGRESS", "TIME SPENT") and formatted strings (`FormattedPagesRead`, `FormattedDuration`, `FormattedIssues`).

6. **Informational `ℹ` Tooltip Flyouts**:
   - Added accessible `ℹ` info icon buttons with rich explanatory flyouts to:
     - Series & Volumes header
     - Duplicate Comics Manager header
     - Webtoon Continuous Scroll reader toolbar button
     - OCR Dialogue Search reader toolbar button
     - Library Backup & Restore settings section

7. **Library Header Layout & Overflow Elimination**:
   - Removed rigid `MinWidth="760"` and disabled horizontal scrolling on the library header `ScrollViewer`.
   - Compacted button padding and search/sort element widths so the toolbar fits comfortably on any windowed screen ratio without horizontal scrollbars.

8. **Library Backup Import Accuracy**:
   - Enhanced `ImportLibraryBackupJsonAsync` with title and filename fallback matching when exact absolute paths differ between computers.
   - Enforced `overwriteExisting: true` on user-confirmed restores and updated notification banner text with exact imported counts.

9. **Consolidated Reading Presets (Removed Redundant Night Mode)**:
   - Cleaned up obsolete standalone Night Mode toggle switches from the reader toolbar and settings page.
   - Fully consolidated all color styling into the 6 high-performance hardware LUT reading presets (Original, Night, Sepia, High Contrast, Grayscale, Inverted).

---

## 5. Version 1.1.0 Final Enhancements & Installer Release

1. **About Komik Vector Crest Branding**:
   - Added the official vector crest logo directly to the left of the "Komik" title in `SettingsPage.xaml` About section (`ms-appx:///Assets/Square44x44Logo.png` at 30x30 with proper vertical centering).

2. **Ultra-Fast Webtoon Parallel Preloader & Native Transitions**:
   - Replaced sequential 4-page loader with a high-throughput parallel loader (`SemaphoreSlim(6)` on background worker threads).
   - Prioritizes outward decoding from the active viewport index.
   - Decodes high-resolution image streams off-thread before dispatching to UI.
   - Integrated native WinUI `EntranceThemeTransition` (vertical offset 24px) and `RepositionThemeTransition` for fluid, instantaneous reading transitions.
   - Dynamic scroll-ratio position sync in `ReaderScrollViewer_ViewChanged` continuously updating `CurrentPageIndex`.

3. **Reading Statistics Layout, Real-Time Tracking & Top 20 Expansion**:
   - Fixed text alignment for "READ PROGRESS" (`130px`, right-aligned) and "TIME SPENT" (`110px`, right-aligned) across both column headers and row items.
   - Page read count accurately tracked on every page advance via `_pagesReadInSession` in `NextPageAsync()` and `GoToPageAsync()`.
   - SQLite query updated to `MAX(COALESCE(SUM(s.pages_read), 0), COALESCE(SUM(c.last_read_page), 0))` to guarantee accurate historical progress.
   - Expanded ranking to show Top 20 comics and series (`LIMIT 20`).

4. **Complete Light & Dark Mode Compatibility**:
   - Created semantic theme resources in `App.xaml` (`ModalScrimBrush`, `ModalCardBackgroundBrush`, `WrappedCardBrush`, `WrappedCardTextBrush`, `WrappedCardSubtextBrush`, `WrappedPillBrush`).
   - Adapted Stats modal, Komik Wrapped yearly card, and Duplicate Comics Manager modal to seamlessly respond to runtime Light and Dark theme changes.

5. **Series & Volumes Visual Active State & Universal Filter Compatibility**:
   - Implemented high-contrast active state for "Series & Volumes" filter chip: vibrant Accent background (`#F59E0B`), bold dark text, and black icon.
   - Unified series clustering with all library filters: when filters are active (Favorites, Continue Reading, Completed, Unread, Tags, Collections, or Search), series matching $\ge 1$ filtered comic are seamlessly shown.
   - Series groups naturally sorted according to the active `SelectedSortOption` (Title A-Z, Title Z-A, Recently Read, Date Added, Page Count, File Size).
   - Fixed `ShowSeriesGrid` and `ShowEmptyFilter` states so filter results and empty-filter notices render reliably in series mode.

6. **Custom Manual Series Creation & Management**:
   - Created SQLite tables `ManualSeries` and `ManualSeriesComics` with full relational integrity.
   - Added interactive "Create New Series" modal with live library search, multi-selection thumbnail grid, select all / clear actions, and real-time selection counts.
   - Added "+ New Series" button in Series view header.
   - Added visual "Manual Series" badge, "Delete Series" command, and per-comic "Remove from Series" action buttons in the Series Detail view.

7. **Verification & Delivery**:
   - Added automated unit test: `LibraryRepository Supports Manual Series Creation, Retrieval and Deletion`.
   - **All 27 automated unit tests PASSED (27 passed, 0 failed)**.
   - Compiled full self-contained win-x64 Release build (`publish_selfcontained`).
   - Built Inno Setup installer: `shipping/Komik-Setup.exe` (v1.1.0).
   - Installed locally on system for immediate testing.

---

## 6. Version 1.1.1 Refinements (Logo Branding, Webtoon Smooth Scroll, Standalone Series Screen & Add to Series)

1. **Official Website Logo Integration**:
   - Integrated the official high-resolution branding logo directly from `komik-website/public/app-icon.png` (512x512) into `Assets/Square150x150Logo.scale-200.png`, `Assets/Square44x44Logo.scale-200.png`, `Assets/Square44x44Logo.png`, `Assets/StoreLogo.png`, and `Assets/app-icon.png`.
   - Configured `Komik.csproj` with `<Content Include="Assets\**\*"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content>` to ensure unpackaged WinUI 3 desktop deployments (`ms-appx:///Assets/...`) bundle and deploy all image assets to the output directory and installer.
   - Updated `SettingsPage.xaml` About section to render `ms-appx:///Assets/Square150x150Logo.scale-200.png` at 36x36 with `Stretch="Uniform"` alongside the Komik header and version tag.

2. **Webtoon Continuous Mode Scroll & Page Change Bugfix**:
   - **Root Cause**: `ReaderScrollViewer_ViewChanged` in continuous Webtoon mode updated `CurrentPageIndex = estPage`. This fired `ViewModel_PropertyChanged("CurrentPageIndex")` which unconditionally executed `ReaderScrollViewer.ChangeView(null, 0, null)`, forcibly snapping the user's view back to the top of the comic on every scroll tick.
   - **Fix**: Added scroll state flags (`_isUserScrollingWebtoon`, `_isProgrammaticScroll`) in `MainPage.xaml.cs`.
   - Guarded `ViewModel_PropertyChanged` so `ChangeView(null, 0, null)` only executes when NOT in Webtoon mode (`if (!ViewModel.IsWebtoonMode)`).
   - Wrapped `CurrentPageIndex = estPage;` in `ReaderScrollViewer_ViewChanged` with `_isUserScrollingWebtoon = true` and guarded against programmatic scroll events.
   - Implemented `ScrollToWebtoonPage(int targetIndex)`: computes the exact vertical layout offset of the target page container relative to `PageDisplayContainer` (with proportional height fallback) and smoothly changes the viewport with `disableAnimation: false`.

3. **Standalone Dedicated Series & Volumes Screen**:
   - Decoupled "Series & Volumes" from the comic filter chip bar into a standalone library feature.
   - Added a dedicated button beside "Duplicate Comics" in the main Library toolbar with active state styling.
   - When entering Series & Volumes:
     - The comic filter pills bar (Favorites, Continue Reading, Completed, Unread, Tags, Collections) is hidden.
     - A dedicated Series Header is presented containing "← Back to Comics", Series Title + Count badge, "+ New Series" button, series search box, Series Sort picker, and Info flyout.
   - Decoupled `UpdateSeriesGroupsAsync()`: evaluates all comics in the library independently of comic filter chips, auto-detects series runs with $\ge 90\%$ title match, and filters dynamically by `SeriesSearchText`.

4. **Add Comics to Existing Series**:
   - Added "+ Add Comics" action button to the `SeriesDetailOverlay` header.
   - Created `AddComicsToSeriesOverlay` modal picker in `LibraryPage.xaml`:
     - Live candidate search box.
     - Quick "Select All" and "Clear Selection" buttons.
     - Multi-selection candidate card grid displaying comic cover thumbnail, title, and page count with visual checkmark indicators.
     - Automatically excludes issues already present in the active series.
   - Added `OpenAddComicsToExistingSeriesCommand`, `CloseAddComicsToSeriesDialogCommand`, and `SaveComicsToExistingSeriesCommand` in `LibraryViewModel.cs`.
   - If the series is already manual, calls `AddComicsToManualSeriesAsync`. If auto-detected, automatically promotes the series into a persistent manual series via `CreateManualSeriesAsync` so all user additions are preserved in SQLite.

5. **Test Suite, Build & Installer Validation**:
   - Extended automated test suite in `Komik.Tests/Program.cs` to verify `AddComicsToManualSeriesAsync`.
   - Ran `dotnet run --project Komik.Tests/Komik.Tests.csproj`: **All 27 unit tests PASSED (27 passed, 0 failed)**.
   - Compiled full self-contained win-x64 Release build (`dotnet publish Komik.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:WindowsPackageType=None -o publish_selfcontained`).
   - Verified all logo and branding assets exist in `publish_selfcontained\Assets`.
   - Compiled Inno Setup installer (`shipping\Komik-Setup.exe`).
   - Executed silent local install (`shipping\Komik-Setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART`) with exit code 0.

