import React from "react";

interface ComicPageProps {
  className?: string;
  isNightMode?: boolean;
}

// ==========================================
// PAGE 1: ORIGINAL ILLUSTRATED COMIC COVER
// ==========================================
export function ComicCoverPage({ className = "", isNightMode = false }: ComicPageProps) {
  return (
    <div
      className={`relative w-full aspect-[2/3] max-w-[280px] sm:max-w-[310px] mx-auto border-[2.5px] border-black bg-[#F5EFEB] text-black shadow-[4px_4px_0px_#000000] overflow-hidden select-none transition-all duration-300 ${className}`}
    >
      {/* Cover Header Banner */}
      <div className="border-b-2 border-black bg-amber p-2 flex items-center justify-between">
        <div className="flex items-center gap-1.5">
          <span className="font-mono text-[9px] font-black bg-black text-white px-1 py-0.5">
            #01
          </span>
          <span className="font-mono text-[9px] font-bold text-black/80 uppercase">
            COLLECTOR&apos;S EDITION
          </span>
        </div>
        <div className="font-mono text-[9px] font-black text-black">
          $3.99 US / 75¢
        </div>
      </div>

      {/* Main Cover Illustration (SVG Vector Art) */}
      <div className="relative p-2.5 flex flex-col justify-between h-[calc(100%-36px)]">
        {/* Masthead Logo */}
        <div className="text-center pt-1">
          <div className="font-display text-2xl sm:text-3xl font-black tracking-tight uppercase text-black leading-none drop-shadow-[2px_2px_0px_rgba(255,215,0,1)]">
            CYBERPUNK
          </div>
          <div className="font-display text-xs sm:text-sm font-black tracking-[0.25em] uppercase text-crimson -mt-0.5">
            CHRONICLES
          </div>
        </div>

        {/* Central Vector Art: Cityscape & Vigilante Silhouette */}
        <div className="my-auto relative h-40 sm:h-44 flex items-center justify-center">
          <svg
            viewBox="0 0 200 160"
            className="w-full h-full"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
          >
            {/* Giant Process Yellow Moon */}
            <circle cx="100" cy="70" r="45" fill="#FFD700" stroke="#000000" strokeWidth="2.5" />

            {/* Halftone Texture Overlay on Moon */}
            <pattern id="cover-halftone" x="0" y="0" width="6" height="6" patternUnits="userSpaceOnUse">
              <circle cx="2" cy="2" r="1" fill="#000000" opacity="0.15" />
            </pattern>
            <circle cx="100" cy="70" r="44" fill="url(#cover-halftone)" />

            {/* Skyscraper Silhouettes in Background */}
            <rect x="25" y="65" width="22" height="95" fill="#1C1D24" stroke="#000000" strokeWidth="2" />
            <rect x="52" y="45" width="26" height="115" fill="#0E0F14" stroke="#000000" strokeWidth="2" />
            <rect x="122" y="50" width="28" height="110" fill="#0E0F14" stroke="#000000" strokeWidth="2" />
            <rect x="154" y="70" width="24" height="90" fill="#1C1D24" stroke="#000000" strokeWidth="2" />

            {/* Lit Windows in Buildings */}
            <rect x="56" y="55" width="4" height="6" fill="#00A3E0" />
            <rect x="66" y="55" width="4" height="6" fill="#FFD700" />
            <rect x="56" y="70" width="4" height="6" fill="#FFD700" />
            <rect x="66" y="85" width="4" height="6" fill="#00A3E0" />
            <rect x="128" y="60" width="4" height="6" fill="#E60050" />
            <rect x="140" y="75" width="4" height="6" fill="#00A3E0" />

            {/* Rooftop Ledge Silhouette */}
            <path d="M10 140 L190 140 L190 160 L10 160 Z" fill="#000000" />
            <path d="M40 140 L60 120 L140 120 L160 140 Z" fill="#000000" stroke="#000000" strokeWidth="2" />

            {/* Masked Hero Silhouette Standing on Ledge */}
            {/* Cloak & Torso */}
            <path
              d="M92 95 C92 90 96 86 100 86 C104 86 108 90 108 95 L114 122 L86 122 Z"
              fill="#000000"
            />
            {/* Billowing Cape */}
            <path
              d="M86 98 C75 105 68 115 65 130 C74 124 82 122 88 122 Z"
              fill="#000000"
            />
            {/* Head & Mask */}
            <circle cx="100" cy="82" r="6" fill="#000000" />
            {/* Glowing Cybernetic Eye / Visor */}
            <line x1="97" y1="82" x2="103" y2="82" stroke="#00A3E0" strokeWidth="1.5" />
          </svg>
        </div>

        {/* Cover Bottom Details: Parody Code Authority Stamp & Barcode */}
        <div className="border-t-2 border-black pt-2 flex items-end justify-between">
          {/* Simulated Barcode */}
          <div className="flex items-center gap-0.5 bg-white p-1 border border-black">
            <div className="w-1 h-6 bg-black" />
            <div className="w-0.5 h-6 bg-black" />
            <div className="w-1.5 h-6 bg-black" />
            <div className="w-0.5 h-6 bg-black" />
            <div className="w-1 h-6 bg-black" />
            <div className="w-2 h-6 bg-black" />
            <div className="w-0.5 h-6 bg-black" />
            <div className="w-1 h-6 bg-black" />
          </div>

          {/* Open Source Seal Stamp */}
          <div className="border-2 border-black bg-white p-1 text-center font-mono text-[7px] font-black leading-tight shadow-[1px_1px_0px_#000]">
            <span className="block text-crimson font-black">APPROVED BY</span>
            <span className="block text-black">OPEN SOURCE</span>
            <span className="block text-[6px] text-muted">CODE AUTHORITY</span>
          </div>
        </div>
      </div>
    </div>
  );
}

// ==========================================
// PAGE 2: INTERIOR STORY SPREAD (PAGE A)
// ==========================================
export function ComicStoryPageA({ className = "" }: { className?: string }) {
  return (
    <div
      className={`relative w-full aspect-[2/3] max-w-[280px] sm:max-w-[310px] mx-auto border-[2.5px] border-black bg-[#F5EFEB] text-black shadow-[4px_4px_0px_#000000] p-2.5 flex flex-col justify-between overflow-hidden select-none transition-all duration-300 ${className}`}
    >
      {/* Top Page Folio */}
      <div className="flex justify-between items-center text-[9px] font-mono font-black border-b border-black pb-1">
        <span>CYBERPUNK CHRONICLES</span>
        <span className="bg-black text-white px-1.5 py-0.2">PAGE 14</span>
      </div>

      {/* Panel 1: Establishing Shot of Neon Metropolis (Wide Horizontal) */}
      <div className="relative border-2 border-black bg-[#10121A] h-24 sm:h-28 overflow-hidden my-1 shadow-[2px_2px_0px_#000]">
        {/* Border Caption Box */}
        <div className="absolute top-1 left-1 bg-amber text-black border border-black px-1.5 py-0.5 text-[8px] font-mono font-black uppercase z-10">
          SECTOR 07 · 00:42 AM
        </div>

        {/* Vector Cityscape & Flying Vehicle */}
        <svg viewBox="0 0 200 90" className="w-full h-full" fill="none">
          {/* Skyline */}
          <path d="M0 90 L0 50 L25 50 L35 30 L50 30 L60 90 Z" fill="#0A0B10" />
          <path d="M55 90 L55 35 L75 35 L85 20 L95 20 L105 90 Z" fill="#181A24" />
          <path d="M100 90 L100 40 L130 40 L140 90 Z" fill="#0A0B10" />
          <path d="M135 90 L135 25 L165 25 L175 90 Z" fill="#181A24" />
          <path d="M170 90 L170 45 L200 45 L200 90 Z" fill="#0A0B10" />
          {/* Neon Searchlights */}
          <polygon points="40,90 80,0 100,0" fill="#00A3E0" opacity="0.15" />
          <polygon points="150,90 110,0 130,0" fill="#FFD700" opacity="0.15" />
          {/* Hover Vehicle Silhouette */}
          <rect x="75" y="32" width="22" height="6" rx="3" fill="#00A3E0" stroke="#000" strokeWidth="1" />
          <line x1="97" y1="35" x2="115" y2="35" stroke="#00A3E0" strokeWidth="1.5" strokeDasharray="2 2" />
        </svg>
      </div>

      {/* Tier 2: Two Asymmetric Action Panels */}
      <div className="grid grid-cols-12 gap-1.5 flex-1 my-1">
        {/* Panel 2 (7 cols): Terminal Hacker Silhouette */}
        <div className="col-span-7 border-2 border-black bg-white p-2 relative flex flex-col justify-between shadow-[2px_2px_0px_#000]">
          {/* Speech Balloon */}
          <div className="relative bg-white border-2 border-black p-1.5 text-[8px] font-sans font-bold leading-tight shadow-[1.5px_1.5px_0px_#000]">
            &quot;The archive decryption protocol is running. No cloud leaks.&quot;
            {/* Balloon Tail */}
            <div className="absolute -bottom-1.5 left-4 w-2 h-2 bg-white border-r-2 border-b-2 border-black rotate-45" />
          </div>

          {/* Hacker Silhouette */}
          <div className="mt-auto pt-2 flex justify-center">
            <svg viewBox="0 0 100 60" className="w-20 h-12" fill="none">
              {/* Terminal Screen Glow */}
              <polygon points="10,55 30,15 70,15 90,55" fill="#00A3E0" opacity="0.25" />
              {/* Head & Shoulders Silhouette */}
              <circle cx="50" cy="22" r="10" fill="#000000" />
              <path d="M30 55 C30 40 40 34 50 34 C60 34 70 40 70 55 Z" fill="#000000" />
            </svg>
          </div>
        </div>

        {/* Panel 3 (5 cols): Mechanical Impact / Spark */}
        <div className="col-span-5 border-2 border-black bg-amber p-1.5 relative flex flex-col items-center justify-center shadow-[2px_2px_0px_#000] overflow-hidden">
          {/* Action Sound Effect */}
          <div className="font-display text-xl font-black text-black tracking-widest rotate-[-12deg] drop-shadow-[2px_2px_0px_#FFF]">
            ZZZZT!
          </div>
          <div className="text-[7px] font-mono font-black bg-black text-white px-1 mt-1">
            LOCAL ENGINE: OK
          </div>
        </div>
      </div>

      {/* Bottom Folio */}
      <div className="text-right text-[8px] font-mono font-bold text-muted">
        CONTINUED ON NEXT PAGE →
      </div>
    </div>
  );
}

// ==========================================
// PAGE 3: INTERIOR STORY SPREAD (PAGE B)
// ==========================================
export function ComicStoryPageB({ className = "" }: { className?: string }) {
  return (
    <div
      className={`relative w-full aspect-[2/3] max-w-[280px] sm:max-w-[310px] mx-auto border-[2.5px] border-black bg-[#F5EFEB] text-black shadow-[4px_4px_0px_#000000] p-2.5 flex flex-col justify-between overflow-hidden select-none transition-all duration-300 ${className}`}
    >
      {/* Top Page Folio */}
      <div className="flex justify-between items-center text-[9px] font-mono font-black border-b border-black pb-1">
        <span>CYBERPUNK CHRONICLES</span>
        <span className="bg-black text-white px-1.5 py-0.2">PAGE 15</span>
      </div>

      {/* Tier 1: Two Upper Panels */}
      <div className="grid grid-cols-2 gap-1.5 my-1 h-28 sm:h-32">
        {/* Panel 1: Leap Action */}
        <div className="border-2 border-black bg-[#161822] p-1 relative flex flex-col justify-between shadow-[2px_2px_0px_#000] overflow-hidden">
          <svg viewBox="0 0 100 80" className="w-full h-full" fill="none">
            {/* Motion Lines */}
            <line x1="10" y1="20" x2="80" y2="70" stroke="#FFD700" strokeWidth="2" strokeDasharray="3 3" />
            <line x1="20" y1="10" x2="90" y2="60" stroke="#FFD700" strokeWidth="2" strokeDasharray="3 3" />
            {/* Leaping Silhouette */}
            <circle cx="55" cy="40" r="7" fill="#FFF" />
            <path d="M48 45 L35 70 M55 47 L65 72 M48 42 L25 35 M55 42 L75 30" stroke="#FFF" strokeWidth="3" strokeLinecap="round" />
          </svg>
          <div className="absolute bottom-1 right-1 font-display text-base font-black text-crimson rotate-[-8deg] drop-shadow-[1px_1px_0px_#FFF]">
            WHAM!
          </div>
        </div>

        {/* Panel 2: Close-up Cybernetic Eye */}
        <div className="border-2 border-black bg-white p-1.5 relative flex flex-col justify-between shadow-[2px_2px_0px_#000]">
          <div className="relative bg-amber border border-black p-1 text-[8px] font-sans font-black leading-tight">
            &quot;Zero telemetry. We run pure local.&quot;
          </div>

          <svg viewBox="0 0 80 50" className="w-full h-10 mt-auto" fill="none">
            {/* Face Contour Silhouette */}
            <path d="M15 45 C20 20 60 20 65 45 Z" fill="#000" />
            {/* Glowing Blue Eye */}
            <circle cx="35" cy="32" r="4" fill="#00A3E0" />
            <circle cx="35" cy="32" r="2" fill="#FFF" />
            {/* Target Reticle */}
            <circle cx="35" cy="32" r="7" stroke="#00A3E0" strokeWidth="0.8" strokeDasharray="1 1" />
          </svg>
        </div>
      </div>

      {/* Panel 3: Bottom Full-Width Dramatic Splash Landing */}
      <div className="border-2 border-black bg-[#0C0D12] p-2 relative flex-1 my-1 shadow-[2px_2px_0px_#000] flex flex-col justify-between overflow-hidden">
        {/* Diegetic Narration Caption */}
        <div className="self-start bg-crimson text-white border border-black px-1.5 py-0.5 text-[8px] font-mono font-black uppercase">
          SPLASHDOWN
        </div>

        {/* Hero Landing Silhouette & Puddle Reflection */}
        <svg viewBox="0 0 200 70" className="w-full h-full" fill="none">
          {/* Halftone Dot Rays */}
          <circle cx="100" cy="55" r="40" stroke="#222634" strokeWidth="1" strokeDasharray="2 2" />
          <circle cx="100" cy="55" r="60" stroke="#222634" strokeWidth="1" strokeDasharray="2 2" />
          {/* Landing Figure */}
          <path d="M90 55 L95 40 L105 40 L110 55 Z" fill="#FFD700" />
          <circle cx="100" cy="34" r="5" fill="#FFD700" />
          {/* Fist to Ground Impact */}
          <circle cx="100" cy="55" r="3" fill="#FFF" />
          <line x1="85" y1="55" x2="115" y2="55" stroke="#00A3E0" strokeWidth="2" />
        </svg>

        {/* Final Dialogue Balloon */}
        <div className="self-end bg-white border-2 border-black px-2 py-0.5 text-[8px] font-mono font-black text-black shadow-[1.5px_1.5px_0px_#000]">
          &quot;KOMIK IS NATIVE.&quot;
        </div>
      </div>

      {/* Bottom Folio */}
      <div className="flex justify-between items-center text-[8px] font-mono font-bold text-muted">
        <span>ISSUE #01 · 2026</span>
        <span>KOMIK READER</span>
      </div>
    </div>
  );
}
