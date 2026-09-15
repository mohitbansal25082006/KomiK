// Tiny Shadow-DOM UI used inside web pages (no framework): the floating crest button, comic toasts
// and the page picker bar. Site CSS can't leak in and ours can't leak out.

export const INK = "#08080A";
export const YELLOW = "#FFD700";
export const CYAN = "#00C2FF";
export const MAGENTA = "#FF1F6D";

let host: HTMLElement | null = null;
let shadow: ShadowRoot | null = null;

function fontUrl(): string {
  try {
    return chrome.runtime.getURL("fonts/Bangers-Regular.woff2");
  } catch {
    return "";
  }
}

export function ensureShadow(): ShadowRoot {
  if (shadow && host?.isConnected) return shadow;
  host = document.createElement("komik-downloader-ui");
  host.style.cssText = "all: initial; position: fixed; z-index: 2147483646; inset: auto 0 0 auto; width: 0; height: 0;";
  shadow = host.attachShadow({ mode: "closed" });
  const style = document.createElement("style");
  const reduced = matchMedia("(prefers-reduced-motion: reduce)").matches;
  style.textContent = `
    @font-face { font-family: "KomikBangers"; src: url("${fontUrl()}") format("woff2"); font-display: swap; }
    * { box-sizing: border-box; }
    .fab { position: fixed; right: 18px; bottom: 18px; width: 58px; height: 58px; border-radius: 50%;
      background: ${YELLOW}; border: 3px solid ${INK}; box-shadow: 4px 4px 0 ${INK}; cursor: pointer; display: grid; place-items: center;
      transition: transform .18s cubic-bezier(.34,1.56,.64,1), box-shadow .18s; padding: 0; }
    .fab:hover { transform: translate(-2px,-2px) rotate(-6deg) scale(1.06); box-shadow: 6px 6px 0 ${INK}; }
    .fab:active { transform: translate(2px,2px) scale(.94); box-shadow: 1px 1px 0 ${INK}; }
    .fab svg { width: 38px; height: 38px; }
    .fab .count { position: absolute; top: -8px; right: -10px; min-width: 26px; height: 24px; padding: 0 6px; border-radius: 12px;
      background: ${MAGENTA}; color: #fff; border: 2.5px solid ${INK}; font: 16px/19px "KomikBangers", Impact, sans-serif; letter-spacing: .5px; text-align: center; }
    .fab .close { position: absolute; left: -8px; top: -8px; width: 20px; height: 20px; border-radius: 50%; background: #fff; border: 2px solid ${INK};
      font: 700 12px/15px system-ui, sans-serif; color: ${INK}; display: none; }
    .fab:hover .close { display: block; }
    .fab.enter { animation: pop .5s cubic-bezier(.34,1.56,.64,1); }
    @keyframes pop { 0% { transform: scale(0) rotate(-40deg); } 100% { transform: scale(1) rotate(0); } }
    .toast { position: fixed; right: 20px; bottom: 92px; max-width: 320px; background: #FFFDF6; color: ${INK}; border: 3px solid ${INK};
      box-shadow: 5px 5px 0 ${INK}; padding: 12px 16px 12px 14px; border-radius: 4px; font: 600 13px/1.35 system-ui, "Segoe UI", sans-serif;
      transform-origin: bottom right; animation: ${reduced ? "none" : "toastIn .45s cubic-bezier(.34,1.56,.64,1)"}; }
    .toast.out { animation: ${reduced ? "none" : "toastOut .25s ease-in forwards"}; opacity: ${reduced ? 0 : 1}; }
    .toast .sfx { position: absolute; top: -20px; left: -12px; padding: 2px 10px; transform: rotate(-8deg); border: 2.5px solid ${INK};
      font: 22px/1 "KomikBangers", Impact, sans-serif; letter-spacing: 1px; color: #fff; -webkit-text-stroke: 1px ${INK}; box-shadow: 3px 3px 0 ${INK}; }
    .toast b { display: block; font: 19px/1.1 "KomikBangers", Impact, sans-serif; letter-spacing: .6px; margin: 4px 0 2px; }
    @keyframes toastIn { 0% { transform: scale(.3) rotate(8deg); opacity: 0; } 100% { transform: none; opacity: 1; } }
    @keyframes toastOut { to { transform: scale(.6) translateY(20px); opacity: 0; } }
    .bar { position: fixed; left: 50%; top: 14px; transform: translateX(-50%); display: flex; gap: 10px; align-items: center;
      background: ${INK}; color: #fff; border: 3px solid ${INK}; box-shadow: 5px 5px 0 ${YELLOW}; padding: 8px 10px 8px 16px; border-radius: 6px;
      font: 600 13px/1 system-ui, sans-serif; }
    .bar .title { font: 22px/1 "KomikBangers", Impact, sans-serif; letter-spacing: 1px; color: ${YELLOW}; }
    .bar button { font: 17px/1 "KomikBangers", Impact, sans-serif; letter-spacing: .8px; border: 2.5px solid ${INK}; padding: 7px 12px;
      border-radius: 4px; cursor: pointer; box-shadow: 3px 3px 0 #000; }
    .bar button:active { transform: translate(2px,2px); box-shadow: none; }
    .bar .done { background: ${YELLOW}; color: ${INK}; }
    .bar .all { background: ${CYAN}; color: ${INK}; }
    .bar .cancel { background: #fff; color: ${INK}; }
  `;
  shadow.appendChild(style);
  (document.documentElement || document.body).appendChild(host);
  return shadow;
}

export const CREST_SVG = `<svg viewBox="0 0 100 100" aria-hidden="true"><g transform="translate(3.5,4)"><polygon points="14,20 48,28 48,78 14,70" fill="#000" opacity=".85"/><polygon points="52,28 86,20 86,70 52,78" fill="#000" opacity=".85"/></g><polygon points="13,74 50,83 87,74 87,78 50,87 13,78" fill="#16171D" stroke="#000" stroke-width="2.5" stroke-linejoin="round"/><polygon points="14,20 48,28 48,78 14,70" fill="#0E0F14" stroke="#000" stroke-width="3.5" stroke-linejoin="round"/><polygon points="18,24 44,30 44,74 18,67" fill="#FFD700" stroke="#000" stroke-width="2.2" stroke-linejoin="round"/><polygon points="23,34 29,35 29,63 23,62" fill="#000"/><polygon points="29,48 38,37 42,38 32,50" fill="#000"/><polygon points="31,47 42,62 38,63 29,51" fill="#000"/><polygon points="52,28 86,20 86,70 52,78" fill="#FFFDF6" stroke="#000" stroke-width="3.5" stroke-linejoin="round"/><polygon points="56,32 82,26 82,46 56,52" fill="#00C2FF" stroke="#000" stroke-width="2"/><polygon points="56,56 68,53 68,72 56,75" fill="#FF1F6D" stroke="#000" stroke-width="2"/><polygon points="71,52 82,49 82,68 71,71" fill="#FFD700" stroke="#000" stroke-width="2"/><polygon points="47,23 53,24 53,89 50,85 47,89" fill="#FF1F6D" stroke="#000" stroke-width="2"/></svg>`;

const SFX = ["POW!", "ZAP!", "BAM!", "WHAM!", "KAPOW!", "BOOM!"];

export function showToast(title: string, message: string, tone: "yellow" | "cyan" | "magenta" = "yellow"): void {
  const root = ensureShadow();
  root.querySelectorAll(".toast").forEach((t) => t.remove());
  const toast = document.createElement("div");
  toast.className = "toast";
  toast.setAttribute("role", "status");
  const sfx = document.createElement("span");
  sfx.className = "sfx";
  sfx.textContent = SFX[Math.floor(Math.random() * SFX.length)];
  sfx.style.background = tone === "cyan" ? CYAN : tone === "magenta" ? MAGENTA : YELLOW;
  const b = document.createElement("b");
  b.textContent = title;
  const p = document.createElement("div");
  p.textContent = message;
  toast.append(sfx, b, p);
  root.appendChild(toast);
  setTimeout(() => {
    toast.classList.add("out");
    setTimeout(() => toast.remove(), 300);
  }, 4200);
}
