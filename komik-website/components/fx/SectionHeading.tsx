"use client";

import { useRef } from "react";
import { gsap, SplitText, useGSAP, prefersReducedMotion } from "@/lib/gsap";

type Tone = "yellow" | "cyan" | "magenta";

const CAPTION: Record<Tone, string> = {
  yellow: "caption-box",
  cyan: "caption-box-cyan",
  magenta: "caption-box-magenta",
};

const ACCENT: Record<Tone, string> = {
  yellow: "text-amber",
  cyan: "text-cyan",
  magenta: "text-magenta",
};

/** Caption box + oversized comic lettering that "pops" in character by character on scroll. */
export default function SectionHeading({
  caption,
  title,
  accent,
  sub,
  tone = "yellow",
  align = "left",
  onPaper = false,
}: {
  caption: string;
  title: string;
  accent?: string;
  sub?: React.ReactNode;
  tone?: Tone;
  align?: "left" | "center";
  onPaper?: boolean;
}) {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(
    () => {
      if (prefersReducedMotion() || !ref.current) return;
      let split: SplitText | null = null;
      let tween: gsap.core.Tween | null = null;
      document.fonts.ready.then(() => {
        if (!ref.current) return;
        split = SplitText.create(ref.current.querySelectorAll("[data-split]"), { type: "words,chars", charsClass: "inline-block" });
        tween = gsap.from(split.chars, {
          yPercent: 120,
          rotate: () => gsap.utils.random(-25, 25),
          opacity: 0,
          duration: 0.7,
          ease: "back.out(2)",
          stagger: 0.022,
          scrollTrigger: { trigger: ref.current, start: "top 82%", once: true },
        });
      });
      gsap.from(ref.current.querySelectorAll("[data-cap]"), {
        scale: 0,
        rotate: -12,
        duration: 0.55,
        ease: "back.out(2.5)",
        scrollTrigger: { trigger: ref.current, start: "top 85%", once: true },
      });
      gsap.from(ref.current.querySelectorAll("[data-sub]"), {
        y: 24,
        opacity: 0,
        duration: 0.7,
        ease: "power3.out",
        delay: 0.25,
        scrollTrigger: { trigger: ref.current, start: "top 82%", once: true },
      });
      return () => {
        tween?.scrollTrigger?.kill();
        split?.revert();
      };
    },
    { scope: ref },
  );

  const centered = align === "center";
  return (
    <div ref={ref} className={`${centered ? "mx-auto text-center" : ""} max-w-4xl`}>
      <div data-cap className={`${CAPTION[tone]} inline-block -rotate-1 px-3 py-1 text-[11px] sm:text-xs`}>
        {caption}
      </div>
      <h2 className="mt-5 font-bangers text-[13vw] leading-[0.92] tracking-wide sm:text-7xl lg:text-8xl">
        <span data-split className={`block ${onPaper ? "text-black" : "text-comic-outline text-newsprint"}`}>
          {title}
        </span>
        {accent && (
          <span data-split className={`text-comic-outline block ${ACCENT[tone]}`}>
            {accent}
          </span>
        )}
      </h2>
      {sub && (
        <p data-sub className={`mt-5 max-w-2xl text-base font-medium leading-relaxed sm:text-lg ${centered ? "mx-auto" : ""} ${onPaper ? "text-black/80" : "text-newsprint/80"}`}>
          {sub}
        </p>
      )}
    </div>
  );
}
