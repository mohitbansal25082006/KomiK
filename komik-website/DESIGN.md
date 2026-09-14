# Komik Website — Design Plan & Creative Direction (v1.1.0)

## 1. Concept: "The Living Comic Book"

The Komik site is designed as an interactive comic book, not a SaaS landing page. Every section is a chapter, every card is a panel, and every interaction answers with ink, halftone and a sound effect. The centerpiece is a **fully working replica of the Komik reader** that holds a complete, original 12-page comic, so visitors can try the product before they download it.

### Core metaphors
- **Chapters & panels**: sections open with a caption box ("Chapter 02 · The archive vault") and oversized Bangers lettering. Content lives in thick-bordered panels with hard offset shadows.
- **Sound effects as feedback**: clicks spawn POW!/ZAP! bursts, keyboard presses shout FLIP!/ZOOM!, stamps slam onto the manifesto.
- **Print textures**: Ben-Day halftone screens, speed lines, sunbursts, torn newsprint and marquee "tape" strips.
- **Real product, real data**: every number and feature on the page comes from the Komik 1.1.0 codebase and docs.

---

## 2. Palette

| Name | Hex | Use |
| :--- | :--- | :--- |
| Ink Black | `#08080A` | Page ground, panel borders, hard shadows |
| Newsprint Paper | `#F5EFE3` | Paper sections (New in 1.1, Manifesto), comic pages |
| Process Yellow | `#FFD700` | Primary CTA, caption boxes, highlights |
| Process Cyan | `#00C2FF` | Secondary accents, engine/tech tags |
| Process Magenta | `#FF1F6D` | Action accents, stamps, Download section |
| Muted Ink | `#949BA6` | Metadata and captions |

Supporting comic colors (orange `#FF7A00`, green `#2FD17A`, violet `#A78BFA`) appear only inside illustrations and collectible cards.

---

## 3. Typography

1. **Bangers**: display lettering, sound effects and headings, rendered with a 3–4px ink stroke and a hard offset shadow (`.text-comic-outline`).
2. **Comic Neue (Bold)**: speech balloons, captions and all lettering inside the demo comic.
3. **Plus Jakarta Sans**: body copy.
4. **JetBrains Mono**: keycaps, specs and technical labels.
5. **Space Grotesk**: kept as a fallback UI face.

All fonts load through `next/font/google` (self-hosted at build time, so no runtime font requests).

---

## 4. Motion Stack

| Library | Role |
| :--- | :--- |
| **GSAP 3.15 + ScrollTrigger + SplitText** | Character-by-character heading pops, the pinned horizontal "Reading Engine" strip, stamp slams, underline drawing |
| **Lenis** | Smooth scrolling driven by GSAP's ticker, so ScrollTrigger stays in sync |
| **Framer Motion 12** | React UI motion: reader flyouts, page turns, 3D tilt cards, layout animations, counters, drag stickers |
| **Pure CSS** | Intro splash (never blocks hydration), marquees, sunbursts, wobble/float loops |

Motion rules:
- Springy, snappy and physical (`back.out`, `elastic.out`, stiff springs). Nothing floaty.
- `prefers-reduced-motion` disables Lenis, the intro splash, SplitText, GSAP pins and click bursts. Content is always visible without animation.
- The intro splash plays once per browser session (`sessionStorage`).
- Scroll-linked effects never block reading. Scroll-locking only happens in reader full-screen mode and the mobile menu (via the `komik:scroll-lock` event).

---

## 5. The Interactive Reader Mockup (`components/reader/`)

A faithful recreation of Komik's WinUI 3 `MainPage` that really works:

| Feature | Desktop | Mobile |
| :--- | :--- | :--- |
| Page turns | ← → arrows, Space/PgUp/PgDn, Home/End, click the left or right 25%, edge buttons | Swipe, tap the left or right 25% |
| Two-page spread (D) | Cover isolated on the right, pairs 2–3 … 10–11, back cover alone | Available from the ⋯ sheet (defaults to single page) |
| Manga RTL (Ctrl+R) | Swaps spread order, inverts arrow keys and scrubber direction | ⋯ sheet |
| Webtoon mode (V) | Continuous vertical scroll with scrubber sync | ⋯ sheet |
| Fit W / H / 1:1 (W/H/A) + zoom | Toolbar, Ctrl +/−/0, Ctrl+wheel, drag to pan | ⋯ sheet, pinch to zoom |
| Color correction | 6 presets + brightness/contrast/warmth sliders (CSS filter stand-in for the GPU LUT) | Palette sheet |
| Bookmarks (Ctrl+D) | Toggle, notes, jump list, persisted in `localStorage` | Toolbar + ⋯ sheet |
| Offline OCR search (Ctrl+F) | Searches the comic's real script, jumps to pages, selectable text overlay with Copy | Search sheet |
| Resume | "Resumed at page X" toast on return | Same |
| Scrubber | Live page-thumbnail preview on hover/drag | Drag to scrub |
| Window chrome | Minimize/close collapse to a restore card, maximize = full screen | Same |
| Full screen (F / F11 / double-click) | Fullscreen API + fixed overlay, auto-hiding chrome, Esc to exit | Fullscreen overlay, tap center to toggle chrome |
| Theme | Dark / light reader chrome | ⋯ sheet |

The reader measures its own container (not the viewport), so it switches to the compact mobile layout whenever it is narrower than 640px.

---

## 6. The Demo Comic (`components/comic/`)

**Cyberpunk Chronicles #01, "The Last Local Archive"**: an original 12-page story written for this site. Courier Kira "Vex" and her drone Byte protect the last offline comic archive from Nimbus Corp's cloud enforcer, Agent Stratus.

- `kit.tsx`: page frame (600×900 native SVG), panels, speech/shout/robot balloons, captions, SFX lettering, bursts, speed lines, rain, halftone patterns. IDs are scoped per page instance with `useId`, so the same page can render several times (reader, thumbnails, demos).
- `art.tsx`: a pose-based character rig (Vex, Stratus, Orrin, citizens) plus portrait close-ups, Byte, drones, skylines, the Nimbus blimp, bookshelves and a retro PC.
- `pages.tsx`: the 12 pages. All lettering lives in `SCRIPT`, which also powers the reader's dialogue search and OCR overlay.

Because pages are vector SVG, they stay razor sharp at any zoom and in full screen.

---

## 7. Page Architecture

1. **Intro splash**: yellow/magenta split panel with a "KOMIK!" burst (CSS only, once per session).
2. **Navbar**: Bangers wordmark, active-section pill (`layoutId`), CMYK scroll-progress ink bar, comic-tile mobile menu.
3. **Hero**: SplitText headline "Your comics. Your PC. Zero cloud.", draggable stickers, rotating speed lines, the reader mockup landing in 3D as you scroll, crossing marquee tapes.
4. **By the numbers**: count-up stat panels (zeros count *down* from 99).
5. **Chapter 02 · Archive vault**: 8 collectible format cards (3D tilt, holographic sheen, flip for engine details) + the animated "CBZ-O-Matic 3000" converter.
6. **Chapter 03 · Reading engine**: a pinned horizontal comic strip on desktop (stacked on mobile) with five live demos: spread pairing, the 6-worker webtoon decoder, a before/after LUT slider, a self-typing library search, and resume/bookmarks.
7. **Special edition · New in 1.1.0**: a paper bento page covering Series & Volumes, Stats & Komik Wrapped, the Duplicate Manager, offline OCR, `.komikbackup` backups, and the responsive library with window memory.
8. **Manifesto**: a torn newsprint editorial with slamming "NO CLOUD / NO LOGIN / NO SPYING / NO DELETES" stamps.
9. **Appendix · Keyboard**: press real keys (or tap keycaps) to see each shortcut's SFX and description.
10. **Final panel · Download**: magenta sunburst, burst-backed CTA, spec cards, taped SmartScreen note.
11. **Footer**: "THE END" with Byte waving goodbye.

---

## 8. Responsive & Accessibility Notes

- Mobile-first layouts at every breakpoint. The pinned horizontal strip only activates at `lg` and above with motion allowed.
- All interactive demos are buttons or ranges with labels. The reader root is focusable with a descriptive `aria-label`.
- Decorative layers are `aria-hidden` and `pointer-events-none`.
- The site stays offline-friendly: no analytics, no third-party runtime requests, and fonts are bundled at build time.
