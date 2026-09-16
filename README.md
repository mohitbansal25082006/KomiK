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

<a href="https://github.com/mohitbansal25082006/KomiK/releases/latest/download/Komik-Setup.exe"><img src="https://img.shields.io/badge/⬇_DOWNLOAD_FOR_WINDOWS-~75_MB-FFD700?style=for-the-badge&labelColor=0A0A0F" alt="Download for Windows" height="42" /></a>
&nbsp;
<a href="https://github.com/mohitbansal25082006/KomiK/releases/latest/download/komik-downloader-1.0.0.zip"><img src="https://img.shields.io/badge/🧩_BROWSER_EXTENSION-CHROME_·_EDGE_·_BRAVE-FF1F6D?style=for-the-badge&labelColor=0A0A0F" alt="Get the KomiK Downloader browser extension" height="42" /></a>
&nbsp;
<a href="https://komik-website-taupe.vercel.app/"><img src="https://img.shields.io/badge/🌐_LIVE_WEBSITE-TRY_THE_READER-00C2FF?style=for-the-badge&labelColor=0A0A0F" alt="Visit the live website" height="42" /></a>

<br /><br />

<img src="https://img.shields.io/badge/version-1.2.0-FF1F6D?style=flat-square&labelColor=0A0A0F" alt="Version 1.2.0" />
<img src="https://img.shields.io/badge/extension-1.0.0-FFD700?style=flat-square&labelColor=0A0A0F" alt="KomiK Downloader 1.0.0" />
<img src="https://img.shields.io/badge/Windows-10_%7C_11-00C2FF?style=flat-square&logo=windows11&logoColor=white&labelColor=0A0A0F" alt="Windows 10 and 11" />
<img src="https://img.shields.io/badge/.NET-8-FFD700?style=flat-square&logo=dotnet&logoColor=white&labelColor=0A0A0F" alt=".NET 8" />
<img src="https://img.shields.io/badge/UI-WinUI_3-FF1F6D?style=flat-square&labelColor=0A0A0F" alt="WinUI 3" />
<img src="https://img.shields.io/badge/telemetry-none-00C2FF?style=flat-square&labelColor=0A0A0F" alt="No telemetry" />
<img src="https://img.shields.io/badge/license-MIT-FFD700?style=flat-square&labelColor=0A0A0F" alt="MIT License" />

<br /><br />

**Komik** is a lightweight, high-performance, **local-only** Windows desktop reader for comics, manga and webtoons,<br />
built with **WinUI 3 (Windows App SDK)** and **C# / .NET 8** and designed around Windows 11 Fluent Design.<br />
Its companion extension, **KomiK Downloader**, saves comics from any website as CBZ files with every tag inside, straight onto your Komik shelf.

<sub>

**[Screenshots](#-see-it-in-action)** · **[Extension](#-komik-downloader)** · **[Features](#-the-reading-engine)** · **[What's new](#-new-in-120)** · **[Formats](#-every-format-zero-codecs)** · **[Install](#-download--install)** · **[Shortcuts](#-keyboard-first-reading)** · **[Privacy](#-your-comics-belong-on-your-pc)** · **[Architecture](#-under-the-hood)** · **[Build](#-build-it-yourself)** · **[Website](#-the-komik-website)**

</sub>

<img src="komik-website/public/readme/stats.svg" alt="8 formats · 6 parallel decoders · 6 LUT presets · 0 network calls · 0 accounts · 115 automated tests" width="100%" />

<br />

<table>
  <tr>
    <td align="center">
      <a href="https://komik-website-taupe.vercel.app/"><b>🌐&nbsp; Try Komik in your browser&nbsp; →</b></a><br />
      <sub>Read the complete demo comic in a working replica of the Komik reader. No install needed.</sub>
    </td>
  </tr>
</table>

</div>

> [!NOTE]
> **Privacy & offline architecture.** Komik is strictly offline and local-first. There is **no cloud sync, no account system, no telemetry or tracking, and zero external network calls anywhere in the application**. All comic metadata, reading progress, cover thumbnails, bookmarks and preferences stay on your own machine. The KomiK Downloader extension collects nothing either: it only talks to the site you download from. Read the full **[privacy policy](https://komik-website-taupe.vercel.app/privacy)**.

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-see-it-in-action"></a>
<img src="komik-website/public/readme/section-try-it.svg" alt="Read the demo comic" width="100%" />

### 🖥️ The Komik desktop app

<div align="center">

<img src="komik-website/public/readme/app-library.jpg" alt="Komik library: comic-style cover grid with reading progress, filter chips, tags and the ADD menu" width="100%" />

<sub><b>The library.</b> A comic-style cover grid with progress badges, favorites, filter chips, <b>Tags</b> and <b>Manage Tags</b>, plus Stats, Duplicates and Series &amp; Volumes one click away.</sub>

<br /><br />

<table>
  <tr>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/app-reader.jpg" alt="Komik reader in two-page spread mode with the floating toolbar and page scrubber" width="100%" /><br />
      <sub><b>The reader.</b> A two-page spread with the floating toolbar and scrubber. The chrome auto-hides while you read.</sub>
    </td>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/app-webtoon.jpg" alt="Komik webtoon continuous vertical scroll mode" width="100%" /><br />
      <sub><b>Webtoon mode.</b> One long vertical strip, decoded in parallel. Zoom keeps your place, and comics reopen at the saved page.</sub>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/app-series.jpg" alt="Komik Series screen with Continuation Series and By Creator sections" width="100%" /><br />
      <sub><b>Series &amp; Volumes.</b> <b>Continuation Series</b> and <b>By Creator</b> sections, with progress and a one-click <b>Continue</b> / <b>Read next</b>.</sub>
    </td>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/app-series-detail.jpg" alt="Komik series detail with cover banner, progress and edit actions" width="100%" /><br />
      <sub><b>Series detail.</b> Gap check, progress, <b>Edit</b>, <b>Cover</b> and <b>Add Comics</b>, with the next issue marked <b>Up next</b>.</sub>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/app-stats.jpg" alt="Komik Reading Insights and Komik Wrapped dashboard" width="100%" /><br />
      <sub><b>Reading Insights.</b> Active reading time, pages, streaks, Komik Wrapped and Top Series with exact progress, all computed locally.</sub>
    </td>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/app-tags.jpg" alt="Komik Manage Tags screen with search, sort, used and unused filters" width="100%" /><br />
      <sub><b>Manage Tags.</b> Search, sort, used / unused filters, rename, merge and bulk-delete unused tags. Built for big tag lists.</sub>
    </td>
  </tr>
  <tr>
    <td colspan="2" align="center">
      <img src="komik-website/public/readme/app-settings.jpg" alt="Komik settings: How Comics Open with layout and direction tiles" width="100%" /><br />
      <sub><b>Settings → How Comics Open.</b> Page by page, two-page spread or webtoon, combined with comic (left to right) or manga (right to left). Everything saves instantly.</sub>
    </td>
  </tr>
</table>

<sub>Every screenshot uses an isolated demo library of original mock comics, including <b>Cyberpunk Chronicles</b>, the demo comic made for the Komik website, packed into real <code>.cbz</code> files.</sub>

</div>

<br />

### 🌐 Interactive Promotional Website

<div align="center">

<a href="https://komik-website-taupe.vercel.app/"><img src="https://img.shields.io/badge/🌐_OPEN_THE_LIVE_DEMO-READ_A_COMIC_NOW-FF1F6D?style=for-the-badge&labelColor=0A0A0F" alt="Open the live demo website" height="38" /></a>

<sub>Spreads · manga RTL · webtoon · color presets · bookmarks · dialogue search · full screen</sub>

</div>

The website holds a fully working replica of the reader, so visitors can read the whole demo comic in the browser before installing anything. Version 1.2.0 adds a live **KomiK Downloader** demo that scans the site's own comic, packs it into a CBZ and drops it on a Komik shelf, plus a **New in 1.2** special edition and a **[privacy policy](https://komik-website-taupe.vercel.app/privacy)** page.

<div align="center">
<img src="komik-website/public/readme/screenshot-hero.jpg" alt="Komik website hero" width="100%" />
<table>
  <tr>
    <td width="50%"><img src="komik-website/public/readme/screenshot-reader.jpg" alt="Website reader mockup in full screen" width="100%" /></td>
    <td width="50%"><img src="komik-website/public/readme/screenshot-formats.jpg" alt="Collectible format cards" width="100%" /></td>
  </tr>
  <tr>
    <td width="50%"><img src="komik-website/public/readme/screenshot-engine.jpg" alt="Pinned reading engine comic strip" width="100%" /></td>
    <td width="50%"><img src="komik-website/public/readme/screenshot-new-1-2.jpg" alt="New in 1.2.0 special edition" width="100%" /></td>
  </tr>
  <tr>
    <td width="50%"><img src="komik-website/public/readme/screenshot-extension-demo.jpg" alt="Live KomiK Downloader demo: a comic scanned, packed and landed on the Komik shelf with its ComicInfo.xml" width="100%" /></td>
    <td width="50%"><img src="komik-website/public/readme/screenshot-extension-panels.jpg" alt="Interactive extension feature panels: tag cleaner, lazy pages, full-size galleries, whole series" width="100%" /></td>
  </tr>
  <tr>
    <td width="50%"><img src="komik-website/public/readme/screenshot-extension-install.jpg" alt="Extension install panel with formats, shortcuts and privacy stamp" width="100%" /></td>
    <td width="50%"><img src="komik-website/public/readme/screenshot-privacy.jpg" alt="Komik privacy policy page" width="100%" /></td>
  </tr>
</table>
<img src="komik-website/public/readme/mobile-screen-hero-section.PNG" alt="Website on mobile" width="32%" />
&nbsp;
<img src="komik-website/public/readme/screenshot-mobile-reader.jpg" alt="Website reader on mobile" width="32%" />
</div>

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-komik-downloader"></a>
<img src="komik-website/public/readme/section-extension.svg" alt="KomiK Downloader: the browser extension" width="100%" />

<div align="center">

<img src="komik-website/public/readme/ext-banner.jpg" alt="KomiK Downloader: comics to CBZ, tags included" width="100%" />

**KomiK Downloader 1.0.0** is a free browser extension for **Chrome, Edge and Brave**.<br />
Open a comic, manga or webtoon on any website, click the crest, and it saves every page as a **CBZ with the title, credits and every tag written inside**.<br />
Komik 1.2.0 files it into your library on its own.

<a href="https://github.com/mohitbansal25082006/KomiK/releases/latest/download/komik-downloader-1.0.0.zip"><img src="https://img.shields.io/badge/⬇_DOWNLOAD-komik--downloader--1.0.0.zip-FFD700?style=for-the-badge&labelColor=0A0A0F" alt="Download komik-downloader-1.0.0.zip" height="36" /></a>
&nbsp;
<img src="https://img.shields.io/badge/CHROME_WEB_STORE-COMING_SOON-FF1F6D?style=for-the-badge&labelColor=0A0A0F" alt="Chrome Web Store: coming soon" height="36" />

<br /><br />

<table>
  <tr>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/ext-pages.jpg" alt="KomiK Downloader popup finding every page of a chapter" width="100%" /><br />
      <sub><b>One click.</b> Every page of the chapter you're reading, lazy-loaded ones included, full size and in order.</sub>
    </td>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/ext-details.jpg" alt="KomiK Downloader details tab with title, credits and tags" width="100%" /><br />
      <sub><b>Every detail.</b> Title, series, number, credits, date and all of the site's tags, cleaned and ready for ComicInfo.xml.</sub>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/ext-chapters.jpg" alt="KomiK Downloader chapter list for batch downloads" width="100%" /><br />
      <sub><b>Whole series.</b> Pick chapters or a range on a series page and each one is saved as its own numbered comic.</sub>
    </td>
    <td width="50%" align="center">
      <img src="komik-website/public/readme/ext-history.jpg" alt="KomiK Downloader side panel with download history" width="100%" /><br />
      <sub><b>Download manager.</b> A side panel with the live queue and a searchable history with covers and tags.</sub>
    </td>
  </tr>
  <tr>
    <td colspan="2" align="center">
      <img src="komik-website/public/readme/ext-settings.jpg" alt="KomiK Downloader settings page" width="100%" /><br />
      <sub><b>Settings.</b> Folder and file-name templates, formats, speed, tag clean-up, page scanning, site rules and themes. Everything saves instantly.</sub>
    </td>
  </tr>
</table>

<sub>The comic in these screenshots is a made-up showcase with generated art.</sub>

</div>

<table>
<tr>
<td width="50%" valign="top">

#### 🔎 Finds the comic on any site
- **Universal scanner**: finds the reader column, ignores logos, ads and avatars
- **Lazy-loading readers** are scrolled for you, then your place is restored
- Webtoon strips, one-page-per-URL readers and script-built readers
- **Gallery sites**: learns full-size image names from one page, with each page's reader page as a fallback
- **Series pages**: detects chapter lists with numbers, volumes and dates
- **Pick pages** by clicking them, for the odd site nothing else can read
- **Site rules**: your own CSS selectors for pages, chapters, title and tags

</td>
<td width="50%" valign="top">

#### 🏷️ Details Komik reads perfectly
- Writes **ComicInfo.xml**: title, series, number, volume, writers, artists, publisher, date, summary, language, manga direction, tags and page info
- **Every tag, no limit**: junk words dropped, counts stripped (`action 1,234` → `Action`), capitals fixed, duplicates merged
- Credits cleaned (`rio pen 48` → `Rio Pen`)
- Ages turned into dates (`uploaded 5 years 6 months ago` → March 2021)
- **Series memory**: fix a series' details once and the next chapters match
- Numbering follows Komik's own parser, so chapters group into one series

</td>
</tr>
<tr>
<td width="50%" valign="top">

#### ⚡ Fast & reliable downloads
- Parallel page downloads per site and overall, with retries and timeouts
- **Resumes after a restart**: pages are kept until the comic is saved
- Hotlink-protected images are fetched with the page as Referer
- AVIF / JPEG XL / HEIC pages converted to JPEG so Komik opens them
- **CBZ** (pages stored, no recompression) · **ZIP** · **PDF** · **Folder**
- Saves to `Downloads/KomiK/{series}/{title}.cbz` by default. Templates also take `{volume}` `{chapter}` `{year}` `{site}` `{author}` `{language}` and more

</td>
<td width="50%" valign="top">

#### 🎨 Comic-style and hands-free
- Popup, **side panel** (queue · this page · history) and a full options page
- Floating crest button and toolbar badge on pages with a comic
- Right-click menu: *Download the comic on this page*
- <kbd>Alt</kbd>+<kbd>K</kbd> opens KomiK · <kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>K</kbd> downloads right away
- Light (paper) theme by default, plus dark theme, sounds, reduced motion and notifications
- Safe across updates: tabs left open while the extension updates stand down quietly

</td>
</tr>
</table>

### 🔗 From a website to your shelf

```mermaid
flowchart LR
    SITE["🌐 Comic page"] -->|click the crest| SCAN["🔎 Scan<br/>pages · chapters · details"]
    SCAN --> JOB["⬇️ Download queue<br/>parallel · resumable"]
    JOB --> PACK["🗜️ Pack CBZ<br/>+ ComicInfo.xml"]
    PACK --> DL[("📁 Downloads/KomiK")]
    DL -->|watched folder| KOMIK["📚 Komik 1.2.0<br/>details & every tag"]
```

> [!TIP]
> In Komik, add **`Downloads\KomiK`** as a watched folder once. Every comic the extension saves then appears in your library with its details and tags, including ones downloaded while Komik was closed. Save a comic again under the same name and Komik picks up its new tags too.

### 🧩 Install the extension

1. **Download** [`komik-downloader-1.0.0.zip`](https://github.com/mohitbansal25082006/KomiK/releases/latest/download/komik-downloader-1.0.0.zip) from the latest release and extract it.
2. Open **`chrome://extensions`** (or `edge://extensions` / `brave://extensions`) and turn on **Developer mode**.
3. Click **Load unpacked**, choose the extracted folder, and pin the KomiK crest to your toolbar.

<sub>A Chrome Web Store listing is on its way. Once it's live, installing takes a single click.</sub>

> [!NOTE]
> **Collects nothing.** No accounts, analytics, ads or servers. The extension only requests the comic's own pages and images from the site you're on, and keeps its settings, queue and history in your browser. Every permission is explained in the **[privacy policy](https://komik-website-taupe.vercel.app/privacy#extension)**.

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
- **Watched folders** with recursive indexing into local SQLite. New downloads are added on their own once they finish, and files saved again under the same name are re-read
- **Reads ComicInfo.xml** from CBZ, ZIP, CBR, CB7 and image folders: title, series, number, credits, date, summary and **every tag**
- **Cover grid** and **detailed list** views
- Instant search, plus filter chips: All · Continue Reading · Favorites · Completed · Unread · Tags
- **Tag manager** with search, sort, usage counts, rename, merge and "delete unused"
- Friendly empty screens for every filter, and a colored sticker showing the active filter and its match count
- Sort by title (A–Z / Z–A), recently read, date added, page count or file size
- Disk-backed thumbnail cache for near-instant covers
- **Non-destructive:** removing a library entry never deletes your file, and removed comics can be added back from Settings

</td>
<td width="50%" valign="top">

#### ⚙️ Settings, details & shell integration
- Comic-style theme in **Light** (default) or **Dark**, or follow Windows
- **How Comics Open:** page by page, two-page spread or webtoon, left to right or manga right to left
- Reading & library defaults, page colors, monitored folders, thumbnail cache tools (all saved instantly)
- **Comic Details** editor: title, series, issue #, writers, artists, publisher, release date, summary. Filled in from embedded ComicInfo.xml, and your edits always win
- **File associations**, so double-clicking a comic in Explorer opens it in Komik
- Command-line launch: `Komik.exe "<path-to-comic>"`

</td>
</tr>
</table>

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-new-in-120"></a>
<img src="komik-website/public/readme/section-new-1-2.svg" alt="Hot off the press: new in 1.2.0" width="100%" />

| # | Feature | What it does |
| :-: | :-- | :-- |
| 1 | 🧩 **KomiK Downloader 1.0.0** | A new companion browser extension for Chrome, Edge and Brave that saves comics from any website as CBZ with their details and every tag inside. [See the extension →](#-komik-downloader) |
| 2 | 📄 **Reads ComicInfo.xml** | Komik reads the details file inside CBZ, ZIP, CBR, CB7 and image folders: title, series, number, volume, writers, artists, publisher, date, summary, language and tags. The embedded title becomes the library title, and metadata you typed or restored is never overwritten. |
| 3 | 🏷️ **Every tag, no limit** | Genres and tags are imported together, trimmed and de-duplicated, with no cap on how many. A comic with 34 tags shows all 34. |
| 4 | 📥 **Folders that fill themselves** | Watched folders pick up finished downloads as they land (after the file stops growing), and anything added while Komik was closed is indexed on the next start, details and tags included. |
| 5 | 🔄 **Re-downloads refresh** | Save a comic again under the same name (say, now with more tags) and Komik notices the file changed and adds what's new. Reading progress, favourites and your own edits stay put. |
| 6 | 👥 **Sharper duplicate finder** | Copies whose names only differ by a leading label such as `original - [Circle (Artist)] Title` are now found, while different parts of a series stay apart. The label no longer hides the creators either. |
| 7 | ✨ **Silky mode switch** | Switching between spreads and webtoon scrolling settles the page back, lays out the new mode off-screen and rises it in, following Windows' animation setting. |
| 8 | 🎨 **Comic-style dialogs** | Cache all covers, Clear cache and Remove watched folder now open as comic panels that match the rest of the app. |
| 9 | 🖼️ **Bigger, sharper icons** | A rebuilt multi-size app icon fills the taskbar properly, and the title-bar crest is larger. |
| 10 | 📦 **Installer polish** | The setup shows the MIT license, and updating closes a running Komik first so files are never locked. |

<details>
<summary><b>📚 Still here from 1.1.0 (click to expand)</b></summary>
<br />

| # | Feature | What it does |
| :-: | :-- | :-- |
| 1 | 🎨 **Comic-style app** | Halftone panels, ink borders, stickers and Bangers lettering across the library, reader, dialogs and toasts, in Light (now the default) and Dark. Cards lift and tilt, panels pop in, and menus open instantly. |
| 2 | 📚 **Series & Volumes** | Two sections: **Continuation Series** (issues, volumes, chapters and sequels, typo-tolerant, with gap detection) and **By Creator**. A creator collection can move to Continuation Series, but never the reverse. |
| 3 | ✏️ **Series editing** | Edit any series: rename, drag the reading order, auto-order, pick the cover, remove comics, toggle auto-update. **+ Add Comics** has search and filters, and a right-click menu works on every comic inside a series. |
| 4 | 🏷️ **Tag manager** | Search, sort by name or use, used / unused filters, rename, merge and delete. Tagging a comic is instant, and pressing Enter creates a new tag. |
| 5 | 🧭 **Smarter filters** | Every empty filter, tag or search shows a comic-style screen with a helpful next step. The active filter appears as a colored sticker with its match count and a clear button. |
| 6 | 📖 **How Comics Open** | Choose page by page, two-page spread or webtoon, plus comic (left to right) or manga (right to left), globally in Settings. Comics open that way at their saved page. |
| 7 | ⚡ **Parallel Webtoon engine** | A 6-worker concurrent decoder with outward viewport priority, an animated auto-hiding toolbar, live zoom percentage and zoom that keeps your place. |
| 8 | 📊 **Reading Stats & Komik Wrapped** | Active reading time (idle capped), pages actually viewed, streaks, periods and **Top Series** with exact page-based progress. |
| 9 | 👥 **Duplicate Manager** | Scores copies by identical bytes, same series and issue, or same cleaned title, explains why, and recommends the copy to keep. Deleting works even when a file was just in use. |
| 10 | 🔍 **Offline OCR** | Windows' built-in `Windows.Media.Ocr` groups speech bubbles in reading order (right to left for manga), tiles tall pages and filters art noise. Dialogue search jumps to the page. No cloud APIs. |
| 11 | ♻️ **Add back removed comics** | Removed comics are remembered and skipped by rescans. Settings lists every comic in your folders that isn't in the library, so you can bring them back. |
| 12 | 🗜️ **Archive → CBZ converter** | Lossless repacking of CBR / CB7 / ZIP / RAR / 7Z and image folders, plus PDF rasterization, into clean zero-padded CBZ files. |
| 13 | 🧭 **Responsive top bar** | A single accent **ADD** dropdown (Add Folder / Add Comic), horizontal mouse-wheel scrolling and a layout that adapts to any window width. |
| 14 | 🪟 **Window memory** | Restores size, position and maximized state on every launch. |
| 15 | 💾 **Portable backups** | A `.komikbackup` JSON file with history, bookmarks, notes, favorites, tags and settings, imported without conflicts. |

</details>

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
| **Installer** | ~75 MB, fully self-contained |
| **Prerequisites** | **None.** .NET 8 and the Windows App SDK runtime are bundled |

<sub>Windows 11 is recommended for native Mica backdrops.</sub>

</td>
</tr>
</table>

> [!TIP]
> **Add the browser extension too.** [KomiK Downloader](#-install-the-extension) saves comics from websites straight into a folder Komik watches, with every tag inside.

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
| <kbd>Alt</kbd>+<kbd>K</kbd> | Open KomiK Downloader on the current page | 🧩 |
| <kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>K</kbd> | Download the comic on this page right away | 🧩 |

<sub>📖 Reader · 🌐 Global · 🧩 Browser extension (change them at <code>chrome://extensions/shortcuts</code>)</sub>

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

The **KomiK Downloader** extension follows the same rules: no accounts, no analytics, no servers of ours, and nothing sold or shared. It only requests the comic's pages and images from the site you're downloading from. The full, plain-language **[privacy policy](https://komik-website-taupe.vercel.app/privacy)** covers both the app and the extension, including every extension permission and why it's needed.

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
        CIR["ComicInfoReader"]
        WFM["WatchedFolderMonitor"]
        THUMB["ThumbnailService"]
        CONV["FormatConversionService"]
        DUP["DuplicateDetectionService"]
        SER["SeriesDetectionService"]
        STATS["ReadingStatsCalculator"]
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
    LVM --> SER
    LVM --> STATS
    SCAN --> THUMB
    SCAN --> CIR
    WFM --> SCAN
    LVM --> DB
    MVM --> DB
    SVM --> DB
```

### 🧩 Extension architecture

```mermaid
flowchart LR
    subgraph PAGE["🌐 Web page"]
        SEN["sentinel.js<br/>page count · floating crest"]
        ENG["engine.js (on demand)<br/>scanner · picker"]
    end

    subgraph EXT["🧠 Extension"]
        BG["Service worker<br/>scans · queue · menus · shortcuts"]
        UI["Popup · side panel · options<br/>React 19"]
        OFF["Offscreen engine<br/>fetch · pack CBZ/PDF · save"]
        IDB[("IndexedDB<br/>jobs · pages · history")]
    end

    SEN --> BG
    UI <--> BG
    BG --> ENG
    BG <--> OFF
    OFF --> IDB
    OFF -->|chrome.downloads| DL[("📁 Downloads/KomiK")]
    DL -->|WatchedFolderMonitor| APP["📚 Komik"]
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
| **Extension** | Manifest V3 · TypeScript · React 19 · Tailwind CSS · Framer Motion · Vite |
| Extension packing | fflate (CBZ/ZIP) · pdf-lib (PDF) · IndexedDB · offscreen document |
| Extension tests | Vitest (unit) · Playwright driving real Chromium (end to end) |

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
    date_created TEXT NOT NULL,
    kind TEXT NOT NULL DEFAULT 'story',      -- 'story' (continuation) or 'creator'
    auto_update INTEGER NOT NULL DEFAULT 0,
    source_key TEXT                          -- the automatic group it was saved from
);

CREATE TABLE IF NOT EXISTS ManualSeriesComics (
    series_id INTEGER NOT NULL,
    comic_id INTEGER NOT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY(series_id, comic_id),
    FOREIGN KEY(series_id) REFERENCES ManualSeries(id) ON DELETE CASCADE,
    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ManualSeriesExclusions (
    series_id INTEGER NOT NULL,
    comic_id INTEGER NOT NULL,
    PRIMARY KEY(series_id, comic_id),
    FOREIGN KEY(series_id) REFERENCES ManualSeries(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS RemovedComics (
    file_path TEXT PRIMARY KEY COLLATE NOCASE,
    title TEXT,
    date_removed TEXT NOT NULL
);

-- Indexes: Comics(file_path, title, is_favorite, last_read_at), ComicTags(comic_id, tag_id),
-- ComicCollections, Bookmarks, ComicMetadata, ReadingSessions(comic_id, session_date), ManualSeriesComics(comic_id)
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
dotnet run --project Komik.Tests/Komik.Tests.csproj -c Release
```

<details>
<summary><b>What the 48 automated tests cover (click to expand)</b></summary>
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
| 28 | `ComicIdentityParser` parses series, volumes, issues, chapters and years |
| 29 | `SeriesDetectionService` groups, orders, merges typos and finds gaps |
| 30 | `DuplicateDetectionService` scores copies, avoids false positives and recommends the best copy |
| 31 | `ReadingSessionTracker` counts active time with an idle cap and distinct pages |
| 32 | `ReadingStatsCalculator` uses local days, streaks, periods and real series totals |
| 33 | `LibraryRepository` upserts live reading sessions and loads metadata in bulk |
| 34 | Continuations and creator collections across the whole library |
| 35 | Manual series with duplicate names get unique names |
| 36 | Manual series: creator section, auto-update, exclusions and going back to automatic |
| 37 | Removed comics are remembered, skipped by rescans and can be added back |
| 38 | Tags: usage counts, rename, merge and delete |
| 39 | Manual series rename keeps names unique |
| 40 | OCR layout groups speech bubbles in reading order and tiles tall pages |
| 41 | Top Series follow series grouping with exact page-based progress |
| 42 | `ComicInfoReader` parses metadata, credits, dates and tags |
| 43 | ComicInfo title resolution keeps the series in the library title |
| 44 | Scanner indexes embedded metadata and tags from CBZ, CB7 and folders |
| 45 | Embedded metadata import never overwrites edits and runs once |
| 46 | New downloads in watched folders are indexed, removed comics stay out |
| 47 | A comic downloaded again under the same name gets all its new tags |
| 48 | CBZ conversion keeps the embedded ComicInfo.xml |

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

### 🧩 Build the extension

Requires [Node.js](https://nodejs.org/) 20 or later.

```powershell
cd KomiK-Extension
npm install
npm run build       # dist\  → load it unpacked in chrome://extensions
npm run watch       # rebuild on every change while developing
npm run typecheck   # TypeScript, no emit
npm test            # 58 unit tests (Vitest)
npm run test:e2e    # 9 end-to-end tests in real Chromium (Playwright)
npm run package     # release\komik-downloader-1.0.0.zip, ready for the Chrome Web Store
npm run store       # release\store\  → store icon, screenshots and promo tiles
```

<details>
<summary><b>What the extension tests cover (click to expand)</b></summary>
<br />

**Unit (58):** universal page detection, reader-theme adapters, metadata scraping (JSON-LD, labelled fields, label/value grids, ages to dates), chapter lists and pagination, gallery previews and full-size rules, identity parsing that mirrors Komik, naming templates and Windows-safe paths, tag cleaning and counts, tag limits and settings migration, ComicInfo.xml output, page HTML parsing without loading scripts, and image sniffing.

**End to end (9)** against a local fixture site in real Chromium:
1. Lazy reader: scans with auto-scroll and saves a tagged CBZ named by its title
2. Series page: batch-downloads chapters resolved in the background
3. Hotlink-protected images download with the reader page as Referer
4. Gallery of previews: every page saved at full size, with the reader page as fallback
5. Gallery whose full-size pages can't be guessed: the pattern is learned from one page
6. Settings change what is saved: folder, file name, page numbering and tag limit
7. Reading a page full of scripts never asks the browser to load them
8. Options and history pages render
9. Pages left open while the extension updates never throw "Extension context invalidated"

</details>

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

<a id="-the-komik-website"></a>
<img src="komik-website/public/readme/section-website.svg" alt="The Komik website" width="100%" />

The marketing and download site lives in [`komik-website/`](komik-website/) and is **[live on the web](https://komik-website-taupe.vercel.app/)**. It's built as a **living comic book**, and its centerpiece is a working replica of the Komik reader that holds a complete, original 12-page comic.

| | |
| :-- | :-- |
| **Live site** | **[🌐 Open the Komik website →](https://komik-website-taupe.vercel.app/)** |
| **Stack** | Next.js 15 (App Router) · React 19 · TypeScript · Tailwind CSS |
| **Motion** | GSAP 3 (ScrollTrigger + SplitText) · Lenis smooth scroll · Framer Motion |
| **Look** | Bangers & Comic Neue lettering · halftone screens · CMYK ink palette · speed lines |
| **Demo comic** | *Cyberpunk Chronicles #01, "The Last Local Archive"*: 12 vector SVG pages |
| **Reader replica** | Spreads · manga RTL · webtoon · fit & zoom · 6 color presets · bookmarks · dialogue search · animated full screen · swipe & pinch on mobile |
| **Extension showcase** | A working browser mock that scans the demo comic, cleans its tags, packs a CBZ and lands it on a Komik shelf with its ComicInfo.xml, plus six hands-on feature panels |
| **Pages** | `/` (the comic book) · [`/privacy`](https://komik-website-taupe.vercel.app/privacy) (privacy policy for the app and extension) |
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

- **Live deployment:** [Komik website on Vercel](https://komik-website-taupe.vercel.app/)
- **Download links** are centralized in `komik-website/lib/config.ts`:
  - Installer: `https://github.com/mohitbansal25082006/KomiK/releases/latest/download/Komik-Setup.exe`
  - Releases: `https://github.com/mohitbansal25082006/KomiK/releases`
  - Extension: `https://github.com/mohitbansal25082006/KomiK/releases/latest/download/komik-downloader-1.0.0.zip`
  - Chrome Web Store: set `EXTENSION_CONFIG.chromeStoreUrl` once the listing is live, and the site switches to an **Add to Chrome** button
- **Privacy policy:** `https://komik-website-taupe.vercel.app/privacy`, the URL to give the Chrome Web Store
- **Social preview:** replace `komik-website/public/og-image.png` with any 1200×630 PNG.
- **Vercel:** import the repository, set **Root Directory** to `komik-website`, keep the Next.js preset, and deploy.
- **README artwork:** the animated banners and screenshots used on this page live in `komik-website/public/readme/`.

</details>

<img src="komik-website/public/readme/divider.svg" alt="" width="100%" />

## ⚠️ Known limitations

- **Explorer in-folder thumbnails.** Cover previews inside File Explorer need an in-process, 64-bit native COM `IThumbnailProvider` DLL. WinUI 3 apps run as standalone executables, so that handler can't be written in managed WinUI C# without a separate C++/WinRT companion. Komik generates fast cover thumbnails inside its own UI, and file associations plus direct launching are handled through normal Windows shell integration.

- **Protected readers.** KomiK Downloader saves the images a page actually shows. Readers that draw scrambled pages onto a canvas, or stream them under DRM, can't be saved as comics.
- **Chrome Web Store.** Until the listing is approved, the extension installs from the release zip with Developer mode.

## 📜 License

Komik and KomiK Downloader are released under the **MIT License**. See [`LICENSE`](LICENSE). The extension's packaged zip includes `THIRD-PARTY-NOTICES.txt` for the fonts and libraries it bundles.

## 🙏 Acknowledgements

- [WinUI 3](https://github.com/microsoft/microsoft-ui-xaml) and the [Windows App SDK](https://github.com/microsoft/WindowsAppSDK)
- [SharpCompress](https://github.com/adamhathcock/sharpcompress) for pure managed archive handling
- [Docnet.Core](https://github.com/GowenGit/docnet) (PDFium) for PDF rendering
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) and [Win2D](https://github.com/microsoft/Win2D)
- [Bangers](https://github.com/googlefonts/bangers) by The Bangers Project Authors (SIL Open Font License 1.1) for the comic lettering
- For the extension: [React](https://react.dev/), [Framer Motion](https://motion.dev/), [fflate](https://github.com/101arrowz/fflate), [pdf-lib](https://pdf-lib.js.org/), [Vite](https://vite.dev/), [Tailwind CSS](https://tailwindcss.com/), and the Comic Neue, Plus Jakarta Sans and JetBrains Mono fonts via [Fontsource](https://fontsource.org/)
- The [Windows 11 Fluent Design System](https://learn.microsoft.com/windows/apps/design/)

<div align="center">

<img src="komik-website/public/readme/the-end.svg" alt="The End... or is it?" width="100%" />

**Created & maintained by [Mohit Bansal](https://github.com/mohitbansal25082006)** · Built natively for Windows with WinUI 3 & .NET 8

<a href="https://github.com/mohitbansal25082006/KomiK/releases/latest/download/Komik-Setup.exe"><img src="https://img.shields.io/badge/⬇_DOWNLOAD-KOMIK_1.2.0-FFD700?style=for-the-badge&labelColor=0A0A0F" alt="Download Komik 1.2.0" /></a>
&nbsp;
<a href="https://github.com/mohitbansal25082006/KomiK/releases/latest/download/komik-downloader-1.0.0.zip"><img src="https://img.shields.io/badge/🧩_EXTENSION-DOWNLOADER_1.0.0-FF1F6D?style=for-the-badge&labelColor=0A0A0F" alt="Download KomiK Downloader 1.0.0" /></a>
&nbsp;
<a href="https://komik-website-taupe.vercel.app/"><img src="https://img.shields.io/badge/🌐_WEBSITE-OPEN-00C2FF?style=for-the-badge&labelColor=0A0A0F" alt="Open the Komik website" /></a>

<sub>If Komik made your reading better, drop a ⭐ on the repo. It helps other readers find it.</sub>

</div>