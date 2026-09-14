import Navbar from "@/components/Navbar";
import MobileNotice from "@/components/MobileNotice";
import Hero from "@/components/Hero";
import FormatShowcase from "@/components/FormatShowcase";
import FeaturePanels from "@/components/FeaturePanels";
import ManifestoSection from "@/components/ManifestoSection";
import KeyboardSection from "@/components/KeyboardSection";
import DownloadSection from "@/components/DownloadSection";
import Footer from "@/components/Footer";

export default function HomePage() {
  return (
    <div className="bg-ink text-newsprint min-h-screen flex flex-col">
      <MobileNotice />
      <Navbar />
      <main className="flex-1">
        <Hero />
        <FormatShowcase />
        <FeaturePanels />
        <ManifestoSection />
        <KeyboardSection />
        <DownloadSection />
      </main>
      <Footer />
    </div>
  );
}
