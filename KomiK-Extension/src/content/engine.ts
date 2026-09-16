// The full page engine, injected only when the user opens KomiK on a tab: deep scan (with auto-scroll
// for lazy readers), in-page fetch for hotlink-protected images, and the click-to-pick page picker.
import { scanDocument } from "@/engine/scan";
import { imageUrlFromElement } from "@/engine/detect/harvest";
import { envelope, isEnvelope, type MessageMap } from "@/shared/messages";
import type { DetectResult, PageRef } from "@/shared/types";
import { absoluteUrl, arrayBufferToBase64, sleep } from "@/shared/util";
import { ensureShadow, showToast } from "./ui";
import { sendSafely } from "./alive";

declare global {
  interface Window {
    __komikEngine?: boolean;
  }
}

(() => {
  if (window.__komikEngine) return;
  window.__komikEngine = true;

  /** Scrolls through the page so lazy loaders swap placeholders for real pages, then returns to the start. */
  async function autoScroll(maxMs = 14000): Promise<void> {
    const startY = scrollY;
    const started = Date.now();
    const lazySelector = "img[data-src], img[data-lazy-src], img[data-original], img[loading=lazy]";
    let lastHeight = 0;
    let stable = 0;
    const step = Math.max(600, Math.round(innerHeight * 0.9));
    while (Date.now() - started < maxMs) {
      window.scrollBy({ top: step, behavior: "instant" as ScrollBehavior });
      await sleep(140);
      const height = document.documentElement.scrollHeight;
      const atBottom = scrollY + innerHeight >= height - 4;
      if (atBottom) {
        stable = height === lastHeight ? stable + 1 : 0;
        lastHeight = height;
        if (stable >= 3) break;
        await sleep(350);
      }
    }
    // Give pending images a moment to start loading, then wait briefly for decode.
    const pending = Array.from(document.querySelectorAll<HTMLImageElement>(lazySelector)).filter((img) => !img.complete);
    await Promise.race([Promise.all(pending.slice(0, 60).map((img) => img.decode().catch(() => undefined))), sleep(2500)]);
    window.scrollTo({ top: startY, behavior: "instant" as ScrollBehavior });
  }

  async function scan(payload: MessageMap["content-scan"]["req"], networkImages?: Array<{ url: string }>): Promise<DetectResult> {
    const run = () => scanDocument(document, { url: location.href, live: true, rules: payload.rules, minPageWidth: payload.minPageWidth, networkImages });
    let result = run();
    const needsScroll = payload.deep || (payload.autoScroll && (result.lazyPending > 0 || (result.pages.length > 0 && result.pages.length < 4 && document.documentElement.scrollHeight > innerHeight * 3)));
    if (needsScroll) {
      await autoScroll();
      const again = run();
      if (again.pages.length >= result.pages.length) result = again;
    }
    if (pickedPages.length) {
      result = { ...result, pages: pickedPages, adapter: "Your picks", confidence: "high" };
    }
    return result;
  }

  async function fetchInPage(url: string): Promise<MessageMap["content-fetch"]["res"]> {
    try {
      const res = await fetch(url, { credentials: "include", referrer: location.href, referrerPolicy: "unsafe-url", cache: "force-cache" });
      if (!res.ok) return { error: `HTTP ${res.status}` };
      const buffer = await res.arrayBuffer();
      return { base64: arrayBufferToBase64(buffer), mime: res.headers.get("content-type") ?? "" };
    } catch (err) {
      // Images already painted on the page can still be read back when CORS blocks fetch.
      const img = Array.from(document.images).find((i) => i.currentSrc === url || i.src === url);
      if (img && img.complete && img.naturalWidth) {
        try {
          const canvas = document.createElement("canvas");
          canvas.width = img.naturalWidth;
          canvas.height = img.naturalHeight;
          canvas.getContext("2d")!.drawImage(img, 0, 0);
          const blob = await new Promise<Blob | null>((r) => canvas.toBlob(r, "image/png"));
          if (blob) return { base64: arrayBufferToBase64(await blob.arrayBuffer()), mime: "image/png" };
        } catch {
          /* tainted canvas */
        }
      }
      return { error: err instanceof Error ? err.message : String(err) };
    }
  }

  // ── Page picker ─────────────────────────────────────────────────────────────────────
  let pickedPages: PageRef[] = [];
  let picking = false;

  function startPicker() {
    if (picking) return;
    picking = true;
    const root = ensureShadow();
    const selected = new Map<Element, string>();
    const outline = document.createElement("style");
    outline.textContent = `
      [data-komik-hover] { outline: 4px dashed #00C2FF !important; outline-offset: -4px !important; cursor: copy !important; }
      [data-komik-picked] { outline: 5px solid #FFD700 !important; outline-offset: -5px !important; filter: saturate(1.15); }
    `;
    document.head.appendChild(outline);

    const bar = document.createElement("div");
    bar.className = "bar";
    bar.innerHTML = `<span class="title">PICK PAGES</span><span class="count">Click images to add them</span>
      <button class="all" type="button">ALL BIG IMAGES</button><button class="done" type="button">DONE</button><button class="cancel" type="button">CANCEL</button>`;
    root.appendChild(bar);
    const countEl = bar.querySelector(".count") as HTMLElement;
    const refresh = () => {
      countEl.textContent = selected.size ? `${selected.size} page${selected.size === 1 ? "" : "s"} picked` : "Click images to add them";
    };

    const imageAt = (target: EventTarget | null): Element | null => {
      const el = target as Element | null;
      if (!el || el === document.documentElement) return null;
      return el.closest("img, picture img, [style*=background-image]") ?? (el.querySelector?.("img") && el.children.length === 1 ? el.querySelector("img") : null);
    };
    const urlOf = (el: Element): string | null => {
      if (el.tagName === "IMG") return imageUrlFromElement(el, location.href).url;
      const m = /url\(["']?([^"')]+)["']?\)/.exec(getComputedStyle(el).backgroundImage);
      return m ? absoluteUrl(m[1], location.href) : null;
    };

    let hovered: Element | null = null;
    const onMove = (e: MouseEvent) => {
      const el = imageAt(e.target);
      if (el === hovered) return;
      hovered?.removeAttribute("data-komik-hover");
      hovered = el;
      el?.setAttribute("data-komik-hover", "");
    };
    const toggle = (el: Element) => {
      if (selected.has(el)) {
        selected.delete(el);
        el.removeAttribute("data-komik-picked");
      } else {
        const url = urlOf(el);
        if (!url) return;
        selected.set(el, url);
        el.setAttribute("data-komik-picked", "");
      }
      refresh();
    };
    const onClick = (e: MouseEvent) => {
      if (e.composedPath().includes(bar)) return;
      const el = imageAt(e.target);
      if (!el) return;
      e.preventDefault();
      e.stopPropagation();
      toggle(el);
    };
    const finish = (commit: boolean) => {
      picking = false;
      removeEventListener("mousemove", onMove, true);
      removeEventListener("click", onClick, true);
      removeEventListener("keydown", onKey, true);
      hovered?.removeAttribute("data-komik-hover");
      selected.forEach((_u, el) => el.removeAttribute("data-komik-picked"));
      outline.remove();
      bar.remove();
      if (!commit) return;
      // Keep reading order: sort by position in the document.
      const ordered = Array.from(selected.entries()).sort(([a], [b]) => (a.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1));
      pickedPages = ordered.map(([el, url]) => ({ url, referer: location.href, source: "picker", width: (el as HTMLImageElement).naturalWidth || undefined, height: (el as HTMLImageElement).naturalHeight || undefined }));
      void sendSafely(envelope("picker-finished", { pages: pickedPages, url: location.href }, "background"));
      showToast(`${pickedPages.length} PAGES PICKED!`, "Open KomiK again to download them.", "yellow");
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") finish(false);
      if (e.key === "Enter") finish(true);
    };
    (bar.querySelector(".done") as HTMLButtonElement).onclick = () => finish(true);
    (bar.querySelector(".cancel") as HTMLButtonElement).onclick = () => finish(false);
    (bar.querySelector(".all") as HTMLButtonElement).onclick = () => {
      document.querySelectorAll("img").forEach((img) => {
        if (!selected.has(img) && img.naturalWidth >= 380 && img.naturalHeight >= 380 && !img.closest("header, footer, nav")) toggle(img);
      });
    };
    addEventListener("mousemove", onMove, true);
    addEventListener("click", onClick, true);
    addEventListener("keydown", onKey, true);
  }

  chrome.runtime.onMessage.addListener((msg, _sender, sendResponse) => {
    if (!isEnvelope(msg) || msg.target !== "content") return false;
    switch (msg.type) {
      case "content-ping":
        sendResponse({ engine: true });
        return false;
      case "content-scan": {
        const payload = msg.payload as MessageMap["content-scan"]["req"] & { networkImages?: Array<{ url: string }> };
        scan(payload, payload.networkImages).then(sendResponse, (err) => sendResponse({ error: String(err) }));
        return true;
      }
      case "content-fetch":
        fetchInPage((msg.payload as { url: string }).url).then(sendResponse);
        return true;
      case "content-picker":
        startPicker();
        sendResponse({ ok: true });
        return false;
      default:
        return false;
    }
  });
})();
