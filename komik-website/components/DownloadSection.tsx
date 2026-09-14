"use client";

import { Download, ExternalLink, ShieldAlert, Cpu, HardDrive, CheckCircle2 } from "lucide-react";
import { APP_CONFIG } from "@/lib/config";

export default function DownloadSection() {
  return (
    <section id="download" className="relative border-b-[3px] border-black bg-ink py-16 sm:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Central Download Command Center */}
        <div className="relative border-[3px] border-black bg-[#0F0F13] p-8 sm:p-12 lg:p-16 max-w-4xl mx-auto shadow-[10px_10px_0px_#000000] text-center">
          {/* Overlapping Diegetic Caption Box */}
          <div className="absolute -top-3.5 left-6 sm:left-10 z-20 caption-box px-3.5 py-1 text-xs font-black tracking-wider rotate-[-0.5deg]">
            FINAL PANEL · ACQUIRE KOMIK FOR WINDOWS
          </div>

          <h2 className="font-display text-4xl sm:text-5xl lg:text-6xl font-black tracking-tight text-newsprint leading-[1.04]">
            Start reading in seconds.
          </h2>

          <p className="mt-4 text-base sm:text-lg text-newsprint/80 max-w-xl mx-auto font-medium leading-relaxed">
            Self-contained Windows installer bundling the .NET 8 and Windows App SDK runtimes. No Developer Mode, no manual certificates, and no background services.
          </p>

          {/* Primary Action Buttons */}
          <div className="mt-8 flex flex-col sm:flex-row items-center justify-center gap-4">
            <a
              href={APP_CONFIG.downloadUrl}
              className="btn-comic-primary w-full sm:w-auto inline-flex items-center justify-center gap-3 px-10 py-4 text-base sm:text-lg font-black tracking-wider uppercase"
            >
              <Download className="h-5 w-5 stroke-[2.5]" />
              <span>Download for Windows (.exe)</span>
            </a>

            <a
              href={APP_CONFIG.releasesUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="btn-comic-secondary w-full sm:w-auto inline-flex items-center justify-center gap-2 px-6 py-4 text-sm font-bold tracking-wide"
            >
              <span>View Releases & Changelog</span>
              <ExternalLink className="h-4 w-4" />
            </a>
          </div>

          {/* Technical Specs Bar */}
          <div className="mt-10 pt-8 border-t-2 border-black grid grid-cols-1 sm:grid-cols-3 gap-3 text-xs font-mono text-left sm:text-center">
            <div className="border-2 border-black bg-[#15151B] p-3.5 shadow-[2px_2px_0px_#000]">
              <span className="block text-[10px] text-amber font-black uppercase tracking-wider">INSTALLER SIZE</span>
              <span className="text-newsprint font-black text-sm">{APP_CONFIG.installerSize}</span>
            </div>
            <div className="border-2 border-black bg-[#15151B] p-3.5 shadow-[2px_2px_0px_#000]">
              <span className="block text-[10px] text-cyan font-black uppercase tracking-wider">PLATFORM</span>
              <span className="text-newsprint font-black text-sm">Windows 10 / 11 64-bit</span>
            </div>
            <div className="border-2 border-black bg-[#15151B] p-3.5 shadow-[2px_2px_0px_#000]">
              <span className="block text-[10px] text-amber font-black uppercase tracking-wider">LICENSE</span>
              <span className="text-newsprint font-black text-sm">MIT Open Source</span>
            </div>
          </div>

          {/* Plain-Language Windows SmartScreen Reassurance Box */}
          <div className="mt-8 border-2 border-black bg-paper text-black p-5 text-left flex items-start gap-4 shadow-[4px_4px_0px_#000]">
            <div className="h-9 w-9 bg-amber border-2 border-black flex items-center justify-center shrink-0 shadow-[1px_1px_0px_#000] mt-0.5">
              <ShieldAlert className="h-5 w-5 text-black stroke-[2.5]" />
            </div>
            <div className="text-xs font-mono leading-relaxed">
              <strong className="text-black block font-black uppercase tracking-wider mb-1 text-sm">
                A Note on Windows SmartScreen (&quot;Windows protected your PC&quot;)
              </strong>
              Because Komik is free, community-distributed open-source software built without an expensive corporate code-signing certificate ($400+/year), Windows Defender SmartScreen may display an alert when you first run <code className="bg-black/10 px-1 py-0.5 font-mono font-black text-black">Komik-Setup.exe</code>.
              <br className="my-1.5" />
              To proceed: Click <strong className="text-black bg-amber px-1 py-0.5 border border-black">&quot;More info&quot;</strong>, then click <strong className="text-black bg-amber px-1 py-0.5 border border-black">&quot;Run anyway&quot;</strong>. The entire codebase is open-source for 100% security inspection.
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
