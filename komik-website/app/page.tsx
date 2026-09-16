import Navbar from "@/components/Navbar";
import Hero from "@/components/Hero";
import StatsStrip from "@/components/StatsStrip";
import FormatShowcase from "@/components/FormatShowcase";
import ReadingEngine from "@/components/ReadingEngine";
import NewInV11 from "@/components/NewInV11";
import NewInV12 from "@/components/NewInV12";
import ExtensionSection from "@/components/extension/ExtensionSection";
import ManifestoSection from "@/components/ManifestoSection";
import KeyboardSection from "@/components/KeyboardSection";
import DownloadSection from "@/components/DownloadSection";
import Footer from "@/components/Footer";
import SmoothScroll from "@/components/fx/SmoothScroll";
import ClickBurst from "@/components/fx/ClickBurst";
import MotionProvider from "@/components/fx/MotionProvider";

export default function HomePage() {
  return (
    <MotionProvider>
      <div className="flex min-h-screen flex-col bg-ink text-newsprint">
        <SmoothScroll />
        <ClickBurst />
        <Navbar />
        <main className="flex-1">
          <Hero />
          <StatsStrip />
          <ExtensionSection />
          <FormatShowcase />
          <ReadingEngine />
          <NewInV12 />
          <NewInV11 />
          <ManifestoSection />
          <KeyboardSection />
          <DownloadSection />
        </main>
        <Footer />
      </div>
    </MotionProvider>
  );
}
