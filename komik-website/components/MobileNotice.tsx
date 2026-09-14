import { Monitor } from "lucide-react";

export default function MobileNotice() {
  return (
    <div className="md:hidden border-b-2 border-black bg-amber px-4 py-2 text-center text-xs text-black font-mono font-bold">
      <div className="flex items-center justify-center gap-1.5">
        <Monitor className="h-3.5 w-3.5 text-black shrink-0 stroke-[2.5]" />
        <span>Komik is a native Windows desktop app. Open on your PC to download.</span>
      </div>
    </div>
  );
}
