import { AnimatePresence, MotionConfig, motion } from "framer-motion";
import { StrictMode, useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import "@/ui/styles.css";
import { Wordmark } from "@/ui/components/Brand";
import { Button, Tabs } from "@/ui/components/Controls";
import { useActiveTab, useHistory, useJobs, useSettings, useTheme } from "@/ui/hooks";
import { setSoundEnabled } from "@/ui/sound";
import { HistoryView } from "@/ui/views/HistoryView";
import { QueueView } from "@/ui/views/QueueView";
import { ScanView } from "@/ui/views/ScanView";

type View = "page" | "queue" | "history";

function SidePanel() {
  const [settings, update, ready] = useSettings();
  useTheme(settings, ready);
  useEffect(() => setSoundEnabled(settings.sounds), [settings.sounds]);
  const tab = useActiveTab();
  const snapshot = useJobs();
  const [history, refreshHistory] = useHistory();
  const [view, setView] = useState<View>(() => (location.hash === "#page" ? "page" : "queue"));
  const [scanKey, setScanKey] = useState(0);

  // Rescan when the user switches tabs or navigates while the panel shows "This page".
  useEffect(() => setScanKey((k) => k + 1), [tab?.id, tab?.url]);

  const active = snapshot.jobs.filter((j) => !["done", "cancelled", "error", "paused"].includes(j.status)).length;

  return (
    <MotionConfig reducedMotion={settings.reducedMotion ? "always" : "user"}>
      <div className="flex h-screen min-w-[320px] flex-col overflow-hidden bg-ink">
        <header className="relative overflow-hidden border-b-[3px] border-gutter bg-panel px-3 pt-2">
          <div className="absolute inset-0 bg-speedlines" />
          <div className="relative mb-2 flex items-center gap-2">
            <Wordmark sub="Download manager" />
            <div className="ml-auto flex gap-1.5">
              <Button size="sm" icon={document.documentElement.classList.contains("dark") ? "sun" : "moon"} title="Switch theme" onClick={() => update({ theme: document.documentElement.classList.contains("dark") ? "light" : "dark" })} />
              <Button size="sm" icon="settings" title="Settings" onClick={() => chrome.runtime.openOptionsPage()} />
            </div>
          </div>
          <div className="relative -mb-[3px]">
            <Tabs<View>
              value={view}
              onChange={setView}
              tabs={[
                { id: "queue", label: "Queue", count: active, icon: "download" },
                { id: "page", label: "This page", icon: "search" },
                { id: "history", label: "History", count: history.length, icon: "book" }
              ]}
            />
          </div>
        </header>
        <AnimatePresence mode="wait">
          <motion.div key={view === "page" ? `page-${scanKey}` : view} initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -8 }} transition={{ duration: 0.16 }} className="flex min-h-0 flex-1 flex-col">
            {view === "queue" && <QueueView snapshot={snapshot} onScan={() => setView("page")} />}
            {view === "page" && <ScanView tab={tab} settings={settings} onOpenQueue={() => setView("queue")} />}
            {view === "history" && <HistoryView items={history} onChanged={refreshHistory} />}
          </motion.div>
        </AnimatePresence>
      </div>
    </MotionConfig>
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <SidePanel />
  </StrictMode>
);
