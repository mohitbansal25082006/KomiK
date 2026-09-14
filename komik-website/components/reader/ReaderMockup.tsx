"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { AnimatePresence, motion, useReducedMotion } from "framer-motion";
import {
  ArrowLeftRight,
  BookOpen,
  Bookmark,
  BookmarkCheck,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  Ellipsis,
  Library,
  Maximize2,
  Minimize2,
  Moon,
  MoveHorizontal,
  MoveVertical,
  Palette,
  Scan,
  ScanText,
  ScrollText,
  Sun,
  ZoomIn,
  ZoomOut,
} from "lucide-react";
import KomikLogo from "@/components/KomikLogo";
import { COMIC_FILE, ComicPage } from "@/components/comic/pages";
import { PAGE_H, PAGE_W } from "@/components/comic/kit";
import { BookmarksPanel, ColorPanel, OcrOverlay, SearchPanel, ViewPanel } from "./ReaderPanels";
import {
  ColorState,
  DEFAULT_COLOR,
  Direction,
  FitMode,
  PAGE_TITLES,
  PanelId,
  TOTAL_PAGES,
  ViewMode,
  clamp,
  cssFilter,
  spreadOf,
} from "./readerModel";

const STORE_KEY = "komik-demo-reader-v1";
const GAP = 6;

type WindowState = "open" | "minimized" | "closed";

export default function ReaderMockup() {
  const reduceMotion = useReducedMotion();
  const rootRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLDivElement>(null);

  const [rootW, setRootW] = useState(1024);
  const [viewport, setViewport] = useState({ w: 900, h: 560 });
  const compact = rootW < 640;

  const [page, setPage] = useState(1);
  const [navDir, setNavDir] = useState(1);
  const [spread, setSpread] = useState(true);
  const [fit, setFitState] = useState<FitMode>("height");
  const [view, setView] = useState<ViewMode>("paged");
  const [dir, setDir] = useState<Direction>("ltr");
  const [zoom, setZoomState] = useState(1);
  const [color, setColor] = useState<ColorState>(DEFAULT_COLOR);
  const [bookmarks, setBookmarks] = useState<Record<number, string>>({});
  const [theme, setTheme] = useState<"dark" | "light">("dark");
  const [fullscreen, setFullscreen] = useState(false);
  const [panel, setPanel] = useState<PanelId>(null);
  const [ocrOpen, setOcrOpen] = useState(false);
  const [chromeVisible, setChromeVisible] = useState(true);
  const [toast, setToast] = useState<{ id: number; msg: string } | null>(null);
  const [windowState, setWindowState] = useState<WindowState>("open");
  const [hydrated, setHydrated] = useState(false);

  const setZoom = useCallback((z: number) => setZoomState(clamp(Math.round(z * 100) / 100, 0.5, 3)), []);
  const setFit = useCallback((f: FitMode) => {
    setFitState(f);
    setZoomState(1);
  }, []);

  const showToast = useCallback((msg: string) => setToast({ id: Date.now(), msg }), []);

  useEffect(() => {
    if (!toast) return;
    const t = setTimeout(() => setToast(null), 2600);
    return () => clearTimeout(t);
  }, [toast]);

  /* ----------------------------- sizing ---------------------------- */

  useEffect(() => {
    const root = rootRef.current;
    const canvas = canvasRef.current;
    if (!root) return;
    const ro = new ResizeObserver(() => {
      setRootW(root.clientWidth);
      if (canvasRef.current) setViewport({ w: canvasRef.current.clientWidth, h: canvasRef.current.clientHeight });
    });
    ro.observe(root);
    if (canvas) ro.observe(canvas);
    return () => ro.disconnect();
  }, [windowState, fullscreen]);

  /* ------------------------ hydrate & persist ----------------------- */

  useEffect(() => {
    const narrow = (rootRef.current?.clientWidth ?? 1024) < 640;
    if (narrow) {
      setSpread(false);
      setFitState("width");
    }
    let resumed = 0;
    try {
      const raw = localStorage.getItem(STORE_KEY);
      if (raw) {
        const s = JSON.parse(raw);
        if (typeof s.page === "number" && s.page > 1 && s.page <= TOTAL_PAGES) resumed = s.page;
        if (s.bookmarks && typeof s.bookmarks === "object") setBookmarks(s.bookmarks);
        if (s.theme === "light" || s.theme === "dark") setTheme(s.theme);
      }
    } catch {
      /* storage unavailable */
    }
    if (resumed) {
      setPage(resumed);
      showToast(`Resumed at page ${resumed}`);
    }
    setHydrated(true);
  }, [showToast]);

  useEffect(() => {
    if (!hydrated) return;
    try {
      localStorage.setItem(STORE_KEY, JSON.stringify({ page, bookmarks, theme }));
    } catch {
      /* storage unavailable */
    }
  }, [page, bookmarks, theme, hydrated]);

  /* --------------------------- geometry ----------------------------- */

  const pad = fullscreen ? (compact ? 8 : 16) : compact ? 10 : 18;
  const availW = Math.max(120, viewport.w - pad * 2);
  const availH = Math.max(120, viewport.h - pad * 2);
  const isSpread = spread && view === "paged";
  const widthSlots = isSpread ? 2 : 1;
  const baseScale =
    fit === "actual" ? 1 : fit === "width" ? (availW - (widthSlots - 1) * GAP) / (PAGE_W * widthSlots) : view === "webtoon" ? Math.min(availH / PAGE_H, availW / PAGE_W) : availH / PAGE_H;
  const scale = Math.max(0.05, baseScale * zoom);
  const pw = PAGE_W * scale;
  const ph = PAGE_H * scale;

  const shown = useMemo(() => spreadOf(page, isSpread), [page, isSpread]);
  const slots: (number | null)[] = useMemo(() => {
    let row: (number | null)[] = [...shown];
    // physical-book placement: the cover sits on the right-hand side, the back cover on the left
    if (isSpread && shown.length === 1) row = shown[0] === 1 ? [null, shown[0]] : [shown[0], null];
    return dir === "rtl" ? row.reverse() : row;
  }, [shown, isSpread, dir]);
  const spreadKey = shown.join("-");
  const contentW = view === "paged" ? slots.length * pw + (slots.length - 1) * GAP + pad * 2 : pw + pad * 2;
  const hOverflow = contentW > viewport.w + 1;

  /* -------------------------- navigation ---------------------------- */

  const scrollWebtoonTo = useCallback(
    (p: number, smooth: boolean) => {
      const el = canvasRef.current;
      if (!el) return;
      el.scrollTo({ top: pad + (p - 1) * (ph + GAP), behavior: smooth && !reduceMotion ? "smooth" : "auto" });
    },
    [pad, ph, reduceMotion],
  );

  const goTo = useCallback(
    (target: number, opts?: { smooth?: boolean }) => {
      const p = clamp(target, 1, TOTAL_PAGES);
      const normalized = view === "paged" ? spreadOf(p, isSpread)[0] : p;
      setNavDir(normalized >= page ? 1 : -1);
      setPage(normalized);
      if (view === "webtoon") scrollWebtoonTo(p, opts?.smooth ?? true);
      else canvasRef.current?.scrollTo({ top: 0, left: 0 });
    },
    [view, isSpread, page, scrollWebtoonTo],
  );

  const next = useCallback(() => {
    if (view === "webtoon") return goTo(page + 1);
    const last = shown[shown.length - 1];
    if (last < TOTAL_PAGES) goTo(last + 1);
  }, [view, goTo, page, shown]);

  const prev = useCallback(() => {
    if (view === "webtoon") return goTo(page - 1);
    const first = shown[0];
    if (first > 1) goTo(spreadOf(first - 1, isSpread)[0]);
  }, [view, goTo, page, shown, isSpread]);

  const turnLeft = dir === "ltr" ? prev : next;
  const turnRight = dir === "ltr" ? next : prev;

  // normalise the current page whenever pairing changes
  useEffect(() => {
    if (view === "paged") setPage((p) => spreadOf(p, isSpread)[0]);
  }, [isSpread, view]);

  // keep webtoon scroll anchored when entering the mode or rescaling
  const lastWebtoonScale = useRef<number | null>(null);
  useEffect(() => {
    if (view !== "webtoon") {
      lastWebtoonScale.current = null;
      return;
    }
    if (lastWebtoonScale.current !== scale) {
      lastWebtoonScale.current = scale;
      requestAnimationFrame(() => scrollWebtoonTo(page, false));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [view, scale]);

  const onCanvasScroll = () => {
    if (view !== "webtoon") return;
    const el = canvasRef.current;
    if (!el) return;
    const idx = Math.floor((el.scrollTop + el.clientHeight * 0.35 - pad) / (ph + GAP)) + 1;
    const p = clamp(idx, 1, TOTAL_PAGES);
    if (p !== page) setPage(p);
  };

  const isMarked = page in bookmarks;
  const toggleBookmark = useCallback(() => {
    setBookmarks((b) => {
      const copy = { ...b };
      if (page in copy) {
        delete copy[page];
        showToast(`Bookmark removed · page ${page}`);
      } else {
        copy[page] = "";
        showToast(`Bookmarked page ${page}`);
      }
      return copy;
    });
  }, [page, showToast]);

  const toggleSpread = useCallback(() => {
    if (view === "webtoon") setView("paged");
    setSpread((s) => {
      showToast(!s || view === "webtoon" ? "Two-page spread · cover isolated" : "Single page");
      return view === "webtoon" ? true : !s;
    });
  }, [view, showToast]);

  const toggleWebtoon = useCallback(() => {
    setView((v) => {
      const nextView = v === "webtoon" ? "paged" : "webtoon";
      showToast(nextView === "webtoon" ? "Webtoon continuous scroll" : "Paged reading");
      return nextView;
    });
    setZoomState(1);
  }, [showToast]);

  const toggleDir = useCallback(() => {
    setDir((d) => {
      showToast(d === "ltr" ? "Manga mode · right-to-left" : "Western mode · left-to-right");
      return d === "ltr" ? "rtl" : "ltr";
    });
  }, [showToast]);

  /* --------------------------- fullscreen --------------------------- */

  const enterFullscreen = useCallback(() => {
    setFullscreen(true);
    setChromeVisible(true);
    const el = rootRef.current;
    if (el && document.fullscreenEnabled && !document.fullscreenElement) {
      el.requestFullscreen?.().catch(() => undefined);
    }
  }, []);

  const exitFullscreen = useCallback(() => {
    setFullscreen(false);
    setChromeVisible(true);
    if (document.fullscreenElement) document.exitFullscreen?.().catch(() => undefined);
  }, []);

  const toggleFullscreen = useCallback(() => (fullscreen ? exitFullscreen() : enterFullscreen()), [fullscreen, enterFullscreen, exitFullscreen]);

  useEffect(() => {
    const onChange = () => {
      if (!document.fullscreenElement) setFullscreen(false);
    };
    document.addEventListener("fullscreenchange", onChange);
    return () => document.removeEventListener("fullscreenchange", onChange);
  }, []);

  useEffect(() => {
    document.dispatchEvent(new CustomEvent("komik:scroll-lock", { detail: fullscreen }));
    const prevOverflow = document.body.style.overflow;
    if (fullscreen) document.body.style.overflow = "hidden";
    if (fullscreen) rootRef.current?.focus({ preventScroll: true });
    return () => {
      document.body.style.overflow = prevOverflow;
      if (fullscreen) document.dispatchEvent(new CustomEvent("komik:scroll-lock", { detail: false }));
    };
  }, [fullscreen]);

  // auto-hide chrome while reading full screen
  const hideTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const pokeChrome = useCallback(() => {
    if (!fullscreen) return;
    setChromeVisible(true);
    if (hideTimer.current) clearTimeout(hideTimer.current);
    hideTimer.current = setTimeout(() => setChromeVisible(false), 2800);
  }, [fullscreen]);
  useEffect(() => {
    if (fullscreen && !compact) pokeChrome();
    return () => {
      if (hideTimer.current) clearTimeout(hideTimer.current);
    };
  }, [fullscreen, compact, pokeChrome]);
  const chromeShown = !fullscreen || chromeVisible || panel !== null;

  /* ---------------------------- keyboard ---------------------------- */

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const root = rootRef.current;
      if (!root || windowState !== "open") return;
      const active = fullscreen || root.contains(document.activeElement);
      if (!active) return;
      const target = e.target as HTMLElement;
      const typing = target.tagName === "INPUT" && (target as HTMLInputElement).type !== "range";
      const ctrl = e.ctrlKey || e.metaKey;

      if (e.key === "Escape") {
        if (panel) setPanel(null);
        else if (ocrOpen) setOcrOpen(false);
        else if (fullscreen) exitFullscreen();
        return;
      }
      if (typing) return;

      const run = (action: () => void) => {
        e.preventDefault();
        pokeChrome();
        action();
      };

      if (ctrl) {
        const ctrlActions: Record<string, () => void> = {
          d: toggleBookmark,
          b: toggleBookmark,
          r: toggleDir,
          f: () => setPanel("search"),
          "=": () => setZoom(zoom + 0.1),
          "+": () => setZoom(zoom + 0.1),
          "-": () => setZoom(zoom - 0.1),
          "0": () => setZoom(1),
        };
        const action = ctrlActions[e.key.toLowerCase()];
        if (action) run(action);
        return;
      }
      if (e.altKey) return;

      const keyActions: Record<string, () => void> = {
        PageDown: next,
        PageUp: prev,
        " ": e.shiftKey ? prev : next,
        Home: () => goTo(1),
        End: () => goTo(TOTAL_PAGES),
        F11: toggleFullscreen,
        d: toggleSpread,
        v: toggleWebtoon,
        w: () => setFit("width"),
        h: () => setFit("height"),
        a: () => setFit("actual"),
        f: toggleFullscreen,
      };
      if (view === "paged") {
        keyActions.ArrowLeft = turnLeft;
        keyActions.ArrowRight = turnRight;
      }
      const action = keyActions[e.key] ?? keyActions[e.key.toLowerCase()];
      if (action) run(action);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [windowState, fullscreen, panel, ocrOpen, exitFullscreen, pokeChrome, toggleBookmark, toggleDir, setZoom, zoom, view, turnLeft, turnRight, next, prev, goTo, toggleFullscreen, toggleSpread, toggleWebtoon, setFit]);

  /* ------------------ pointer: click zones, pan, swipe, pinch ------------------ */

  const pointers = useRef(new Map<number, { x: number; y: number }>());
  const gesture = useRef<{
    x: number;
    y: number;
    sl: number;
    st: number;
    t: number;
    moved: boolean;
    type: string;
    pinch?: { dist: number; zoom: number };
  } | null>(null);

  const overflowX = () => {
    const el = canvasRef.current;
    return !!el && el.scrollWidth > el.clientWidth + 2;
  };
  const overflowAny = () => {
    const el = canvasRef.current;
    return !!el && (el.scrollWidth > el.clientWidth + 2 || el.scrollHeight > el.clientHeight + 2);
  };

  const onPointerDown = (e: React.PointerEvent<HTMLDivElement>) => {
    rootRef.current?.focus({ preventScroll: true });
    if (panel) setPanel(null);
    const el = canvasRef.current!;
    pointers.current.set(e.pointerId, { x: e.clientX, y: e.clientY });
    if (pointers.current.size === 2) {
      const [a, b] = [...pointers.current.values()];
      gesture.current = { x: e.clientX, y: e.clientY, sl: el.scrollLeft, st: el.scrollTop, t: Date.now(), moved: true, type: "touch", pinch: { dist: Math.hypot(a.x - b.x, a.y - b.y), zoom } };
      return;
    }
    gesture.current = { x: e.clientX, y: e.clientY, sl: el.scrollLeft, st: el.scrollTop, t: Date.now(), moved: false, type: e.pointerType };
    if (e.pointerType === "mouse" && e.button === 0 && overflowAny()) el.setPointerCapture(e.pointerId);
  };

  const onPointerMove = (e: React.PointerEvent<HTMLDivElement>) => {
    pokeChrome();
    if (!pointers.current.has(e.pointerId)) return;
    pointers.current.set(e.pointerId, { x: e.clientX, y: e.clientY });
    const g = gesture.current;
    if (!g) return;
    if (g.pinch && pointers.current.size >= 2) {
      const [a, b] = [...pointers.current.values()];
      const d = Math.hypot(a.x - b.x, a.y - b.y);
      setZoom(g.pinch.zoom * (d / g.pinch.dist));
      return;
    }
    const dx = e.clientX - g.x;
    const dy = e.clientY - g.y;
    if (Math.abs(dx) > 6 || Math.abs(dy) > 6) g.moved = true;
    if (g.type === "mouse" && g.moved && canvasRef.current?.hasPointerCapture(e.pointerId)) {
      canvasRef.current.scrollLeft = g.sl - dx;
      canvasRef.current.scrollTop = g.st - dy;
    }
  };

  const onPointerUp = (e: React.PointerEvent<HTMLDivElement>) => {
    const g = gesture.current;
    pointers.current.delete(e.pointerId);
    if (canvasRef.current?.hasPointerCapture(e.pointerId)) canvasRef.current.releasePointerCapture(e.pointerId);
    if (!g) return;
    if (g.pinch) {
      if (pointers.current.size === 0) gesture.current = null;
      return;
    }
    gesture.current = null;
    const dx = e.clientX - g.x;
    const dy = e.clientY - g.y;
    const rect = canvasRef.current!.getBoundingClientRect();

    if (!g.moved) {
      const rx = (e.clientX - rect.left) / rect.width;
      if (view === "paged" && rx < 0.25) return turnLeft();
      if (view === "paged" && rx > 0.75) return turnRight();
      if (fullscreen || compact) setChromeVisible((v) => !v);
      return;
    }
    if (g.type !== "mouse" && view === "paged" && !overflowX() && Math.abs(dx) > 50 && Math.abs(dx) > Math.abs(dy) * 1.4 && Date.now() - g.t < 700) {
      if (dx < 0) turnRight();
      else turnLeft();
    }
  };

  const onPointerCancel = (e: React.PointerEvent<HTMLDivElement>) => {
    pointers.current.delete(e.pointerId);
    if (pointers.current.size === 0) gesture.current = null;
  };

  const onDoubleClick = (e: React.MouseEvent<HTMLDivElement>) => {
    const rect = canvasRef.current!.getBoundingClientRect();
    const rx = (e.clientX - rect.left) / rect.width;
    if (rx >= 0.25 && rx <= 0.75) toggleFullscreen();
  };

  const onWheel = (e: React.WheelEvent<HTMLDivElement>) => {
    if (e.ctrlKey) {
      e.preventDefault();
      setZoom(zoom * (e.deltaY < 0 ? 1.08 : 0.92));
    }
  };

  // non-passive wheel listener so ctrl+wheel zoom doesn't zoom the whole page
  useEffect(() => {
    const el = canvasRef.current;
    if (!el) return;
    const block = (e: WheelEvent) => {
      if (e.ctrlKey) e.preventDefault();
    };
    el.addEventListener("wheel", block, { passive: false });
    return () => el.removeEventListener("wheel", block);
  }, [windowState]);

  /* ----------------------------- render ----------------------------- */

  const filter = cssFilter(color);
  const night = color.preset === "night";
  const canvasBg = night ? "var(--r-canvas-night)" : "var(--r-canvas)";
  const progress = Math.round((page / TOTAL_PAGES) * 100);
  const pageLabel = view === "paged" && shown.length === 2 ? `${shown[0]}–${shown[1]}` : `${page}`;
  const pageAnim = reduceMotion
    ? { initial: { opacity: 0 }, animate: { opacity: 1 } }
    : {
        initial: { opacity: 0.2, x: 70 * navDir * (dir === "rtl" ? -1 : 1), rotateY: -12 * navDir * (dir === "rtl" ? -1 : 1) },
        animate: { opacity: 1, x: 0, rotateY: 0 },
      };

  const themeVars = theme === "dark" ? "reader-theme-dark" : "reader-theme-light";

  if (windowState !== "open") {
    return (
      <div ref={rootRef} className={`${themeVars} relative flex min-h-[260px] items-center justify-center rounded-xl border-[3px] border-black bg-[#101014] p-6 shadow-[10px_10px_0px_#000]`}>
        <div className="bg-halftone pointer-events-none absolute inset-0 opacity-20" />
        <motion.div initial={{ scale: 0.8, opacity: 0 }} animate={{ scale: 1, opacity: 1 }} className="relative w-full max-w-sm rounded-xl border border-white/15 bg-[#1E1E24] p-5 text-center shadow-2xl">
          <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-lg bg-black/40">
            <KomikLogo size={36} />
          </div>
          <p className="font-semibold text-white">{windowState === "minimized" ? "Komik is minimized" : "Komik closed"}</p>
          <p className="mt-1 text-sm text-white/60">Your place (page {page}) is saved locally. No cloud required.</p>
          <button type="button" onClick={() => setWindowState("open")} className="btn-comic-primary mt-4 inline-flex items-center gap-2 px-5 py-2 text-sm uppercase">
            <BookOpen className="h-4 w-4" /> {windowState === "minimized" ? "Restore window" : "Reopen comic"}
          </button>
        </motion.div>
      </div>
    );
  }

  const iconBtn = (active = false) =>
    `inline-flex h-8 min-w-8 shrink-0 items-center justify-center gap-1.5 rounded-md px-2 text-[12px] font-medium transition-colors ${
      active ? "bg-[var(--r-accent)] text-black" : "text-[var(--r-text)] hover:bg-[var(--r-hover)]"
    }`;
  const sep = <span className="mx-0.5 h-5 w-px shrink-0 bg-[var(--r-border)]" />;

  return (
    <div
      ref={rootRef}
      tabIndex={0}
      aria-label="Interactive Komik reader demo. Click, then use the keyboard: arrows turn pages, D spread, V webtoon, W/H/A fit, Ctrl+R manga, Ctrl+D bookmark, Ctrl+F search, F fullscreen."
      onMouseMove={pokeChrome}
      className={`${themeVars} group/reader flex flex-col overflow-hidden text-[var(--r-text)] outline-none focus-visible:ring-4 focus-visible:ring-amber/70 ${
        fullscreen ? "fixed inset-0 z-[300] h-[100dvh] w-screen bg-[var(--r-window)]" : "relative rounded-xl border-[3px] border-black bg-[var(--r-window)] shadow-[10px_10px_0px_#000]"
      }`}
    >
      {/* ---------------- Title bar ---------------- */}
      <div className={`flex h-9 shrink-0 select-none items-center justify-between border-b border-[var(--r-border)] bg-[var(--r-titlebar)] pl-3 transition-all duration-300 ${chromeShown ? "" : fullscreen ? "-mt-9 opacity-0" : ""}`}>
        <div className="flex min-w-0 items-center gap-2">
          <KomikLogo size={16} />
          <span className="truncate text-[12px] text-[var(--r-text)]/90">
            {COMIC_FILE} <span className="text-[var(--r-muted)]">— Komik</span>
          </span>
        </div>
        <div className="flex h-full shrink-0 items-center">
          <button type="button" aria-label="Minimize" title="Minimize" onClick={() => (exitFullscreen(), setWindowState("minimized"))} className="flex h-9 w-11 items-center justify-center text-[var(--r-text)]/70 hover:bg-[var(--r-hover)]">
            <span className="h-px w-2.5 bg-current" />
          </button>
          <button type="button" aria-label={fullscreen ? "Restore" : "Maximize"} title={fullscreen ? "Restore (Esc)" : "Maximize (F11)"} onClick={toggleFullscreen} className="flex h-9 w-11 items-center justify-center text-[var(--r-text)]/70 hover:bg-[var(--r-hover)]">
            {fullscreen ? <Minimize2 className="h-3 w-3" /> : <span className="h-2.5 w-2.5 border border-current" />}
          </button>
          <button type="button" aria-label="Close" title="Close" onClick={() => (exitFullscreen(), setWindowState("closed"))} className="flex h-9 w-11 items-center justify-center text-[var(--r-text)]/70 hover:bg-[#E81123] hover:text-white">
            <span className="text-[12px]">✕</span>
          </button>
        </div>
      </div>

      {/* ---------------- Toolbar ---------------- */}
      <div
        className={`relative z-30 shrink-0 border-b border-[var(--r-border)] bg-[var(--r-chrome)] backdrop-blur-xl transition-all duration-300 ${
          chromeShown ? "" : "pointer-events-none -mt-12 opacity-0"
        }`}
      >
        {compact ? (
          <div className="flex h-12 items-center gap-1 px-2">
            <div className="flex min-w-0 flex-1 items-center gap-2 pl-1">
              <Library className="h-4 w-4 shrink-0 text-[var(--r-muted)]" />
              <div className="min-w-0">
                <div className="truncate text-[12px] font-semibold leading-tight">Cyberpunk Chronicles #01</div>
                <div className="truncate text-[10px] leading-tight text-[var(--r-muted)]">
                  p.{pageLabel} · {PAGE_TITLES[page]}
                </div>
              </div>
            </div>
            <button type="button" aria-label="Bookmark page" onClick={toggleBookmark} className={iconBtn(false)}>
              {isMarked ? <BookmarkCheck className="h-4 w-4 text-amber" /> : <Bookmark className="h-4 w-4" />}
            </button>
            <button type="button" aria-label="Search dialogue" onClick={() => setPanel(panel === "search" ? null : "search")} className={iconBtn(panel === "search")}>
              <ScanText className="h-4 w-4" />
            </button>
            <button type="button" aria-label="Color presets" onClick={() => setPanel(panel === "color" ? null : "color")} className={iconBtn(panel === "color" || color.preset !== "original")}>
              <Palette className="h-4 w-4" />
            </button>
            <button type="button" aria-label="Full screen" onClick={toggleFullscreen} className={iconBtn(fullscreen)}>
              {fullscreen ? <Minimize2 className="h-4 w-4" /> : <Maximize2 className="h-4 w-4" />}
            </button>
            <button type="button" aria-label="More reading options" onClick={() => setPanel(panel === "menu" ? null : "menu")} className={iconBtn(panel === "menu" || panel === "bookmarks")}>
              <Ellipsis className="h-4 w-4" />
            </button>
          </div>
        ) : (
          <div data-lenis-prevent className="no-scrollbar flex h-12 items-center gap-0.5 overflow-x-auto px-2">
            <span className={`${iconBtn(false)} pointer-events-none max-w-[190px] shrink truncate`}>
              <Library className="h-4 w-4 shrink-0 text-[var(--r-muted)]" />
              <span className="truncate font-semibold">Cyberpunk Chronicles #01</span>
            </span>
            {sep}
            <button type="button" title="Fit to Height (H)" onClick={() => setFit("height")} className={iconBtn(fit === "height")}>
              <MoveVertical className="h-4 w-4" />
              <span className="hidden xl:inline">Height</span>
            </button>
            <button type="button" title="Fit to Width (W)" onClick={() => setFit("width")} className={iconBtn(fit === "width")}>
              <MoveHorizontal className="h-4 w-4" />
              <span className="hidden xl:inline">Width</span>
            </button>
            <button type="button" title="Actual Size 1:1 (A)" onClick={() => setFit("actual")} className={iconBtn(fit === "actual")}>
              <Scan className="h-4 w-4" />
              <span className="hidden xl:inline">1:1</span>
            </button>
            {sep}
            <button type="button" title="Two-Page Spread (D)" onClick={toggleSpread} className={iconBtn(isSpread)}>
              <BookOpen className="h-4 w-4" />
              <span className="hidden lg:inline">Spread</span>
            </button>
            <button type="button" title="Webtoon Continuous Scroll (V)" onClick={toggleWebtoon} className={iconBtn(view === "webtoon")}>
              <ScrollText className="h-4 w-4" />
              <span className="hidden lg:inline">Webtoon</span>
            </button>
            <button type="button" title="Reading Direction: Western / Manga RTL (Ctrl+R)" onClick={toggleDir} className={iconBtn(dir === "rtl")}>
              <ArrowLeftRight className="h-4 w-4" />
              <span>{dir === "rtl" ? "RTL" : "LTR"}</span>
            </button>
            {sep}
            <button type="button" title="Offline OCR & Dialogue Search (Ctrl+F)" onClick={() => setPanel(panel === "search" ? null : "search")} className={iconBtn(panel === "search" || ocrOpen)}>
              <ScanText className="h-4 w-4" />
            </button>
            <button type="button" title="Bookmark Page (Ctrl+D)" onClick={toggleBookmark} className={iconBtn(false)}>
              {isMarked ? <BookmarkCheck className="h-4 w-4 text-[#F59E0B]" /> : <Bookmark className="h-4 w-4 text-[#F59E0B]" />}
            </button>
            <button type="button" title="View Bookmarks" onClick={() => setPanel(panel === "bookmarks" ? null : "bookmarks")} className={iconBtn(panel === "bookmarks")}>
              <span className="font-mono text-[11px]">{Object.keys(bookmarks).length}</span>
              <ChevronRight className={`h-3 w-3 transition-transform ${panel === "bookmarks" ? "rotate-90" : ""}`} />
            </button>
            <button type="button" title="Color Correction & Night Mode" onClick={() => setPanel(panel === "color" ? null : "color")} className={iconBtn(panel === "color" || color.preset !== "original")}>
              <Palette className="h-4 w-4" />
              <span className="hidden lg:inline">{color.preset === "original" ? "Color" : color.preset[0].toUpperCase() + color.preset.slice(1)}</span>
            </button>
            {sep}
            <button type="button" title="Zoom Out (Ctrl -)" onClick={() => setZoom(zoom - 0.1)} className={iconBtn(false)}>
              <ZoomOut className="h-4 w-4" />
            </button>
            <button type="button" title="Reset Zoom (Ctrl 0)" onClick={() => setZoom(1)} className={`${iconBtn(false)} w-12 font-mono text-[11px]`}>
              {Math.round(zoom * 100)}%
            </button>
            <button type="button" title="Zoom In (Ctrl +)" onClick={() => setZoom(zoom + 0.1)} className={iconBtn(false)}>
              <ZoomIn className="h-4 w-4" />
            </button>
            {sep}
            <button type="button" title="Toggle Dark / Light Theme" onClick={() => setTheme(theme === "dark" ? "light" : "dark")} className={iconBtn(false)}>
              {theme === "dark" ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
            </button>
            <button type="button" title="Toggle Fullscreen (F11 / F)" onClick={toggleFullscreen} className={iconBtn(fullscreen)}>
              {fullscreen ? <Minimize2 className="h-4 w-4" /> : <Maximize2 className="h-4 w-4" />}
            </button>
          </div>
        )}
      </div>

      {/* ---------------- Canvas ---------------- */}
      <div
        className={`relative min-h-0 ${fullscreen ? "flex-1" : compact ? "h-[min(72vh,560px)]" : "h-[clamp(420px,66vh,680px)]"}`}
        style={{ background: canvasBg, transition: "background 0.4s ease" }}
      >
        <div className="bg-halftone pointer-events-none absolute inset-0 opacity-[0.07]" />

        <div
          ref={canvasRef}
          data-lenis-prevent
          onScroll={onCanvasScroll}
          onPointerDown={onPointerDown}
          onPointerMove={onPointerMove}
          onPointerUp={onPointerUp}
          onPointerCancel={onPointerCancel}
          onDoubleClick={onDoubleClick}
          onWheel={onWheel}
          className={`reader-scroll absolute inset-0 overflow-auto ${fit === "actual" || zoom > 1 ? "cursor-grab active:cursor-grabbing" : ""}`}
          style={{ touchAction: hOverflow ? "pan-x pan-y" : "pan-y", perspective: 1600 }}
        >
          {view === "paged" ? (
            <div className="flex min-h-full min-w-full" style={{ padding: pad }}>
                <motion.div
                  key={`${spreadKey}-${dir}-${isSpread}`}
                  initial={pageAnim.initial}
                  animate={pageAnim.animate}
                  transition={{ duration: 0.28, ease: [0.2, 0, 0, 1] }}
                  className="m-auto flex"
                  style={{ gap: GAP, filter, transition: "filter 0.35s ease" }}
                >
                  {slots.map((n, i) =>
                    n === null ? (
                      <div key={`blank-${i}`} className="shrink-0" style={{ width: pw, height: ph }} aria-hidden />
                    ) : (
                      <div key={n} className="relative shrink-0 bg-white shadow-[0_8px_30px_rgba(0,0,0,0.45)]" style={{ width: pw, height: ph }}>
                        <ComicPage n={n} />
                        {n in bookmarks && <Bookmark className="absolute right-2 top-0 h-7 w-7 fill-[#F59E0B] text-black" />}
                      </div>
                    ),
                  )}
                </motion.div>
            </div>
          ) : (
            <div className="flex min-w-full flex-col items-center" style={{ padding: pad, gap: GAP, filter, transition: "filter 0.35s ease" }}>
              {Array.from({ length: TOTAL_PAGES }, (_, i) => i + 1).map((n) => (
                <div key={n} className="relative shrink-0 bg-white shadow-[0_8px_30px_rgba(0,0,0,0.45)]" style={{ width: pw, height: ph }}>
                  <ComicPage n={n} />
                  {n in bookmarks && <Bookmark className="absolute right-2 top-0 h-7 w-7 fill-[#F59E0B] text-black" />}
                </div>
              ))}
            </div>
          )}
        </div>

        {/* edge turn buttons */}
        {view === "paged" && !compact && (
          <>
            <button
              type="button"
              aria-label="Turn page left"
              title="Turn Page Left (Left Arrow / click left 25%)"
              onClick={turnLeft}
              className={`absolute left-2 top-1/2 z-20 flex h-20 w-9 -translate-y-1/2 items-center justify-center rounded-full border border-[var(--r-border)] bg-[var(--r-chrome)] text-[var(--r-text)] opacity-0 backdrop-blur transition-all hover:bg-amber hover:text-black group-hover/reader:opacity-100 ${chromeShown ? "" : "!opacity-0"}`}
            >
              <ChevronLeft className="h-5 w-5" />
            </button>
            <button
              type="button"
              aria-label="Turn page right"
              title="Turn Page Right (Right Arrow / click right 25%)"
              onClick={turnRight}
              className={`absolute right-2 top-1/2 z-20 flex h-20 w-9 -translate-y-1/2 items-center justify-center rounded-full border border-[var(--r-border)] bg-[var(--r-chrome)] text-[var(--r-text)] opacity-0 backdrop-blur transition-all hover:bg-amber hover:text-black group-hover/reader:opacity-100 ${chromeShown ? "" : "!opacity-0"}`}
            >
              <ChevronRight className="h-5 w-5" />
            </button>
          </>
        )}

        {view === "webtoon" && (
          <div className="pointer-events-none absolute left-3 top-3 z-20 rounded-full border border-[var(--r-border)] bg-[var(--r-chrome)] px-2.5 py-1 font-mono text-[10px] text-[var(--r-muted)] backdrop-blur">
            WEBTOON · 6 parallel decoders
          </div>
        )}

        {/* OCR overlay */}
        <AnimatePresence>
          {ocrOpen && (
            <motion.div initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -10 }} className="absolute inset-x-0 top-3 z-30 flex justify-center">
              <OcrOverlay page={page} onClose={() => setOcrOpen(false)} />
            </motion.div>
          )}
        </AnimatePresence>

        {/* Flyouts */}
        <AnimatePresence>
          {panel && compact && (
            <motion.div key="scrim" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="absolute inset-0 z-30 bg-black/55" onClick={() => setPanel(null)} />
          )}
        </AnimatePresence>
        <AnimatePresence>
          {panel && (
              <motion.div
                key={panel}
                data-lenis-prevent
                initial={compact ? { y: "100%" } : { opacity: 0, y: -8, scale: 0.98 }}
                animate={compact ? { y: 0 } : { opacity: 1, y: 0, scale: 1 }}
                exit={compact ? { y: "100%" } : { opacity: 0, y: -8, scale: 0.98 }}
                transition={{ type: "spring", stiffness: 420, damping: 36 }}
                className={
                  compact
                    ? "absolute inset-x-0 bottom-0 z-40 max-h-[82%] overflow-auto rounded-t-2xl border-t border-[var(--r-border)] bg-[var(--r-flyout)] p-4 pb-5 shadow-2xl backdrop-blur-xl"
                    : "absolute right-3 top-3 z-40 max-h-[calc(100%-24px)] w-[320px] overflow-auto rounded-xl border border-[var(--r-border)] bg-[var(--r-flyout)] p-4 shadow-2xl backdrop-blur-xl"
                }
              >
                {compact && <div className="mx-auto mb-3 h-1 w-10 rounded-full bg-[var(--r-border)]" />}
                {panel === "color" && <ColorPanel color={color} setColor={setColor} />}
                {panel === "bookmarks" && (
                  <BookmarksPanel
                    bookmarks={bookmarks}
                    page={page}
                    toggle={toggleBookmark}
                    setNote={(p, note) => setBookmarks((b) => ({ ...b, [p]: note }))}
                    remove={(p) =>
                      setBookmarks((b) => {
                        const c = { ...b };
                        delete c[p];
                        return c;
                      })
                    }
                    goTo={(p) => {
                      goTo(p);
                      if (compact) setPanel(null);
                    }}
                  />
                )}
                {panel === "search" && (
                  <SearchPanel
                    page={page}
                    goTo={(p) => {
                      goTo(p);
                      if (compact) setPanel(null);
                    }}
                    openOcr={() => {
                      setOcrOpen(true);
                      setPanel(null);
                    }}
                  />
                )}
                {panel === "menu" && (
                  <div className="space-y-5">
                    <ViewPanel
                      fit={fit}
                      setFit={setFit}
                      spread={spread}
                      toggleSpread={toggleSpread}
                      view={view}
                      toggleWebtoon={toggleWebtoon}
                      dir={dir}
                      toggleDir={toggleDir}
                      zoom={zoom}
                      setZoom={setZoom}
                      theme={theme}
                      toggleTheme={() => setTheme(theme === "dark" ? "light" : "dark")}
                    />
                    <BookmarksPanel
                      bookmarks={bookmarks}
                      page={page}
                      toggle={toggleBookmark}
                      setNote={(p, note) => setBookmarks((b) => ({ ...b, [p]: note }))}
                      remove={(p) =>
                        setBookmarks((b) => {
                          const c = { ...b };
                          delete c[p];
                          return c;
                        })
                      }
                      goTo={(p) => {
                        goTo(p);
                        setPanel(null);
                      }}
                    />
                  </div>
                )}
              </motion.div>
          )}
        </AnimatePresence>

        {/* Toast */}
        <AnimatePresence>
          {toast && (
            <motion.div
              key={toast.id}
              initial={{ opacity: 0, y: 12, scale: 0.96 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: 12 }}
              className="pointer-events-none absolute inset-x-0 bottom-4 z-50 flex justify-center px-4"
            >
              <div className="flex items-center gap-2 rounded-full border border-[var(--r-border)] bg-[var(--r-flyout)] px-3.5 py-1.5 text-[12px] font-medium shadow-2xl backdrop-blur-xl">
                <span className="h-2 w-2 rounded-full bg-amber" />
                {toast.msg}
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      {/* ---------------- Scrubber ---------------- */}
      <div className={`relative z-30 shrink-0 border-t border-[var(--r-border)] bg-[var(--r-chrome)] backdrop-blur-xl transition-all duration-300 ${chromeShown ? "" : "pointer-events-none -mb-14 opacity-0"}`}>
        <Scrubber
          page={page}
          dir={dir}
          compact={compact}
          label={pageLabel}
          progress={progress}
          onFirst={() => goTo(dir === "ltr" ? 1 : TOTAL_PAGES)}
          onLast={() => goTo(dir === "ltr" ? TOTAL_PAGES : 1)}
          onLeft={turnLeft}
          onRight={turnRight}
          onScrub={(p) => goTo(p, { smooth: false })}
        />
      </div>
    </div>
  );
}

/* ------------------------------ Scrubber ------------------------------ */

function Scrubber({
  page,
  dir,
  compact,
  label,
  progress,
  onFirst,
  onLast,
  onLeft,
  onRight,
  onScrub,
}: {
  page: number;
  dir: Direction;
  compact: boolean;
  label: string;
  progress: number;
  onFirst: () => void;
  onLast: () => void;
  onLeft: () => void;
  onRight: () => void;
  onScrub: (p: number) => void;
}) {
  const trackRef = useRef<HTMLDivElement>(null);
  const [hover, setHover] = useState<{ p: number; x: number } | null>(null);
  const [dragging, setDragging] = useState(false);

  const pageFromX = (clientX: number) => {
    const r = trackRef.current!.getBoundingClientRect();
    let ratio = clamp((clientX - r.left) / r.width, 0, 1);
    if (dir === "rtl") ratio = 1 - ratio;
    return { p: Math.round(1 + ratio * (TOTAL_PAGES - 1)), x: clamp(clientX - r.left, 0, r.width) };
  };

  const thumbRatio = (page - 1) / (TOTAL_PAGES - 1);
  const thumbLeft = `${(dir === "rtl" ? 1 - thumbRatio : thumbRatio) * 100}%`;
  const btn = "flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-[var(--r-text)] hover:bg-[var(--r-hover)]";

  return (
    <div className="flex h-12 items-center gap-1 px-2 sm:gap-2 sm:px-3">
      {!compact && (
        <button type="button" onClick={onFirst} title={dir === "ltr" ? "First Page (Home)" : "Last Page (End)"} className={btn}>
          <ChevronsLeft className="h-4 w-4" />
        </button>
      )}
      <button type="button" onClick={onLeft} title="Turn Left" aria-label="Turn page left" className={btn}>
        <ChevronLeft className="h-4 w-4" />
      </button>

      <div
        ref={trackRef}
        role="slider"
        tabIndex={0}
        aria-label="Page scrubber"
        aria-valuemin={1}
        aria-valuemax={TOTAL_PAGES}
        aria-valuenow={page}
        className="relative h-8 flex-1 cursor-pointer touch-none"
        onPointerDown={(e) => {
          e.currentTarget.setPointerCapture(e.pointerId);
          setDragging(true);
          const h = pageFromX(e.clientX);
          setHover(h);
          onScrub(h.p);
        }}
        onPointerMove={(e) => {
          const h = pageFromX(e.clientX);
          if (e.pointerType === "mouse" || dragging) setHover(h);
          if (dragging && h.p !== page) onScrub(h.p);
        }}
        onPointerUp={() => {
          setDragging(false);
          setHover(null);
        }}
        onPointerLeave={() => !dragging && setHover(null)}
        onKeyDown={(e) => {
          if (e.key === "ArrowUp") onScrub(page + 1);
          if (e.key === "ArrowDown") onScrub(page - 1);
        }}
      >
        <div className="absolute inset-x-0 top-1/2 h-1.5 -translate-y-1/2 rounded-full bg-[var(--r-track)]" />
        <div
          className="absolute top-1/2 h-1.5 -translate-y-1/2 rounded-full bg-amber"
          style={dir === "ltr" ? { left: 0, width: thumbLeft } : { right: 0, width: `${thumbRatio * 100}%` }}
        />
        <div
          className="absolute top-1/2 h-4 w-4 -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-black bg-amber shadow transition-[left] duration-150"
          style={{ left: thumbLeft }}
        />
        <AnimatePresence>
          {hover && (
            <motion.div
              initial={{ opacity: 0, y: 6 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: 6 }}
              transition={{ duration: 0.12 }}
              className="pointer-events-none absolute bottom-9 z-50 -translate-x-1/2"
              style={{ left: clamp(hover.x, 50, (trackRef.current?.clientWidth ?? 200) - 50) }}
            >
              <div className="rounded-lg border border-[var(--r-border)] bg-[var(--r-flyout)] p-1.5 shadow-2xl backdrop-blur-xl">
                <div className="h-[120px] w-[80px] overflow-hidden rounded-sm bg-white">
                  <ComicPage n={hover.p} />
                </div>
                <div className="mt-1 text-center font-mono text-[10px] font-bold text-[var(--r-text)]">
                  {hover.p} / {TOTAL_PAGES}
                </div>
              </div>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      <span className="min-w-[52px] text-center font-mono text-[11px] font-bold text-[var(--r-text)]">
        {label} / {TOTAL_PAGES}
      </span>
      <button type="button" onClick={onRight} title="Turn Right" aria-label="Turn page right" className={btn}>
        <ChevronRight className="h-4 w-4" />
      </button>
      {!compact && (
        <>
          <button type="button" onClick={onLast} title={dir === "ltr" ? "Last Page (End)" : "First Page (Home)"} className={btn}>
            <ChevronsRight className="h-4 w-4" />
          </button>
          <span className="w-10 border-l border-[var(--r-border)] pl-2 font-mono text-[11px] font-black text-amber">{progress}%</span>
        </>
      )}
    </div>
  );
}
