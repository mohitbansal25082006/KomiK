"use client";

import { useEffect } from "react";
import Lenis from "lenis";
import { gsap, ScrollTrigger, prefersReducedMotion } from "@/lib/gsap";

/**
 * Lenis smooth scrolling driven by GSAP's ticker so ScrollTrigger animations
 * stay perfectly in sync. Disabled for reduced-motion users and while the
 * reader mockup is full screen (it dispatches `komik:scroll-lock`).
 */
export default function SmoothScroll() {
  useEffect(() => {
    if (prefersReducedMotion()) return;

    const lenis = new Lenis({
      lerp: 0.11,
      smoothWheel: true,
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
      document.removeEventListener("komik:scroll-lock", onLock);
      gsap.ticker.remove(tick);
      lenis.destroy();
    };
  }, []);

  return null;
}
