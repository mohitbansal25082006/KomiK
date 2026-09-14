# Komik Website — Design Plan & Creative Direction

## 1. Visual Concept: "The Authentic Comic Page"

The Komik marketing site is designed not as a generic dark SaaS landing page with cyan glowing cards, but as an **authentic, tactile comic book page**. The layout directly models physical graphic novel publishing: deep ink gutters, crisp 90-degree panel frames, diegetic caption boxes pinned directly to panel borders, warm newsprint paper interiors, and four-color process (CMYK) color pops.

### Core Metaphors
- **Panels & Gutters, Not Cards & Gaps**: Individual sections and features are rendered as asymmetric comic panels bound by solid black ink gutters (`border-[3px] border-black`). Borders touch or align tightly rather than floating with uniform rounded gaps.
- **Diegetic Border Captions**: Generic floating tracked-out SaaS eyebrow labels (`CHAPTER II · ...`) are entirely replaced by **diegetic caption boxes** physically overlapping and pinned to panel frame edges (`absolute -top-3.5 left-6 caption-box`).
- **Paper & Ink Dual Canvas**: To escape the generic "dark grey SaaS" trap, editorial and collector panels utilize warm tactile newsprint paper (`#F5EFEB`) with dense black ink typography (`#08080A`), while nocturnal panels simulate dark-room reading with real-time 256-entry Look-Up Table (LUT) warmth.
- **Visual Hierarchy for Core Pillars**: The two most critical technical capabilities—**Universal Format Support** (CBZ, CBR, CB7, PDF) and the **Native Reading Engine** (two-page spreads, Manga RTL, LUT color correction)—command dominant, oversized splash panels rather than sharing equal visual weight with secondary settings.

---

## 2. Color Palette (Named Hex Values)

The palette draws strictly from physical comic book printing presses and dark-room nocturnal reading:

| Color Name | Hex Code | Purpose & Application |
| :--- | :--- | :--- |
| **True Ink Black** | `#08080A` | Dense, true printing ink background and structural page foundation. |
| **Gutter Black** | `#000000` | 3px razor-sharp structural panel borders and drop-shadows. |
| **Newsprint Paper** | `#F5EFEB` | Warm uncoated physical paper tone for editorial panels and daytime reader canvas. |
| **Newsprint White** | `#F4EFEA` | High-contrast headline text on dark panels. |
| **Muted Ink** | `#949BA6` | Secondary technical metadata and caption details. |
| **Process Yellow (Amber)** | `#FFD700` | Classic comic narration boxes, primary download action button, and night mode warmth. |
| **Process Cyan** | `#00A3E0` | Technical engine tags, format badges, and active state indicators. |
| **Process Magenta** | `#E60050` | Action badges, format conversion stamps, and alert highlights. |

### Excluded Generic Patterns
- ❌ **No SaaS neon blue/cyan gradients** (`#00F0FF` / `#6366F1`).
- ❌ **No rounded grey cards** (`rounded-xl` / `rounded-2xl`). All panels maintain sharp 90° corners.
- ❌ **No blurry diffuse shadows** (`box-shadow: 0 20px 25px rgba(...)`). Replaced with crisp, solid offset block shadows (`shadow-[6px_6px_0px_#000000]`).
- ❌ **No floating tracked-out eyebrow labels** in empty space.

---

## 3. Typography System

The typographic pairing balances bold graphic novel display presence with modern desktop software precision:

1. **Display & Splash Headlines**: **`Space Grotesk`** (Bold / Black, 800–900 weight)
   - Ultra-bold, punchy, graphic letterforms that evoke classic comic book titles and splash page banners.
2. **Subheadings & Technical Badges**: **`Space Grotesk`** (SemiBold / Bold)
   - Geometric confidence fitting a high-performance Windows App SDK application.
3. **Body Copy & Guides**: **`Plus Jakarta Sans`** (Medium, 500 weight)
   - High legibility, neutral reading rhythm, and crisp letter spacing across both paper and dark panels.
4. **Captions & Shortcuts**: **`JetBrains Mono`** (Bold / Black)
   - Vintage comic narration tags, mechanical hotkey badges, and technical format parameters.

---

## 4. Motion System & Interactive WinUI 3 Simulation

The motion and interactive design directly mirrors the physical application documented across the Komik project:

- **Panel Drop-In Entrance**: On initial load, hero panels assemble onto the drafting table with a snappy spring curve (`stiffness: 260, damping: 24`) with slight micro-rotation (`rotate: -0.4deg` to `0deg`).
- **Tactile Button Impact Snap**:
  - Hover: Shifts `-3px, -3px` while casting an expanded `8px 8px 0px #000000` solid block shadow.
  - Active Press: Slams `+2px, +2px` with a compressed `2px 2px 0px #000000` shadow.
- **Pixel-Accurate WinUI 3 Fluent Reader Mockup**:
  - **Windows 11 Native Chrome**: Features rounded top corners, title bar height of 36px, native Windows App SDK icon, and standard Windows 11 window controls (minimize `—`, maximize `□`, close `✕` turning red `#E81123` on hover; explicitly avoiding macOS traffic-light dots).
  - **Mica Backdrop**: Translucent acrylic/Mica background (`bg-[#121316]/95 backdrop-blur-xl`) with fine noise texture overlay.
  - **Floating Acrylic Toolbar Pill**: Faithfully recreates the `FloatingPillCardStyle` from `MainPage.xaml` in its revealed state, displaying Open, Fit Height / Fit Width toggles, 2-Page Spread toggle, Manga RTL toggle, Night Mode LUT toggle, zoom buttons, and fullscreen shortcuts.
  - **Original Illustrated Comic Art**: Replaces wireframe placeholder boxes with bespoke SVG vector comic art (Cover page with giant yellow moon, skyscraper silhouettes, masked vigilante with glowing visor, Comics Code authority seal parody, and barcode; Story Page A with metropolis establishing shot and terminal hacker silhouette; Story Page B with alley leap and splash landing).
  - **Live Functional Toggles**:
    - **Fit Height vs. Fit Width**: Recomposes page dimensions live (vertical constrained height vs. wide horizontal expansion).
    - **Single-Page vs. 2-Page Spread**: Toggles between a single centered page and adjacent dual pages with a visible spine gutter.
    - **Cover-Page Isolation**: Scrubbing or navigating to Page 1 isolates the cover page as a single page even in spread mode, matching Komik's real cover page isolation engine.
    - **Western vs. Manga RTL**: Physically swaps the positions of Page A and Page B on the canvas.
    - **Night Mode LUT**: Applies real-time GPU warmth LUT shader filter (`sepia(0.55) saturate(1.35) hue-rotate(-15deg) brightness(0.92) contrast(1.05)`) directly across the illustrated comic pages.
    - **Page Scrubber Overlay**: Floating bottom pill slider that is fully draggable and clickable, updating page count and percentage live.
- **Reduced Motion Accessibility**:
  - When `prefers-reduced-motion: reduce` is enabled, all animations and transforms are zeroed (`duration: 0.001ms`), ensuring instant display with zero visual brokenness.

---

## 5. Layout Architecture & Hierarchy

1. **Header Panel**:
   - Ink black background with a heavy 3px black bottom gutter, high-contrast navigation links, and a high-contrast GitHub release button alongside the "Get Komik" action button.
   - **Komik Brand Crest (`<KomikLogo />`)**: High-contrast, tactile comic emblem set within a deep ink container (`bg-[#121316]`) with an offset golden-amber comic shadow (`shadow-[3px_3px_0px_#FFD700]`). Features an open dual-page comic spread with a golden-amber cover flap, bold black "K" lettermark, crimson reading ribbon, and cyan/magenta story panels with pure white newsprint paper. Provides 100% visual contrast across any background without yellow-on-yellow washing out.
2. **Hero Splash Spread**:
   - Double-column comic splash page. Left: graphic novel typography, clear guarantees, and primary CTA. Right: pixel-accurate WinUI 3 reader mockup with live functional toggles, window title bar with Komik crest, and original illustrated comic art.
3. **Dominant Format Archive Vault (Collector's 8-Format Arsenal)**:
   - Full 8-format unified collector's rack with substantial visual weight, high-contrast labels, and clear compression descriptions across `.CBZ`, `.CBR`, `.CB7`, `.PDF`, `.ZIP`, `.RAR`, `.7Z`, and `FOLDERS`. Integrated with the Universal Lossless CBZ Conversion Lab.
4. **Asymmetric Reading Engine Spread**:
   - Dominant full-width panel dedicated to two-page spread pairing (cover page offset preservation) and Manga RTL mode, paired with asymmetric secondary panels for SQLite library indexing and session bookmarks.
5. **Creator's Editorial Manifesto**:
   - Tactile physical newsprint paper slab (`bg-paper text-black`) establishing Komik's local-first pledge: 100% offline, zero accounts, zero telemetry, non-destructive SQLite.
6. **Keyboard Navigation Matrix**:
   - Tactile keycap battle-cards for distraction-free reading shortcuts.
7. **Download Command Center**:
   - High-contrast command box, direct setup executable download, technical specs, and plain-language Windows SmartScreen reassurance note.
8. **Colophon / Footer**:
   - Terminates cleanly with zero dead space, acknowledging creator Mohit Bansal, MIT license, and GitHub release links.

---

## 6. Self-Critique: Before vs. After Overhaul

| Design Aspect | Before (Generic SaaS Template) | After (Authentic Comic Page) |
| :--- | :--- | :--- |
| **Section Labels** | Floating yellow eyebrow tags in empty margins (`CHAPTER II · ...`). | Diegetic caption boxes physically pinned to panel frame edges (`caption-box`). |
| **Component Layout** | Uniform rounded dark cards (`rounded-xl`) arranged in identical 2x2 or 3x3 CSS grids. | Asymmetric comic panels with sharp 90° corners, shared 3px ink gutters, and dominant splash features. |
| **Color Palette** | Generic dark navy (`#08090C`, `#0F131A`) with neon cyan glow accents (`#00F0FF`). | True ink black (`#08080A`), warm newsprint paper (`#F5EFEB`), and authentic CMYK process printing color pops. |
| **Visual Hierarchy** | Formats and reading engine shared equal card size with minor features. | Formats and reading engine occupy massive, commanding multi-column splash panels. |
| **Motion Feedback** | Soft CSS scale transitions. | Snappy spring-physics panel drops and hard mechanical impact button states. |
| **Layout Bugs** | Stray dev overlay pills on left edge; white void below footer. | Dev indicators disabled; `html` & `body` backgrounds unified; zero dead scroll space. |
