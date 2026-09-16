import { useEffect, useRef, useState } from "react";
import { send } from "@/shared/messages";
import { hostOf } from "@/shared/util";

const prepared = new Set<string>();

/**
 * Shows a picture from another site. The Referer that site expects is set up first, and if the browser
 * still refuses to show it (hotlink protection, or images the site marks as not shareable across sites),
 * the extension fetches it instead, so a preview never ends up blank.
 */
export function RemoteImage({ src, referer, alt = "", className = "", fallback }: { src?: string; referer?: string; alt?: string; className?: string; fallback?: React.ReactNode }) {
  const [ready, setReady] = useState(!src || !referer || prepared.has(`${hostOf(src)}|${referer}`));
  const [failed, setFailed] = useState(false);
  const [inlined, setInlined] = useState<string | null>(null);
  const triedFetch = useRef(false);

  useEffect(() => {
    setFailed(false);
    setInlined(null);
    triedFetch.current = false;
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

  /** The browser wouldn't show it: pull the bytes through the extension, which sites can't block. */
  const loadThroughExtension = async () => {
    if (!src || triedFetch.current) {
      setFailed(true);
      return;
    }
    triedFetch.current = true;
    const res = await send("fetch-image", { url: src, referer }).catch(() => ({ error: "failed" }) as { error: string });
    if ("base64" in res && res.base64) setInlined(`data:${res.mime || "image/jpeg"};base64,${res.base64}`);
    else setFailed(true);
  };

  if (!src || failed) {
    return <div className={`grid place-items-center bg-halftone-strong ${className}`}>{fallback}</div>;
  }
  if (!ready) return <div className={`animate-pulse bg-halftone-strong ${className}`} />;
  return (
    <img
      src={inlined ?? src}
      alt={alt}
      loading="lazy"
      decoding="async"
      draggable={false}
      className={className}
      onError={() => void loadThroughExtension()}
    />
  );
}
