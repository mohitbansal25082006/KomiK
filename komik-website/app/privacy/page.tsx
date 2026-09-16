import type { Metadata } from "next";
import Link from "next/link";
import { ArrowLeft, Database, Download, Github, HardDrive, Mail, MonitorSmartphone, Puzzle, ShieldCheck } from "lucide-react";
import { APP_CONFIG, EXTENSION_CONFIG } from "@/lib/config";
import KomikLogo from "@/components/KomikLogo";

const UPDATED = "16 September 2026";

export const metadata: Metadata = {
  title: "Privacy Policy | Komik & KomiK Downloader",
  description:
    "Komik and the KomiK Downloader browser extension collect no personal data. Everything stays on your PC and in your browser: no accounts, no analytics, no servers.",
  robots: "index, follow",
  alternates: { canonical: "/privacy" },
};

const PROMISES = [
  { big: "0", small: "personal data collected", tone: "bg-amber" },
  { big: "0", small: "analytics or trackers", tone: "bg-cyan" },
  { big: "0", small: "servers of ours involved", tone: "bg-magenta text-white" },
  { big: "0", small: "data sold or shared", tone: "bg-paper" },
];

const PERMISSIONS: { name: string; why: string }[] = [
  { name: "Access to the sites you visit", why: "Reads the pages and details of the comic on the page you are viewing, and downloads that comic's images from the same site." },
  { name: "scripting", why: "Runs the page scanner on the current tab when you open KomiK, to find the comic's pages and details." },
  { name: "downloads · downloads.open", why: "Saves the finished comic to your Downloads folder, and opens or shows it when you click Open or Show in folder." },
  { name: "storage · unlimitedStorage", why: "Keeps your settings, the queue and history, and holds downloaded pages temporarily so a large download can resume after a restart." },
  { name: "tabs", why: "Knows which page to scan, and opens chapter pages in a background tab during a series download." },
  { name: "webRequest", why: "Notices page images a reader loads with scripts, so no page is missed. Nothing is changed, blocked or recorded." },
  { name: "declarativeNetRequest", why: "Sends the page's own address as Referer when fetching its images, for sites that refuse direct image links." },
  { name: "offscreen", why: "Builds CBZ and PDF files in the background without slowing the browser." },
  { name: "sidePanel · contextMenus · notifications · alarms", why: "Shows the download manager, adds the right-click menu item, tells you when a download finishes and keeps long downloads running." },
];

function Section({ id, kicker, title, tone, icon: Icon, children }: { id: string; kicker: string; title: string; tone: string; icon: typeof ShieldCheck; children: React.ReactNode }) {
  return (
    <section id={id} className="scroll-mt-24 border-[3px] border-black bg-panel text-newsprint shadow-[8px_8px_0_#000]">
      <header className={`${tone} flex flex-wrap items-center justify-between gap-2 border-b-[3px] border-black px-5 py-3 text-black`}>
        <h2 className="flex items-center gap-2 font-bangers text-3xl leading-none tracking-wide sm:text-4xl">
          <Icon className="h-6 w-6 sm:h-7 sm:w-7" /> {title}
        </h2>
        <span className="border-2 border-black bg-black px-2 py-0.5 font-mono text-[10px] font-black uppercase text-white">{kicker}</span>
      </header>
      <div className="space-y-4 p-5 text-[15px] leading-relaxed text-newsprint/85 sm:p-7 [&_h3]:mt-6 [&_h3]:font-bangers [&_h3]:text-2xl [&_h3]:tracking-wide [&_h3]:text-newsprint [&_li]:ml-5 [&_li]:list-disc [&_li]:pl-1 [&_strong]:text-newsprint [&_ul]:space-y-1.5">
        {children}
      </div>
    </section>
  );
}

export default function PrivacyPage() {
  return (
    <div className="flex min-h-screen flex-col bg-ink text-newsprint">
      <header className="sticky top-0 z-50 border-b-[3px] border-black bg-ink">
        <div className="mx-auto flex h-16 max-w-5xl items-center justify-between gap-4 px-4 sm:px-6">
          <Link href="/" className="group flex items-center gap-3" aria-label="Komik home">
            <span className="flex h-10 w-10 items-center justify-center border-2 border-black bg-[#121316] shadow-[3px_3px_0_#FFD700]">
              <KomikLogo size={28} />
            </span>
            <span className="font-bangers text-2xl leading-none tracking-wider transition-colors group-hover:text-amber">KOMIK</span>
          </Link>
          <div className="flex items-center gap-2 sm:gap-3">
            <Link href="/" className="btn-comic-primary inline-flex items-center gap-2 px-3 py-2 text-xs uppercase tracking-wider">
              <ArrowLeft className="h-4 w-4" /> <span className="hidden sm:inline">Back to the comic</span>
              <span className="sm:hidden">Home</span>
            </Link>
          </div>
        </div>
      </header>

      <main className="relative flex-1 overflow-hidden">
        <div aria-hidden className="pointer-events-none absolute inset-0">
          <div className="bg-halftone-fade absolute inset-0" />
          <div className="absolute -left-40 top-0 h-[600px] w-[600px] bg-[radial-gradient(circle,rgba(0,194,255,0.16),transparent_65%)]" />
          <div className="absolute -right-40 top-60 h-[600px] w-[600px] bg-[radial-gradient(circle,rgba(255,31,109,0.14),transparent_65%)]" />
        </div>

        <div className="relative mx-auto max-w-5xl px-4 pb-24 pt-12 sm:px-6 sm:pt-16">
          <div className="caption-box inline-block -rotate-1 px-3 py-1 text-xs">The fine print · Updated {UPDATED}</div>
          <h1 className="mt-5 font-bangers leading-[0.9] tracking-wide">
            <span className="text-comic-outline block text-[16vw] text-newsprint sm:text-8xl">Privacy</span>
            <span className="text-comic-outline block text-[16vw] text-cyan sm:text-8xl">Policy</span>
          </h1>
          <p className="mt-6 max-w-2xl text-lg font-medium leading-relaxed text-newsprint/85">
            This policy covers the <strong>Komik</strong> desktop app ({APP_CONFIG.version}) and the <strong>{EXTENSION_CONFIG.name}</strong> browser extension ({EXTENSION_CONFIG.version}). The
            short version: <mark className="bg-amber px-1 font-bold text-black">we collect nothing.</mark> Your comics, your library and your reading stay on your devices.
          </p>

          <div className="mt-10 grid grid-cols-2 gap-4 md:grid-cols-4">
            {PROMISES.map((p, i) => (
              <div key={p.small} className={`${p.tone} relative border-[3px] border-black p-4 text-black shadow-[6px_6px_0_#000]`} style={{ transform: `rotate(${[-2, 1.5, -1, 2][i]}deg)` }}>
                <div className="bg-halftone-paper pointer-events-none absolute inset-0 opacity-40" />
                <div className="relative font-bangers text-6xl leading-none">{p.big}</div>
                <div className="relative mt-1 text-xs font-extrabold uppercase leading-snug">{p.small}</div>
              </div>
            ))}
          </div>

          <nav aria-label="On this page" className="mt-10 flex flex-wrap gap-2 font-mono text-[11px] font-black uppercase">
            {[
              ["#extension", "KomiK Downloader"],
              ["#app", "Komik app"],
              ["#sharing", "Sharing"],
              ["#your-choices", "Your choices"],
              ["#contact", "Contact"],
            ].map(([href, label]) => (
              <a key={href} href={href} className="btn-comic-secondary px-3 py-1.5">
                {label}
              </a>
            ))}
          </nav>

          <div className="mt-10 space-y-10">
            <Section id="extension" kicker="Browser extension" title={EXTENSION_CONFIG.name} tone="bg-cyan" icon={Puzzle}>
              <p>
                {EXTENSION_CONFIG.name} saves the comic you are reading as a CBZ, ZIP, PDF or folder with its details embedded. It does <strong>not</strong> collect, store, transmit, sell or
                share any personal data, and it contains no analytics, advertising or tracking code. It has no account system and talks to no server run by us.
              </p>

              <h3>What it reads, and when</h3>
              <ul>
                <li>
                  <strong>The comic page you choose to download.</strong> When you open KomiK (toolbar icon, floating button, right-click menu or shortcut), it reads that page&apos;s images,
                  title, credits, tags and chapter list to build the comic. This happens entirely inside your browser.
                </li>
                <li>
                  <strong>A quick page count.</strong> A small script counts large images on pages you visit so the toolbar badge and floating button can appear. It reads nothing else, and
                  the count never leaves your browser.
                </li>
                <li>
                  <strong>Chapter pages for series downloads.</strong> When you download several chapters, each chapter page is opened in a background tab so its pages can be found, then
                  closed.
                </li>
              </ul>

              <h3>Network requests</h3>
              <p>
                The only requests the extension makes are to the website you are downloading from: the comic&apos;s pages, images and chapter pages, the same ones your browser would load if
                you read them. It may send that site&apos;s own address as the Referer header so images load. Nothing is sent anywhere else.
              </p>

              <h3>What stays in your browser</h3>
              <ul>
                <li>
                  <strong>Settings</strong> (folder, naming, formats, theme and so on) in the browser&apos;s extension storage.
                </li>
                <li>
                  <strong>The download queue and pages in progress</strong>, so downloads survive a restart. Pages are deleted once the comic is saved or the job is removed.
                </li>
                <li>
                  <strong>Download history</strong>: title, series, tags, a small cover picture, page count, file size, the page address it came from and where it was saved.
                </li>
                <li>
                  <strong>Series memory</strong>: corrections you make to a series&apos; details, if that setting is on.
                </li>
                <li>
                  <strong>Your saved comics</strong> go to your Downloads folder (by default <code className="bg-black/30 px-1 font-mono text-sm">Downloads/KomiK</code>) through the
                  browser&apos;s normal downloads.
                </li>
              </ul>

              <h3>Permissions and why they are needed</h3>
              <div className="overflow-hidden border-[3px] border-black">
                {PERMISSIONS.map((p, i) => (
                  <div key={p.name} className={`grid gap-1 px-4 py-3 sm:grid-cols-[230px_1fr] sm:gap-4 ${i % 2 ? "bg-panel-card" : "bg-ink-deep"}`}>
                    <div className="font-mono text-xs font-black text-amber">{p.name}</div>
                    <div className="text-sm text-newsprint/85">{p.why}</div>
                  </div>
                ))}
              </div>
              <p className="text-sm text-newsprint/70">
                The use of information received from browser APIs adheres to the Chrome Web Store User Data Policy, including the Limited Use requirements.
              </p>
            </Section>

            <Section id="app" kicker="Windows desktop app" title="Komik" tone="bg-amber" icon={MonitorSmartphone}>
              <p>
                Komik is a local-first reader. It makes <strong>no network calls</strong>, has <strong>no accounts</strong> and contains <strong>no telemetry or analytics</strong>. It never uploads
                your comics or anything about your reading.
              </p>
              <h3>What stays on your PC</h3>
              <ul>
                <li>
                  <strong>Your library</strong>: the comics you add, their details and tags, series, collections, bookmarks, reading progress and reading-time statistics, in a database under{" "}
                  <code className="bg-black/30 px-1 font-mono text-sm">%USERPROFILE%\.komik</code>.
                </li>
                <li>
                  <strong>Cover thumbnails</strong> and a small library cache in the same folder, so covers load quickly and your library can be restored.
                </li>
                <li>
                  <strong>Settings and window position</strong>, stored with the library.
                </li>
                <li>
                  <strong>A crash log</strong> in <code className="bg-black/30 px-1 font-mono text-sm">%LOCALAPPDATA%\Komik</code> if something goes wrong. It stays on your PC.
                </li>
                <li>
                  <strong>Backups</strong> (<code className="bg-black/30 px-1 font-mono text-sm">.komikbackup</code>) only where you choose to save them.
                </li>
              </ul>
              <p>Text recognition (OCR) uses the recognizer built into Windows and runs on your PC. Comic files are only read, never changed, unless you convert or delete one yourself.</p>
            </Section>

            <Section id="sharing" kicker="Third parties" title="Sharing & selling" tone="bg-magenta text-white" icon={Database}>
              <p>
                We do not sell, rent, trade or share any data with anyone, because we do not have any. Neither product includes third-party services, advertising or remote code. The websites
                you download from have their own privacy policies, which apply to your visits there.
              </p>
              <p>Neither product is directed at children under 13, and neither knowingly handles personal information about anyone.</p>
            </Section>

            <Section id="your-choices" kicker="Control" title="Your choices" tone="bg-[#2FD17A]" icon={HardDrive}>
              <ul>
                <li>
                  <strong>Extension:</strong> clear the history from the side panel, cancel or clear downloads in the queue, reset settings on the options page, or remove the extension, which
                  deletes everything it stored. Your saved comics stay in your Downloads folder.
                </li>
                <li>
                  <strong>Komik:</strong> remove comics from the library at any time. Uninstall from Windows Settings, then delete{" "}
                  <code className="bg-black/30 px-1 font-mono text-sm">%USERPROFILE%\.komik</code> to erase the library, thumbnails and settings.
                </li>
              </ul>
              <h3>Changes to this policy</h3>
              <p>
                If this policy changes, the new version will be posted on this page with a new date at the top. Because neither product collects data, a change can never apply to anything
                collected earlier.
              </p>
            </Section>

            <Section id="contact" kicker="Questions" title="Contact" tone="bg-paper" icon={Mail}>
              <p>
                Komik and {EXTENSION_CONFIG.name} are free, open-source projects by <strong>{APP_CONFIG.author}</strong>. Every line of code is public, so you can check all of the above for
                yourself. For questions about this policy, open an issue on GitHub.
              </p>
              <div className="flex flex-wrap gap-3">
                <a href={APP_CONFIG.issuesUrl} target="_blank" rel="noopener noreferrer" className="btn-comic-primary inline-flex items-center gap-2 px-4 py-2.5 text-sm">
                  <Github className="h-4 w-4" /> Ask on GitHub
                </a>
                <a href={APP_CONFIG.repoUrl} target="_blank" rel="noopener noreferrer" className="btn-comic-secondary inline-flex items-center gap-2 px-4 py-2.5 text-sm">
                  Read the source
                </a>
                <Link href="/#download" className="btn-comic-secondary inline-flex items-center gap-2 px-4 py-2.5 text-sm">
                  <Download className="h-4 w-4" /> Get Komik
                </Link>
              </div>
            </Section>
          </div>
        </div>
      </main>

      <footer className="border-t-[3px] border-black bg-ink py-6">
        <div className="mx-auto flex max-w-5xl flex-col items-center justify-between gap-2 px-4 font-mono text-[11px] text-muted sm:flex-row sm:px-6">
          <span>
            © 2026 {APP_CONFIG.author} · MIT License
          </span>
          <span>Effective {UPDATED}</span>
        </div>
      </footer>
    </div>
  );
}
