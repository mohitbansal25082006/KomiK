"use client";

import { useEffect } from "react";
import Lenis from "lenis";
import { gsap, ScrollTrigger, prefersReducedMotion } from "@/lib/gsap";

const NAV_OFFSET = 72;

const easeInOutCubic = (t: number) => (t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2);

/** Longer trips take a little longer, but every jump feels like one calm, eased glide. */
const durationFor = (distance: number) => Math.min(1.6, Math.max(0.7, 0.55 + Math.abs(distance) / 6000));

/**
 * Smooth scrolling for the whole site.
 * - Desktop: Lenis wheel smoothing driven by GSAP's ticker (keeps ScrollTrigger in sync).
 * - Touch: native momentum scrolling.
 * - Every in-page link (navbar, mobile menu, CTAs) goes through one eased, time-based
 *   scroll instead of the browser's default jump, and waits for menus to close first.
 * - Offscreen CSS loop animations are paused to keep the compositor free.
 */
export default function SmoothScroll() {
  useEffect(() => {
    const reduced = prefersReducedMotion();

    /* ---- pause offscreen CSS animations ---- */
    const io = new IntersectionObserver(
      (entries) => {
        for (const e of entries) e.target.classList.toggle("anim-paused", !e.isIntersecting);
      },
      { rootMargin: "200px 0px" },
    );
    document.querySelectorAll("section, footer").forEach((el) => io.observe(el));

    /* ---- Lenis (desktop only) ---- */
    const touchOnly = window.matchMedia("(hover: none) and (pointer: coarse)").matches;
    let lenis: Lenis | null = null;
    let tick: ((time: number) => void) | null = null;

    if (!reduced && !touchOnly) {
      lenis = new Lenis({
        lerp: 0.1,
        wheelMultiplier: 1,
        smoothWheel: true,
        syncTouch: false,
      });
      lenis.on("scroll", ScrollTrigger.update);
      tick = (time: number) => lenis!.raf(time * 1000);
      gsap.ticker.add(tick);
      gsap.ticker.lagSmoothing(0);
    }

    /* ---- flag the page while it scrolls ---- */
    // CSS uses html.is-scrolling to hold the giant rotating backgrounds still while content moves over
    // them (see globals.css). Window scroll events fire for Lenis and native touch scrolling alike.
    const root = document.documentElement;
    let scrollIdle = 0;
    const onScrollActivity = () => {
      if (!root.classList.contains("is-scrolling")) root.classList.add("is-scrolling");
      window.clearTimeout(scrollIdle);
      scrollIdle = window.setTimeout(() => root.classList.remove("is-scrolling"), 160);
    };
    window.addEventListener("scroll", onScrollActivity, { passive: true });

    /* ---- keep pinned sections in step with the page ---- */
    // Content above a pinned section can change height after load (the reader measuring itself, demos
    // opening and closing). ScrollTrigger's start and end points would then be stale, so a pinned strip
    // starts or lets go too early. Re-measure whenever the page height really changes.
    let lastHeight = document.documentElement.scrollHeight;
    let refreshTimer = 0;
    const onHeightChange = () => {
      window.clearTimeout(refreshTimer);
      refreshTimer = window.setTimeout(() => {
        const height = document.documentElement.scrollHeight;
        if (Math.abs(height - lastHeight) < 2) return;
        ScrollTrigger.refresh();
        // A refresh can resize pin spacers itself; remember the settled height so it never loops.
        lastHeight = document.documentElement.scrollHeight;
      }, 250);
    };
    const heightObserver = new ResizeObserver(onHeightChange);
    heightObserver.observe(document.body);
    document.fonts?.ready.then(() => ScrollTrigger.refresh());

    const onLock = (e: Event) => {
      if (!lenis) return;
      if ((e as CustomEvent<boolean>).detail) lenis.stop();
      else lenis.start();
    };
    document.addEventListener("komik:scroll-lock", onLock);

    /* ---- eased scroll for touch devices (no Lenis) ---- */
    let raf = 0;
    const cancelTween = () => {
      if (raf) cancelAnimationFrame(raf);
      raf = 0;
    };
    const tweenWindowTo = (top: number, onDone?: () => void) => {
      cancelTween();
      const start = window.scrollY;
      const dist = top - start;
      const dur = durationFor(dist) * 1000;
      const t0 = performance.now();
      const step = (now: number) => {
        const p = Math.min(1, (now - t0) / dur);
        window.scrollTo(0, start + dist * easeInOutCubic(p));
        if (p < 1) raf = requestAnimationFrame(step);
        else {
          raf = 0;
          onDone?.();
        }
      };
      raf = requestAnimationFrame(step);
    };
    // a finger or wheel on the page always wins over an in-flight jump
    window.addEventListener("touchstart", cancelTween, { passive: true });
    window.addEventListener("wheel", cancelTween, { passive: true });

    const targetTop = (hash: string) => {
      if (hash === "#top" || hash === "#") return 0;
      const target = document.querySelector<HTMLElement>(hash);
      if (!target) return null;
      const extra = hash === "#reader" ? 44 : 0; // leave room for the reader's speech balloon
      return Math.max(0, target.getBoundingClientRect().top + window.scrollY - NAV_OFFSET - extra);
    };

    const scrollToHash = (hash: string, settle = true) => {
      const top = targetTop(hash);
      if (top === null) return;
      if (reduced) {
        window.scrollTo(0, top);
        return;
      }
      // content that animates in while we travel can nudge the target: glide the last few px
      const correct = () => {
        if (!settle) return;
        const again = targetTop(hash);
        if (again !== null && Math.abs(again - window.scrollY) > 2) scrollToHash(hash, false);
      };
      if (lenis) {
        lenis.start();
        lenis.scrollTo(top, { duration: settle ? durationFor(top - window.scrollY) : 0.4, easing: easeInOutCubic, force: true, onComplete: correct });
      } else {
        tweenWindowTo(top, correct);
      }
    };

    const onClick = (e: MouseEvent) => {
      if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
      const link = (e.target as HTMLElement | null)?.closest<HTMLAnchorElement>('a[href^="#"]');
      if (!link) return;
      const hash = link.getAttribute("href") ?? "#";
      if (hash !== "#top" && hash !== "#" && !document.querySelector(hash)) return;
      e.preventDefault();
      history.replaceState(null, "", hash === "#" ? window.location.pathname : hash);
      // give the mobile menu / scroll locks a moment to release before measuring
      window.setTimeout(() => scrollToHash(hash), 90);
    };
    document.addEventListener("click", onClick);

    return () => {
      io.disconnect();
      heightObserver.disconnect();
      window.clearTimeout(refreshTimer);
      window.removeEventListener("scroll", onScrollActivity);
      window.clearTimeout(scrollIdle);
      root.classList.remove("is-scrolling");
      cancelTween();
      document.removeEventListener("click", onClick);
      document.removeEventListener("komik:scroll-lock", onLock);
      window.removeEventListener("touchstart", cancelTween);
      window.removeEventListener("wheel", cancelTween);
      if (tick) gsap.ticker.remove(tick);
      lenis?.destroy();
    };
  }, []);

  return null;
}
