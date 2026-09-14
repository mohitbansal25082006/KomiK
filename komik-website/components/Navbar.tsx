"use client";

import Link from "next/link";
import { APP_CONFIG } from "@/lib/config";
import { Download, Github, Monitor, BookOpen } from "lucide-react";
import KomikLogo from "@/components/KomikLogo";

export default function Navbar() {
  return (
    <header className="sticky top-0 z-50 w-full border-b-[3px] border-black bg-ink/95 backdrop-blur-md">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
        {/* Brand */}
        <Link href="/" className="group flex items-center gap-3">
          <div className="relative flex h-10 w-10 items-center justify-center border-2 border-black bg-[#121316] shadow-[3px_3px_0px_#FFD700] transition-transform group-hover:-translate-x-0.5 group-hover:-translate-y-0.5">
            <KomikLogo size={28} />
          </div>
          <div className="flex flex-col">
            <span className="font-display text-xl font-bold tracking-tight text-newsprint group-hover:text-amber transition-colors">
              {APP_CONFIG.name}
            </span>
            <span className="text-[10px] font-mono uppercase tracking-widest text-muted -mt-1 font-semibold">
              Windows Native
            </span>
          </div>
        </Link>

        {/* Desktop Navigation Links with Clear High Contrast */}
        <nav className="hidden md:flex items-center gap-7 text-sm font-bold text-newsprint/90">
          <a
            href="#formats"
            className="hover:text-amber transition-colors hover:underline underline-offset-4 decoration-amber"
          >
            Formats
          </a>
          <a
            href="#reading-engine"
            className="hover:text-amber transition-colors hover:underline underline-offset-4 decoration-amber"
          >
            Reading Engine
          </a>
          <a
            href="#why-offline"
            className="hover:text-amber transition-colors hover:underline underline-offset-4 decoration-amber"
          >
            The Manifesto
          </a>
          <a
            href="#shortcuts"
            className="hover:text-amber transition-colors hover:underline underline-offset-4 decoration-amber"
          >
            Shortcuts
          </a>
          <a
            href="#download"
            className="hover:text-amber transition-colors hover:underline underline-offset-4 decoration-amber"
          >
            Download
          </a>
        </nav>

        {/* Actions */}
        <div className="flex items-center gap-3">
          <a
            href={APP_CONFIG.repoUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex h-9 w-9 items-center justify-center border-2 border-black bg-[#242630] text-white hover:bg-amber hover:text-black hover:border-black transition-colors shadow-[2px_2px_0px_#000]"
            aria-label="GitHub Repository"
            title="View Source on GitHub"
          >
            <Github className="h-4 w-4" />
          </a>

          <a
            href={APP_CONFIG.downloadUrl}
            className="btn-comic-primary inline-flex items-center gap-2 px-4 py-2 text-xs font-black tracking-wider uppercase"
          >
            <Download className="h-3.5 w-3.5 stroke-[2.5]" />
            <span className="hidden sm:inline">Get Komik (.exe)</span>
            <span className="sm:hidden">Get App</span>
          </a>
        </div>
      </div>
    </header>
  );
}
