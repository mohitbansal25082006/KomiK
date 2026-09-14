"use client";

import { useEffect, useState } from "react";
import { AnimatePresence, motion, useScroll, useSpring } from "framer-motion";
import { Download, Github, Menu, X } from "lucide-react";
import { APP_CONFIG } from "@/lib/config";
import KomikLogo from "@/components/KomikLogo";

const LINKS = [
  { href: "#reader", label: "Demo", id: "reader" },
  { href: "#formats", label: "Formats", id: "formats" },
  { href: "#reading-engine", label: "Engine", id: "reading-engine" },
  { href: "#whats-new", label: "New in 1.1", id: "whats-new" },
  { href: "#why-offline", label: "Manifesto", id: "why-offline" },
  { href: "#shortcuts", label: "Shortcuts", id: "shortcuts" },
];

const TILE = ["bg-amber text-black", "bg-cyan text-black", "bg-magenta text-white", "bg-paper text-black", "bg-amber text-black", "bg-cyan text-black"];

export default function Navbar() {
  const { scrollYProgress } = useScroll();
  const progress = useSpring(scrollYProgress, { stiffness: 200, damping: 30, mass: 0.3 });
  const [active, setActive] = useState<string | null>(null);
  const [open, setOpen] = useState(false);
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 24);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    const io = new IntersectionObserver(
      (entries) => {
        for (const e of entries) if (e.isIntersecting) setActive(e.target.id);
      },
      { rootMargin: "-45% 0px -50% 0px" },
    );
    LINKS.forEach((l) => {
      const el = document.getElementById(l.id);
      if (el) io.observe(el);
    });
    return () => {
      window.removeEventListener("scroll", onScroll);
      io.disconnect();
    };
  }, []);

  useEffect(() => {
    document.body.style.overflow = open ? "hidden" : "";
    document.dispatchEvent(new CustomEvent("komik:scroll-lock", { detail: open }));
  }, [open]);

  return (
    <header className={`sticky top-0 z-50 w-full border-b-[3px] border-black transition-colors duration-300 ${scrolled ? "bg-ink/90 backdrop-blur-md" : "bg-ink"}`}>
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
        <a href="#top" className="group flex shrink-0 items-center gap-3" aria-label="Komik home">
          <motion.div
            whileHover={{ rotate: [0, -12, 10, -6, 0], scale: 1.08 }}
            transition={{ duration: 0.5 }}
            className="relative flex h-10 w-10 items-center justify-center border-2 border-black bg-[#121316] shadow-[3px_3px_0_#FFD700]"
          >
            <KomikLogo size={28} />
          </motion.div>
          <div className="flex flex-col">
            <span className="font-bangers text-2xl leading-none tracking-wider text-newsprint transition-colors group-hover:text-amber">KOMIK</span>
            <span className="-mt-0.5 font-mono text-[9px] font-bold uppercase tracking-[0.2em] text-muted">v{APP_CONFIG.version} · Windows</span>
          </div>
        </a>

        <nav className="hidden items-center gap-1 lg:flex">
          {LINKS.map((l) => (
            <a key={l.id} href={l.href} className="relative px-3 py-1.5 text-sm font-bold text-newsprint/85 transition-colors hover:text-white">
              {active === l.id && (
                <motion.span
                  layoutId="nav-pill"
                  className="absolute inset-0 -rotate-1 border-2 border-black bg-amber shadow-[2px_2px_0_#000]"
                  transition={{ type: "spring", stiffness: 500, damping: 35 }}
                />
              )}
              <span className={`relative ${active === l.id ? "text-black" : ""}`}>{l.label}</span>
            </a>
          ))}
        </nav>

        <div className="flex items-center gap-2 sm:gap-3">
          <a
            href={APP_CONFIG.repoUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="hidden h-10 w-10 items-center justify-center border-2 border-black bg-[#242630] text-white shadow-[2px_2px_0_#000] transition-colors hover:bg-cyan hover:text-black sm:inline-flex"
            aria-label="GitHub repository"
          >
            <Github className="h-4 w-4" />
          </a>
          <a href={APP_CONFIG.downloadUrl} className="btn-comic-primary inline-flex items-center gap-2 px-3 py-2 text-xs uppercase tracking-wider sm:px-4">
            <Download className="h-4 w-4 stroke-[2.5]" />
            <span className="hidden sm:inline">Get Komik</span>
            <span className="sm:hidden">Get</span>
          </a>
          <button
            type="button"
            onClick={() => setOpen((o) => !o)}
            className="inline-flex h-10 w-10 items-center justify-center border-2 border-black bg-paper text-black shadow-[2px_2px_0_#000] lg:hidden"
            aria-label={open ? "Close menu" : "Open menu"}
            aria-expanded={open}
          >
            {open ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </button>
        </div>
      </div>

      {/* CMYK ink progress bar */}
      <motion.div
        aria-hidden
        style={{ scaleX: progress, transformOrigin: "0% 50%" }}
        className="absolute inset-x-0 -bottom-[3px] h-[3px] bg-[linear-gradient(90deg,#FFD700_0_33%,#00C2FF_33%_66%,#FF1F6D_66%)]"
      />

      <AnimatePresence>
        {open && (
          <motion.nav
            initial={{ clipPath: "polygon(0 0, 100% 0, 100% 0, 0 0)" }}
            animate={{ clipPath: "polygon(0 0, 100% 0, 100% 100%, 0 100%)" }}
            exit={{ clipPath: "polygon(0 0, 100% 0, 100% 0, 0 0)" }}
            transition={{ duration: 0.35, ease: [0.7, 0, 0.3, 1] }}
            data-lenis-prevent
            className="fixed inset-x-0 top-[67px] z-40 h-[calc(100dvh-67px)] overflow-auto border-t-[3px] border-black bg-ink p-4 lg:hidden"
          >
            <div className="bg-halftone pointer-events-none absolute inset-0 opacity-30" />
            <div className="relative grid grid-cols-2 gap-3">
              {LINKS.map((l, i) => (
                <motion.a
                  key={l.id}
                  href={l.href}
                  onClick={() => setOpen(false)}
                  initial={{ opacity: 0, y: 30, rotate: i % 2 ? 3 : -3 }}
                  animate={{ opacity: 1, y: 0, rotate: i % 2 ? 1 : -1 }}
                  transition={{ delay: 0.08 + i * 0.05, type: "spring", stiffness: 400, damping: 22 }}
                  className={`${TILE[i]} flex aspect-[4/3] items-end border-[3px] border-black p-3 font-bangers text-3xl leading-none shadow-[5px_5px_0_#000]`}
                >
                  {l.label}
                </motion.a>
              ))}
            </div>
            <motion.a
              href={APP_CONFIG.downloadUrl}
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ delay: 0.4 }}
              className="btn-comic-primary relative mt-5 flex items-center justify-center gap-2 py-4 text-base uppercase"
            >
              <Download className="h-5 w-5" /> Download for Windows
            </motion.a>
          </motion.nav>
        )}
      </AnimatePresence>
    </header>
  );
}
