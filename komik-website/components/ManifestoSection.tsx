"use client";

import { WifiOff, UserX, Lock, HardDrive, ShieldCheck, Check } from "lucide-react";

export default function ManifestoSection() {
  const pillars = [
    {
      icon: WifiOff,
      title: "100% Offline & Disconnected",
      desc: "Zero external network calls. Komik functions completely offline whether you are on an airplane, in a subway, or completely off the grid.",
      accent: "bg-black text-white",
    },
    {
      icon: UserX,
      title: "No Accounts, Ever",
      desc: "No email address requested, no sign-in screens, no authentication tokens, and no passwords. Install and read immediately.",
      accent: "bg-black text-white",
    },
    {
      icon: Lock,
      title: "Zero Telemetry or Analytics",
      desc: "No tracking SDKs, no crash-beacon telemetry, no session recordings. What you read and how long you read is strictly your business.",
      accent: "bg-black text-white",
    },
    {
      icon: HardDrive,
      title: "Non-Destructive Local SQLite",
      desc: "All progress, custom tags, and bookmarks are stored in a standard local SQLite database. Komik never alters or deletes your original files.",
      accent: "bg-black text-white",
    },
  ];

  return (
    <section id="why-offline" className="relative border-b-[3px] border-black bg-ink py-16 sm:py-24">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Creator's Editorial Paper Slab Frame */}
        <div className="relative border-[3px] border-black bg-paper text-black p-6 sm:p-10 lg:p-14 shadow-[8px_8px_0px_#000000]">
          {/* Overlapping Diegetic Caption Box */}
          <div className="absolute -top-3.5 left-6 sm:left-10 z-20 caption-box-magenta px-3.5 py-1 text-xs font-black tracking-wider rotate-[-0.6deg]">
            EDITORIAL · FROM THE DRAFTING TABLE · THE LOCAL-FIRST PLEDGE
          </div>

          <div className="max-w-4xl">
            <h2 className="font-display text-3xl sm:text-4xl lg:text-5xl font-black tracking-tight text-black leading-[1.06]">
              Your comics belong on your PC. <br />
              <span className="bg-black text-amber px-2.5 py-0.5 inline-block mt-1 border-2 border-black shadow-[3px_3px_0px_#000000] rotate-[-0.5deg]">
                Not in someone else&apos;s cloud.
              </span>
            </h2>

            <p className="mt-6 text-base sm:text-lg text-black/85 leading-relaxed font-medium">
              Most modern comic readers demand that you create an account, upload your personal archive to their remote servers, agree to behavioral telemetry, or pay a monthly subscription just to turn a digital page.
            </p>

            <p className="mt-4 text-base sm:text-lg text-black font-black">
              Komik is deliberately engineered in complete opposition to that model:
            </p>

            {/* 4 Pillars Grid: Tactile Newsprint Panels with Black Gutters */}
            <div className="mt-8 grid grid-cols-1 sm:grid-cols-2 gap-4 font-mono">
              {pillars.map((p) => {
                const Icon = p.icon;
                return (
                  <div
                    key={p.title}
                    className="border-2 border-black bg-white p-5 shadow-[3px_3px_0px_#000000] flex flex-col justify-between"
                  >
                    <div>
                      <div className="flex items-center gap-2 mb-2 font-black text-black text-sm uppercase">
                        <div className="h-6 w-6 bg-black text-amber flex items-center justify-center border border-black shrink-0">
                          <Icon className="h-3.5 w-3.5 stroke-[2.5]" />
                        </div>
                        <span>{p.title}</span>
                      </div>
                      <p className="text-xs text-black/80 leading-relaxed font-sans font-medium">
                        {p.desc}
                      </p>
                    </div>

                    <div className="mt-4 pt-2 border-t border-black/20 flex items-center gap-1.5 text-[10px] font-bold text-black/60 uppercase">
                      <Check className="h-3 w-3 stroke-[3] text-black" />
                      Strictly Enforced
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Editorial Pledge Seal */}
            <div className="mt-8 pt-6 border-t-2 border-black flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 text-xs font-mono">
              <div className="flex items-center gap-2 text-black font-bold">
                <ShieldCheck className="h-4 w-4 text-black stroke-[2.5]" />
                <span className="uppercase tracking-wider">Independent Software Pledge · Built Without Venture Capital</span>
              </div>

              <div className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-black text-white text-[11px] font-black uppercase border border-black shadow-[2px_2px_0px_#000]">
                <span>100% Free & Open-Source Software</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
