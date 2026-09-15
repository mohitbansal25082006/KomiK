import { useEffect, useState } from "react";
import { send } from "@/shared/messages";
import { hostOf } from "@/shared/util";

const prepared = new Set<string>();

/** Loads a page/cover thumbnail from another site, adding its Referer first so hotlink protection allows it. */
export function RemoteImage({ src, referer, alt = "", className = "", fallback }: { src?: string; referer?: string; alt?: string; className?: string; fallback?: React.ReactNode }) {
  const [ready, setReady] = useState(!src || !referer || prepared.has(`${hostOf(src)}|${referer}`));
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    setFailed(false);
    if (!src || !referer || src.startsWith("data:")) {
      setReady(true);
      return;
    }
    const key = `${hostOf(src)}|${referer}`;
    if (prepared.has(key)) {
      setReady(true);
      return;
    }
    let alive = true;
    send("off-ensure-referer", { url: src, referer })
      .catch(() => undefined)
      .finally(() => {
        prepared.add(key);
        if (alive) setReady(true);
      });
    return () => {
      alive = false;
    };
  }, [src, referer]);

  if (!src || failed) {
    return <div className={`grid place-items-center bg-halftone-strong ${className}`}>{fallback}</div>;
  }
  if (!ready) return <div className={`animate-pulse bg-halftone-strong ${className}`} />;
  return <img src={src} alt={alt} loading="lazy" decoding="async" draggable={false} className={className} onError={() => setFailed(true)} />;
}
