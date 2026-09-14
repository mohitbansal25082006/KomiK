"use client";

import { useEffect } from "react";
import Lenis from "lenis";
import { gsap, ScrollTrigger, prefersReducedMotion } from "@/lib/gsap";

/**
 * Buttery wheel scrolling on desktop via Lenis, driven by GSAP's ticker so
 * ScrollTrigger stays perfectly in sync. Touch devices keep native momentum
 * scrolling (it is already the smoothest option there). Also pauses CSS loop
 * animations that are offscreen so the compositor stays free while scrolling.
 */
export default function SmoothScroll() {
  useEffect(() => {
    /* ---- pause offscreen CSS animations (all devices) ---- */
    const io = new IntersectionObserver(
      (entries) => {
        for (const e of entries) e.target.classList.toggle("anim-paused", !e.isIntersecting);
      },
      { rootMargin: "200px 0px" },
    );
    document.querySelectorAll("section, footer").forEach((el) => io.observe(el));

    const touchOnly = window.matchMedia("(hover: none) and (pointer: coarse)").matches;
    if (prefersReducedMotion() || touchOnly) {
      return () => io.disconnect();
    }

    const lenis = new Lenis({
      lerp: 0.075,
      wheelMultiplier: 0.95,
      smoothWheel: true,
      syncTouch: false,
      anchors: { offset: -72 },
    });

    lenis.on("scroll", ScrollTrigger.update);
    const tick = (time: number) => lenis.raf(time * 1000);
    gsap.ticker.add(tick);
    gsap.ticker.lagSmoothing(0);

    const onLock = (e: Event) => {
      const locked = (e as CustomEvent<boolean>).detail;
      if (locked) lenis.stop();
      else lenis.start();
    };
    document.addEventListener("komik:scroll-lock", onLock);

    return () => {
      io.disconnect();
      document.removeEventListener("komik:scroll-lock", onLock);
      gsap.ticker.remove(tick);
      lenis.destroy();
    };
  }, []);

  return null;
}
