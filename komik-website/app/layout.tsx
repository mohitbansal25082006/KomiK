import type { Metadata } from "next";
import { Space_Grotesk, Plus_Jakarta_Sans, JetBrains_Mono, Bangers, Comic_Neue } from "next/font/google";
import "./globals.css";
import { APP_CONFIG } from "@/lib/config";

const spaceGrotesk = Space_Grotesk({
  subsets: ["latin"],
  variable: "--font-space-grotesk",
  weight: ["500", "600", "700"],
});

const jakarta = Plus_Jakarta_Sans({
  subsets: ["latin"],
  variable: "--font-jakarta",
  weight: ["400", "500", "600", "700"],
});

const jetbrainsMono = JetBrains_Mono({
  subsets: ["latin"],
  variable: "--font-jetbrains-mono",
  weight: ["500", "700"],
});

const bangers = Bangers({
  subsets: ["latin"],
  variable: "--font-bangers",
  weight: ["400"],
});

const comicNeue = Comic_Neue({
  subsets: ["latin"],
  variable: "--font-comic",
  weight: ["400", "700"],
});

export const metadata: Metadata = {
  metadataBase: new URL("https://github.com/mohitbansal25082006/KomiK"),
  title: "Komik — Your Comics. Your PC. Zero Cloud. | Windows Comic & Manga Reader",
  description:
    "A lightning-fast, local-first Windows 11 comic reader built with WinUI 3 and .NET 8. Supports CBZ, CBR, CB7, PDF, and image folders. 100% offline, zero accounts, zero telemetry.",
  keywords: [
    "Comic reader",
    "Manga reader Windows",
    "CBZ reader",
    "CBR reader Windows 11",
    "WinUI 3 comic reader",
    "Offline comic viewer",
    "CB7 reader",
    "Open source comic reader",
    "Webtoon reader Windows",
    "Offline manga reader",
  ],
  authors: [{ name: APP_CONFIG.author, url: APP_CONFIG.repoUrl }],
  creator: APP_CONFIG.author,
  publisher: APP_CONFIG.author,
  robots: "index, follow",
  openGraph: {
    type: "website",
    locale: "en_US",
    url: APP_CONFIG.repoUrl,
    title: "Komik — Modern Fluent Comic & Manga Reader for Windows",
    description:
      "A lightning-fast, local-first Windows 11 comic reader built with WinUI 3 and .NET 8. 100% offline, zero accounts, zero telemetry.",
    siteName: "Komik",
    images: [
      {
        url: "/og-image.png",
        width: 1200,
        height: 630,
        alt: "Komik — Modern Fluent Comic Reader for Windows",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    title: "Komik — Modern Fluent Comic & Manga Reader for Windows",
    description:
      "Fast, local-first Windows 11 desktop comic reader. CBZ, CBR, CB7, PDF. Free & 100% offline.",
    images: ["/og-image.png"],
  },
  icons: {
    icon: "/favicon.ico",
    apple: "/app-icon.png",
  },
};

const INTRO_ONCE = `try{if(sessionStorage.getItem("komik-intro")){document.documentElement.classList.add("intro-seen")}else{sessionStorage.setItem("komik-intro","1")}}catch(e){}`;

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      suppressHydrationWarning
      className={`${spaceGrotesk.variable} ${jakarta.variable} ${jetbrainsMono.variable} ${bangers.variable} ${comicNeue.variable} bg-ink text-newsprint dark`}
    >
      <head>
        <script dangerouslySetInnerHTML={{ __html: INTRO_ONCE }} />
      </head>
      <body className="min-h-screen bg-ink text-newsprint antialiased selection:bg-amber selection:text-ink">
        <div className="intro-curtain" aria-hidden>
          <div className="intro-half intro-top" />
          <div className="intro-half intro-bottom" />
          <div className="intro-word">KOMIK!</div>
        </div>
        {children}
      </body>
    </html>
  );
}
