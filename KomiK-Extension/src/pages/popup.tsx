import { MotionConfig, motion } from "framer-motion";
import { StrictMode, useEffect } from "react";
import { createRoot } from "react-dom/client";
import "@/ui/styles.css";
import { send } from "@/shared/messages";
import { formatSpeed } from "@/shared/util";
import { Wordmark } from "@/ui/components/Brand";
import { Button, InkProgress } from "@/ui/components/Controls";
import { Icon } from "@/ui/components/Icon";
import { useActiveTab, useJobs, useSettings, useTheme } from "@/ui/hooks";
import { setSoundEnabled } from "@/ui/sound";
import { ScanView } from "@/ui/views/ScanView";

function Popup() {
  const [settings, update, ready] = useSettings();
  useTheme(settings, ready);
  useEffect(() => setSoundEnabled(settings.sounds), [settings.sounds]);
  const tab = useActiveTab();
  const snapshot = useJobs();

  const openPanel = async () => {
    const win = await chrome.windows.getCurrent();
    try {
      await chrome.sidePanel.open({ windowId: win.id! });
      window.close();
    } catch {
      await send("open-side-panel", { windowId: win.id });
    }
  };

  const active = snapshot.jobs.filter((j) => ["resolving", "downloading", "packing", "saving", "queued"].includes(j.status));
  const pagesTotal = active.reduce((s, j) => s + j.pages.length, 0);
  const pagesDone = active.reduce((s, j) => s + j.pages.filter((p) => p.done).length, 0);

  return (
    <MotionConfig reducedMotion={settings.reducedMotion ? "always" : "user"}>
      <div className="flex h-[600px] w-[430px] flex-col overflow-hidden bg-ink">
        <header className="relative flex items-center gap-2 overflow-hidden border-b-[3px] border-gutter bg-panel px-3 py-2">
          <div className="absolute inset-0 bg-speedlines" />
          <div className="relative"><Wordmark /></div>
          <div className="relative ml-auto flex gap-1.5">
            <Button size="sm" icon={settings.theme === "dark" || (settings.theme === "system" && matchMedia("(prefers-color-scheme: dark)").matches) ? "sun" : "moon"} title="Switch theme" onClick={() => update({ theme: document.documentElement.classList.contains("dark") ? "light" : "dark" })} />
            <div className="relative">
              <Button size="sm" icon="panel" title="Download manager" onClick={openPanel} />
              {active.length > 0 && <span className="absolute -right-1.5 -top-1.5 min-w-[18px] rounded-full border-2 border-gutter bg-magenta px-1 text-center font-bangers text-[11px] leading-[14px] text-white">{active.length}</span>}
            </div>
            <Button size="sm" icon="settings" title="Settings" onClick={() => chrome.runtime.openOptionsPage()} />
          </div>
        </header>

        <ScanView tab={tab} settings={settings} compact onOpenQueue={openPanel} />

        {active.length > 0 && (
          <motion.button initial={{ y: 40 }} animate={{ y: 0 }} onClick={openPanel} className="flex items-center gap-2.5 border-t-[3px] border-gutter bg-gutter px-3 py-2 text-left text-white">
            <Icon name="download" size={16} className="text-yellow" />
            <div className="min-w-0 flex-1">
              <div className="flex justify-between font-bangers text-[14px] tracking-wider">
                <span>{active.length} DOWNLOADING{pagesTotal ? ` · ${pagesDone}/${pagesTotal} PAGES` : ""}</span>
                <span className="text-yellow">{formatSpeed(snapshot.speedBps)}</span>
              </div>
              <div className="mt-1"><InkProgress value={pagesTotal ? pagesDone / pagesTotal : 0.05} moving /></div>
            </div>
            <Icon name="chevron" size={16} />
          </motion.button>
        )}
      </div>
    </MotionConfig>
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <Popup />
  </StrictMode>
);
