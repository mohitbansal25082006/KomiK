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
