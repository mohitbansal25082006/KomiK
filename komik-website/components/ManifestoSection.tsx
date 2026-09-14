"use client";

import { useRef } from "react";
import { HardDrive, Lock, UserX, WifiOff } from "lucide-react";
import { gsap, useGSAP, prefersReducedMotion } from "@/lib/gsap";

const PILLARS = [
  { icon: WifiOff, title: "100% offline", desc: "Zero external network calls. Komik works the same on a plane, in a tunnel, or completely off the grid.", stamp: "NO CLOUD" },
  { icon: UserX, title: "No accounts, ever", desc: "No email, no sign-in screen, no tokens, no passwords. Install it and start reading.", stamp: "NO LOGIN" },
  { icon: Lock, title: "Zero telemetry", desc: "No tracking SDKs, no crash beacons, no session recordings. What you read is your business.", stamp: "NO SPYING" },
  { icon: HardDrive, title: "Non-destructive SQLite", desc: "Progress, tags and bookmarks live in a local SQLite database. Removing a comic from the library never deletes the file.", stamp: "NO DELETES" },
];

export default function ManifestoSection() {
  const ref = useRef<HTMLElement>(null);

  useGSAP(
    () => {
      if (prefersReducedMotion()) return;
      gsap.utils.toArray<HTMLElement>("[data-stamp]").forEach((el, i) => {
        gsap.fromTo(
          el,
          { scale: 3.2, opacity: 0, rotate: -40 },
          {
            scale: 1,
            opacity: 1,
            rotate: i % 2 ? 9 : -11,
            duration: 0.45,
            ease: "back.out(3)",
            scrollTrigger: { trigger: el.parentElement, start: "top 75%", once: true },
            onComplete: () => {
              gsap.fromTo(el.parentElement, { x: -6 }, { x: 0, duration: 0.35, ease: "elastic.out(2, 0.3)" });
            },
          },
        );
      });
      gsap.fromTo("[data-underline]", { strokeDashoffset: 900 }, { strokeDashoffset: 0, duration: 1.2, ease: "power2.inOut", scrollTrigger: { trigger: ref.current, start: "top 60%", once: true } });
      gsap.from("[data-headline] > span", {
        y: 80,
        opacity: 0,
        rotate: -4,
        stagger: 0.15,
        duration: 0.8,
        ease: "power4.out",
        scrollTrigger: { trigger: ref.current, start: "top 70%", once: true },
      });
    },
    { scope: ref },
  );

  return (
    <section ref={ref} id="why-offline" className="relative scroll-mt-16 border-b-[3px] border-black bg-ink py-16 sm:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="relative">
          {/* torn paper slab */}
          <div className="relative bg-paper-grain px-5 pb-12 pt-14 text-black shadow-[10px_10px_0_#000] sm:px-10 lg:px-16 [clip-path:polygon(0_2%,4%_0,9%_1.5%,15%_0,22%_1.8%,30%_0.2%,38%_1.6%,47%_0,55%_1.4%,63%_0.1%,72%_1.7%,80%_0.3%,88%_1.5%,95%_0,100%_1.2%,100%_98%,96%_100%,90%_98.4%,83%_100%,76%_98.2%,68%_99.8%,60%_98.3%,52%_100%,44%_98.6%,36%_99.9%,28%_98.2%,20%_100%,12%_98.5%,5%_100%,0_98.4%)]">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b-[3px] border-black pb-3 font-mono text-[11px] font-black uppercase tracking-wider">
              <span>The Daily Panel · Editorial</span>
              <span className="hidden sm:inline">Vol. 1 · No. 1.1.0</span>
              <span className="caption-box-magenta px-2 py-0.5 text-[10px]">From the drafting table</span>
            </div>

            <div className="mt-8 grid gap-10 lg:grid-cols-12">
              <div className="lg:col-span-7">
                <h2 data-headline className="font-bangers text-[14vw] leading-[0.88] tracking-wide sm:text-8xl">
                  <span className="block">Your comics</span>
                  <span className="block">belong on</span>
                  <span className="relative inline-block">
                    your PC.
                    <svg className="absolute -bottom-4 left-0 h-6 w-full overflow-visible" viewBox="0 0 400 24" preserveAspectRatio="none" aria-hidden>
                      <path data-underline d="M4 16 C 80 4, 160 22, 240 10 S 380 8, 396 14" fill="none" stroke="#FF1F6D" strokeWidth="7" strokeLinecap="round" strokeDasharray="900" />
                    </svg>
                  </span>
                </h2>
                <p className="mt-10 max-w-xl font-serif text-lg leading-relaxed text-black/85 first-letter:float-left first-letter:mr-2 first-letter:font-bangers first-letter:text-7xl first-letter:leading-[0.8]">
                  Most modern comic apps want you to create an account, upload your personal archive to their servers, accept behavioral telemetry, or pay a monthly subscription
                  just to turn a digital page.
                </p>
                <p className="mt-4 max-w-xl text-lg font-black">Komik was deliberately built as the opposite of that model.</p>
                <p className="mt-6 font-comic text-xl font-bold -rotate-1 text-[#1a3a8a]">— signed, a reader who just wanted to read. ✎</p>
              </div>

              <div className="grid content-start gap-5 sm:grid-cols-2 lg:col-span-5 lg:grid-cols-1">
                {PILLARS.map((p) => (
                  <div key={p.title} className="relative border-[3px] border-black bg-white p-4 pb-6 shadow-[4px_4px_0_#000]">
                    <div className="flex items-center gap-2 font-bangers text-2xl leading-none">
                      <span className="flex h-8 w-8 shrink-0 items-center justify-center border-2 border-black bg-black text-amber">
                        <p.icon className="h-4 w-4" />
                      </span>
                      {p.title}
                    </div>
                    <p className="mt-2 text-[13px] font-medium leading-snug text-black/80">{p.desc}</p>
                    <span
                      data-stamp
                      className="pointer-events-none absolute -bottom-3 -right-2 border-[3px] border-magenta px-2 py-0.5 font-bangers text-xl tracking-wider text-magenta opacity-90 mix-blend-multiply"
                      style={{ transform: "rotate(-11deg)" }}
                    >
                      {p.stamp}
                    </span>
                  </div>
                ))}
              </div>
            </div>

            <div className="mt-10 flex flex-wrap items-center justify-between gap-4 border-t-[3px] border-black pt-4 font-mono text-xs font-black uppercase">
              <span>Independent · MIT licensed · No venture capital</span>
              <span className="bg-black px-3 py-1.5 text-amber">100% free &amp; open source</span>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
