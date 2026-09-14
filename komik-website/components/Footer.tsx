import { APP_CONFIG } from "@/lib/config";
import { Download, Github } from "lucide-react";
import KomikLogo from "@/components/KomikLogo";

export default function Footer() {
  return (
    <footer className="border-t-[3px] border-black bg-ink py-12">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
          {/* Brand & Description */}
          <div className="flex items-start sm:items-center gap-3.5">
            <div className="flex h-10 w-10 items-center justify-center border-2 border-black bg-[#121316] shadow-[2px_2px_0px_#FFD700]">
              <KomikLogo size={26} />
            </div>
            <div>
              <div className="font-display text-lg font-black text-newsprint tracking-tight">
                {APP_CONFIG.name}
              </div>
              <p className="text-xs text-muted max-w-md font-medium">
                {APP_CONFIG.oneLiner}
              </p>
            </div>
          </div>

          {/* Action Links */}
          <div className="flex items-center gap-5 text-xs font-mono font-bold text-muted">
            <a
              href={APP_CONFIG.downloadUrl}
              className="text-amber hover:underline inline-flex items-center gap-1.5"
            >
              <Download className="h-3.5 w-3.5 stroke-[2.5]" />
              <span>Direct Download (.exe)</span>
            </a>

            <span className="text-black font-black">|</span>

            <a
              href={APP_CONFIG.repoUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="hover:text-newsprint transition-colors inline-flex items-center gap-1.5"
            >
              <Github className="h-3.5 w-3.5" />
              <span>GitHub Repository</span>
            </a>
          </div>
        </div>

        <div className="mt-8 pt-6 border-t-2 border-black flex flex-col sm:flex-row items-center justify-between gap-3 text-[11px] font-mono text-muted/80">
          <span>
            Created &amp; maintained by <strong className="text-newsprint font-bold">{APP_CONFIG.author}</strong>. Open source under the MIT License.
          </span>
          <span className="text-newsprint/60">
            Engineered natively for Windows 10 &amp; 11 with WinUI 3 and .NET 8.
          </span>
        </div>
      </div>
    </footer>
  );
}
