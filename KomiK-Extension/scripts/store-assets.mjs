// Builds the Chrome Web Store listing images into release/store/ from the real extension:
//   icon-128.png                 store icon (96px artwork inside 16px of transparent padding)
//   screenshot-1..5.png          1280x800 screenshots of the popup, details, chapters, history and settings
//   promo-small-440x280.png      small promo tile
//   promo-marquee-1400x560.png   marquee promo tile
// The comic shown is a made-up showcase with generated art, so the listing contains no real site or artwork.
// Run `npm run build` first (npm run store does both).
import { chromium } from "@playwright/test";
import sharp from "sharp";
import { spawn, spawnSync } from "node:child_process";
import { createServer } from "node:http";
import { createServer as createNetServer } from "node:net";
import { existsSync, mkdirSync, rmSync, writeFileSync } from "node:fs";
import { readFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const dist = join(root, "dist");
const out = join(root, "release", "store");
if (!existsSync(join(dist, "manifest.json"))) throw new Error("Run npm run build first.");
mkdirSync(out, { recursive: true });

// ── Showcase comic ──────────────────────────────────────────────────────────────────
const PALETTES = [
  ["#FFD700", "#FF1F6D", "#00C2FF"],
  ["#00C2FF", "#FFD700", "#FF7A00"],
  ["#FF7A00", "#2FD17A", "#FFD700"],
  ["#A78BFA", "#FFD700", "#FF1F6D"],
  ["#2FD17A", "#FF1F6D", "#00C2FF"],
  ["#FF1F6D", "#00C2FF", "#FFD700"],
  ["#FFD700", "#A78BFA", "#2FD17A"],
  ["#00C2FF", "#FF7A00", "#A78BFA"]
];
const SFX = ["ZOOM!", "KRAK!", "WHAM!", "POW!", "VRRM!", "BOOM!", "ZAP!", "SKREE!"];
const LINES = ["Storm's coming.", "Hold on tight!", "Not today!", "Deliver it. Always.", "Is that... a comet?", "We're almost there!", "Go, go, GO!", "Package secured."];

/** One comic page as SVG: three inked panels with halftone, a figure, a speech bubble and a sound effect. */
function pageSvg(n) {
  const [a, b, c] = PALETTES[(n - 1) % PALETTES.length];
  const dots = `<pattern id="d${n}" width="14" height="14" patternUnits="userSpaceOnUse"><circle cx="7" cy="7" r="3" fill="#000" opacity=".18"/></pattern>`;
  const rays = Array.from({ length: 18 }, (_, i) => {
    const t = (i / 18) * Math.PI * 2;
    return `<line x1="400" y1="300" x2="${400 + Math.cos(t) * 700}" y2="${300 + Math.sin(t) * 700}" stroke="#fff" stroke-width="10" opacity=".25"/>`;
  }).join("");
  const burst = Array.from({ length: 24 }, (_, i) => {
    const t = (i / 24) * Math.PI * 2;
    const r = i % 2 ? 70 : 120;
    return `${560 + Math.cos(t) * r},${820 + Math.sin(t) * r}`;
  }).join(" ");
  return `<svg xmlns="http://www.w3.org/2000/svg" width="800" height="1200" viewBox="0 0 800 1200">
  <defs>${dots}<clipPath id="p1"><rect x="30" y="30" width="740" height="520"/></clipPath></defs>
  <rect width="800" height="1200" fill="#fff"/>
  <g clip-path="url(#p1)"><rect x="30" y="30" width="740" height="520" fill="${a}"/>${rays}<rect x="30" y="30" width="740" height="520" fill="url(#d${n})"/>
    <circle cx="${300 + n * 20}" cy="330" r="120" fill="${b}" stroke="#000" stroke-width="10"/>
    <circle cx="${270 + n * 20}" cy="300" r="16" fill="#000"/><circle cx="${340 + n * 20}" cy="300" r="16" fill="#000"/>
    <path d="M${250 + n * 20} 380 Q ${305 + n * 20} 420 ${360 + n * 20} 380" stroke="#000" stroke-width="10" fill="none"/></g>
  <rect x="30" y="30" width="740" height="520" fill="none" stroke="#000" stroke-width="12"/>
  <path d="M470 70 h270 a30 30 0 0 1 30 30 v70 a30 30 0 0 1 -30 30 h-190 l-50 50 l10 -50 h-40 a30 30 0 0 1 -30 -30 v-70 a30 30 0 0 1 30 -30z" fill="#fff" stroke="#000" stroke-width="8"/>
  <text x="605" y="148" font-family="Comic Sans MS, Arial" font-weight="700" font-size="30" text-anchor="middle">${LINES[(n - 1) % LINES.length]}</text>
  <rect x="30" y="580" width="350" height="590" fill="${c}"/><rect x="30" y="580" width="350" height="590" fill="url(#d${n})"/>
  <polygon points="80,1120 205,700 330,1120" fill="${a}" stroke="#000" stroke-width="10"/>
  <rect x="30" y="580" width="350" height="590" fill="none" stroke="#000" stroke-width="12"/>
  <rect x="410" y="580" width="360" height="590" fill="${b}"/>
  <polygon points="${burst}" fill="#FFD700" stroke="#000" stroke-width="8"/>
  <text x="560" y="845" font-family="Impact, Arial Black, sans-serif" font-size="64" text-anchor="middle" fill="#FF1F6D" stroke="#000" stroke-width="3" transform="rotate(-8 560 830)">${SFX[(n - 1) % SFX.length]}</text>
  <rect x="410" y="580" width="360" height="590" fill="none" stroke="#000" stroke-width="12"/>
</svg>`;
}

const TAGS = ["Action", "Sci-Fi", "Adventure", "Space Opera", "Couriers", "Found Family", "Comedy", "Drama", "Robots", "Heist", "Slow Burn", "Full Color"];
const siteChrome = (title, body) => `<!doctype html><html lang="en"><head><meta charset="utf-8"><title>${title}</title>
<meta property="og:site_name" content="Panel House">
<style>
  body{margin:0;background:#15151c;color:#eee;font-family:Segoe UI,Arial,sans-serif}
  header{display:flex;align-items:center;gap:18px;padding:14px 28px;background:#0c0c12;border-bottom:3px solid #FFD700}
  header b{font:900 26px Impact,Arial Black;letter-spacing:1px;color:#FFD700}
  header nav a{color:#bbb;margin-right:14px;text-decoration:none}
  main{max-width:860px;margin:0 auto;padding:24px}
  h1{font:900 38px Impact,Arial Black;margin:0 0 10px;letter-spacing:.5px}
  .info{display:grid;grid-template-columns:120px 1fr;gap:6px 14px;background:#1f1f29;padding:16px;border-radius:10px;margin-bottom:22px}
  .info a{color:#00C2FF;text-decoration:none;margin-right:8px}
  .tags a{display:inline-block;background:#2b2b38;color:#ffd;padding:3px 10px;border-radius:12px;margin:2px}
  .reading-area img{display:block;width:100%;margin:0 0 6px}
  ul.chapter-list{list-style:none;padding:0}
  ul.chapter-list li{background:#1f1f29;margin:6px 0;padding:12px 16px;border-radius:8px;display:flex;justify-content:space-between}
  ul.chapter-list a{color:#fff;text-decoration:none;font-weight:600}
</style></head><body><header><b>PANEL HOUSE</b><nav><a href="#">Latest</a><a href="#">Genres</a><a href="#">Popular</a></nav></header><main>${body}</main></body></html>`;

const info = `<div class="info">
  <span>Author</span><span><a href="/author/ada-ink">Ada Ink</a></span>
  <span>Artist</span><span><a href="/artist/rio-pen">Rio Pen</a></span>
  <span>Publisher</span><span>Orbit Ink</span>
  <span>Status</span><span>Ongoing</span>
  <span>Uploaded</span><span>2 years 3 months ago</span>
  <span>Tags</span><span class="tags">${TAGS.map((t) => `<a href="/tag/${t.toLowerCase().replace(/ /g, "-")}">${t}</a>`).join("")}</span>
</div>`;

const chapterPage = () =>
  siteChrome(
    "Starlight Courier Chapter 15 - Panel House",
    `<h1>Starlight Courier — Chapter 15</h1>${info}
     <p style="color:#aaa">A courier races a solar storm to deliver the one package that can save her station.</p>
     <div class="reading-area">${Array.from({ length: 8 }, (_, i) => `<img src="/pages/sc15/${String(i + 1).padStart(3, "0")}.jpg" width="800" height="1200" alt="Page ${i + 1}">`).join("")}</div>`
  );

const seriesPage = () =>
  siteChrome(
    "Starlight Courier - Panel House",
    `<h1>Starlight Courier</h1>${info}
     <p style="color:#aaa">A courier races a solar storm to deliver the one package that can save her station.</p>
     <ul class="chapter-list">${Array.from({ length: 15 }, (_, i) => 15 - i).map((n) => `<li><a href="/starlight-courier/chapter-${n}/">Chapter ${n}</a><span>${n} days ago</span></li>`).join("")}</ul>`
  );

// Real comic pages are photos of art (JPEG), so the showcase pages are rendered to JPEG as well.
const pageImages = await Promise.all(
  PALETTES.map((_, i) => sharp(Buffer.from(pageSvg(i + 1))).jpeg({ quality: 88 }).toBuffer())
);

function startShowcase() {
  return new Promise((done) => {
    const server = createServer((req, res) => {
      const p = new URL(req.url, "http://x").pathname;
      const send = (type, body) => {
        res.writeHead(200, { "content-type": type, "cache-control": "no-store" });
        res.end(body);
      };
      if (p === "/starlight-courier/chapter-15/") return send("text/html", chapterPage());
      if (p === "/starlight-courier/") return send("text/html", seriesPage());
      const art = /^\/pages\/sc15\/0*(\d+)\.jpg$/.exec(p);
      if (art && pageImages[Number(art[1]) - 1]) return send("image/jpeg", pageImages[Number(art[1]) - 1]);
      res.writeHead(404).end();
    });
    server.listen(0, "127.0.0.1", () => done({ server, origin: `http://127.0.0.1:${server.address().port}` }));
  });
}

// ── Browser with the extension ──────────────────────────────────────────────────────
const freePort = () =>
  new Promise((ok) => {
    const s = createNetServer();
    s.listen(0, "127.0.0.1", () => {
      const port = s.address().port;
      s.close(() => ok(port));
    });
  });

const tmp = join(tmpdir(), `komik-store-${Date.now()}`);
mkdirSync(join(tmp, "profile", "Default"), { recursive: true });
mkdirSync(join(tmp, "Downloads"), { recursive: true });
writeFileSync(join(tmp, "profile", "Default", "Preferences"), JSON.stringify({ download: { default_directory: join(tmp, "Downloads"), prompt_for_download: false } }));

const { server, origin } = await startShowcase();
const port = await freePort();
const chrome = spawn(
  chromium.executablePath(),
  [`--user-data-dir=${join(tmp, "profile")}`, `--remote-debugging-port=${port}`, `--disable-extensions-except=${dist}`, `--load-extension=${dist}`, "--headless=new", "--no-first-run", "--hide-scrollbars", "about:blank"],
  { stdio: "ignore" }
);

let browser;
try {
  for (let i = 0; i < 60 && !browser; i++) {
    browser = await chromium.connectOverCDP(`http://127.0.0.1:${port}`).catch(() => undefined);
    if (!browser) await new Promise((r) => setTimeout(r, 250));
  }
  const context = browser.contexts()[0];
  const session = await browser.newBrowserCDPSession();
  await session.send("Browser.setDownloadBehavior", { behavior: "default" });

  let extensionId = "";
  for (let i = 0; i < 80 && !extensionId; i++) {
    for (const worker of context.serviceWorkers()) {
      if (!worker.url().startsWith("chrome-extension://")) continue;
      const name = await worker.evaluate(() => chrome.runtime.getManifest().name).catch(() => "");
      if (name === "KomiK Downloader") extensionId = new URL(worker.url()).host;
    }
    if (!extensionId) await new Promise((r) => setTimeout(r, 250));
  }
  if (!extensionId) throw new Error("KomiK Downloader did not start.");
  await new Promise((r) => setTimeout(r, 1500));
  for (const p of context.pages()) if (p.url().includes("options.html")) await p.close();

  const ext = async (path, size) => {
    const page = await context.newPage();
    if (size) await page.setViewportSize(size);
    await page.goto(`chrome-extension://${extensionId}/${path}`);
    return page;
  };
  const tabIdFor = (page, url) => page.evaluate(async (u) => (await chrome.tabs.query({})).find((t) => t.url === u).id, url);
  const message = (page, type, payload) => page.evaluate(({ type, payload }) => chrome.runtime.sendMessage({ komik: true, type, payload, target: "background" }), { type, payload });

  const shots = {};
  const grab = async (page, name, opts = {}) => {
    shots[name] = await page.screenshot({ type: "png", ...opts });
  };

  // The comic pages behind the popup.
  const chapterUrl = `${origin}/starlight-courier/chapter-15/`;
  const comic = await context.newPage();
  await comic.setViewportSize({ width: 1280, height: 800 });
  await comic.goto(chapterUrl);
  await comic.waitForTimeout(800);
  await comic.evaluate(() => window.scrollTo(0, 360));
  await comic.waitForTimeout(500);
  await grab(comic, "site-chapter");

  const helper = await ext("options.html", { width: 800, height: 600 });
  const chapterTab = await tabIdFor(helper, chapterUrl);

  const popup = await ext(`popup.html?tabId=${chapterTab}`, { width: 430, height: 600 });
  await popup.getByText("PERFECT MATCH!").waitFor({ timeout: 30_000 });
  await popup.waitForTimeout(1500);
  await grab(popup, "popup-pages");
  await popup.getByRole("tab", { name: /Details/ }).click();
  await popup.waitForTimeout(900);
  await grab(popup, "popup-details");
  await popup.close();

  // A finished download for the history list.
  const scan = await message(helper, "scan-tab", { tabId: chapterTab, force: true });
  const { ids } = await message(helper, "queue-jobs", { jobs: [{ meta: scan.meta, format: "cbz", pages: scan.pages, coverIndex: 0, sourceUrl: chapterUrl, tabId: chapterTab }] });
  for (let i = 0; i < 90; i++) {
    const snap = await message(helper, "jobs-snapshot", {});
    const job = snap.jobs.find((j) => ids.includes(j.id));
    if (job?.status === "done") break;
    if (job?.status === "error") throw new Error(job.error);
    await new Promise((r) => setTimeout(r, 1000));
  }

  const seriesUrl = `${origin}/starlight-courier/`;
  const series = await context.newPage();
  await series.setViewportSize({ width: 1280, height: 800 });
  await series.goto(seriesUrl);
  await series.waitForTimeout(700);
  await grab(series, "site-series");
  const seriesTab = await tabIdFor(helper, seriesUrl);
  const seriesPopup = await ext(`popup.html?tabId=${seriesTab}`, { width: 430, height: 600 });
  await seriesPopup.getByRole("tab", { name: /Chapters/ }).waitFor({ timeout: 30_000 });
  await seriesPopup.waitForTimeout(900);
  await seriesPopup.getByRole("tab", { name: /Chapters/ }).click();
  await seriesPopup.waitForTimeout(700);
  // Pick a few chapters so the download button shows what it will do.
  for (const n of [13, 14, 15]) await seriesPopup.locator("button", { hasText: new RegExp(`Chapter ${n}(?!\\d)`) }).first().click();
  await seriesPopup.waitForTimeout(500);
  await grab(seriesPopup, "popup-chapters");
  await seriesPopup.close();

  const panel = await ext("sidepanel.html", { width: 400, height: 780 });
  await panel.getByRole("tab", { name: /History/ }).click();
  await panel.getByText("Starlight Courier Ch. 15").first().waitFor({ timeout: 15_000 });
  await panel.waitForTimeout(900);
  await grab(panel, "panel-history");
  await panel.close();

  const options = await ext("options.html", { width: 1280, height: 800 });
  await options.waitForTimeout(1200);
  // Scroll the chapter heading clear of the sticky top bar.
  await options.evaluate(() => {
    const heading = document.querySelector("#metadata");
    if (heading) window.scrollTo(0, heading.getBoundingClientRect().top + window.scrollY - 96);
  });
  await options.waitForTimeout(900);
  await grab(options, "options");
  await options.close();

  // ── Compose the listing images ─────────────────────────────────────────────────────
  const b64 = (buf) => `data:image/png;base64,${buf.toString("base64")}`;
  const bangers = `data:font/woff2;base64,${(await readFile(join(dist, "fonts", "Bangers-Regular.woff2"))).toString("base64")}`;
  // The full-size crest, so the large marquee tile stays sharp.
  const crestPng = await sharp(join(root, "..", "Assets", "app-icon.png")).trim({ threshold: 1 }).resize(640, 640, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } }).png().toBuffer();
  const crest = `data:image/png;base64,${crestPng.toString("base64")}`;
  const baseCss = `@font-face{font-family:Bangers;src:url(${bangers}) format("woff2")}
    *{box-sizing:border-box}body{margin:0;overflow:hidden;font-family:Segoe UI,Arial,sans-serif}
    .headline{font-family:Bangers;letter-spacing:1.5px;color:#fff;-webkit-text-stroke:2px #08080A;paint-order:stroke fill;text-shadow:4px 4px 0 #08080A;line-height:.95}
    .sticker{display:inline-block;background:#FFD700;color:#08080A;border:3px solid #08080A;box-shadow:4px 4px 0 #08080A;padding:4px 14px;font-family:Bangers;letter-spacing:1px;transform:rotate(-2deg)}
    .ui{border:4px solid #08080A;border-radius:10px;box-shadow:10px 10px 0 #08080A;display:block}`;

  const frame = await context.newPage();
  const render = async (html, size, name) => {
    await frame.setViewportSize(size);
    await frame.setContent(`<!doctype html><html><head><style>${baseCss}</style></head><body>${html}</body></html>`);
    await frame.evaluate(() => document.fonts.ready);
    await frame.waitForTimeout(300);
    writeFileSync(join(out, name), await frame.screenshot({ type: "png" }));
    console.log(`  ${name}`);
  };

  /** A browser page with the extension UI floating over it, and a headline on the left. */
  const overSite = (site, ui, uiWidth, headline, sub, sticker) => `
    <div style="position:relative;width:1280px;height:800px;background:#15151c">
      <img src="${b64(site)}" style="position:absolute;inset:0;width:1280px;height:800px;filter:brightness(.45) saturate(.8)">
      <div style="position:absolute;left:0;top:0;bottom:0;width:640px;background:linear-gradient(90deg,rgba(8,8,10,.92),rgba(8,8,10,.55) 75%,transparent)"></div>
      <div style="position:absolute;left:64px;top:190px;width:560px">
        <span class="sticker" style="font-size:26px">${sticker}</span>
        <div class="headline" style="font-size:84px;margin-top:22px">${headline}</div>
        <p style="color:#e8e8ee;font-size:25px;line-height:1.4;margin-top:22px">${sub}</p>
      </div>
      <img class="ui" src="${b64(ui)}" style="position:absolute;right:70px;top:50%;transform:translateY(-50%);width:${uiWidth}px">
    </div>`;

  console.log("Writing store images to release/store:");
  await render(overSite(shots["site-chapter"], shots["popup-pages"], 470, "ANY COMIC.<br>ONE CLICK.", "Finds every page of the chapter you're reading — lazy-loaded, full-size and in order.", "COMICS · MANGA · WEBTOONS"), { width: 1280, height: 800 }, "screenshot-1.png");
  await render(overSite(shots["site-chapter"], shots["popup-details"], 470, "TITLES, CREDITS<br>AND EVERY TAG", "Details and all the site's tags are written into the CBZ as ComicInfo.xml, ready for the Komik reader.", "METADATA BUILT IN"), { width: 1280, height: 800 }, "screenshot-2.png");
  await render(overSite(shots["site-series"], shots["popup-chapters"], 470, "WHOLE SERIES,<br>CHAPTER BY CHAPTER", "Pick the chapters you want from a series page and KomiK saves each one as its own comic.", "BATCH DOWNLOADS"), { width: 1280, height: 800 }, "screenshot-3.png");
  await render(overSite(shots["site-chapter"], shots["panel-history"], 400, "FAST, SAFE<br>AND RESUMABLE", "Parallel downloads with retries. Unfinished comics pick up where they stopped, even after a restart.", "DOWNLOAD MANAGER"), { width: 1280, height: 800 }, "screenshot-4.png");
  await render(`<img src="${b64(shots["options"])}" style="display:block;width:1280px;height:800px">`, { width: 1280, height: 800 }, "screenshot-5.png");

  const tile = (w, h, crestSize, titleSize, tagSize) => `
    <div style="position:relative;width:${w}px;height:${h}px;overflow:hidden;background:#FFD700">
      <div style="position:absolute;inset:-50%;background:repeating-conic-gradient(from 0deg at 50% 50%,rgba(255,255,255,.35) 0 9deg,transparent 9deg 18deg)"></div>
      <div style="position:absolute;inset:0;background-image:radial-gradient(rgba(8,8,10,.14) 22%,transparent 24%);background-size:14px 14px"></div>
      <div style="position:relative;display:flex;align-items:center;justify-content:center;gap:${Math.round(w * 0.03)}px;height:100%">
        <img src="${crest}" style="width:${crestSize}px;height:${crestSize}px;filter:drop-shadow(5px 5px 0 #08080A)">
        <div>
          <div class="headline" style="font-size:${titleSize}px">KOMIK</div>
          <div class="sticker" style="font-size:${tagSize}px;margin-top:8px;background:#FF1F6D;color:#fff;-webkit-text-stroke:1px #08080A">DOWNLOADER</div>
          <div style="font-family:Bangers;letter-spacing:1px;font-size:${Math.round(tagSize * 0.8)}px;color:#08080A;margin-top:${Math.round(tagSize * 0.5)}px">Comics to CBZ · tags included</div>
        </div>
      </div>
    </div>`;
  await render(tile(440, 280, 132, 76, 24), { width: 440, height: 280 }, "promo-small-440x280.png");
  await render(tile(1400, 560, 300, 180, 52), { width: 1400, height: 560 }, "promo-marquee-1400x560.png");
} finally {
  if (chrome.pid) spawnSync("taskkill", ["/PID", String(chrome.pid), "/T", "/F"], { stdio: "ignore" });
  await browser?.close().catch(() => undefined);
  server.close();
  try {
    rmSync(tmp, { recursive: true, force: true });
  } catch {
    /* the browser may hold the profile for a moment */
  }
}

// Store icon: the guidelines ask for 96x96 artwork centred in a 128x128 image.
const trimmed = await sharp(join(root, "..", "Assets", "app-icon.png")).trim({ threshold: 1 }).toBuffer();
const art = await sharp(trimmed).resize(96, 96, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } }).toBuffer();
await sharp({ create: { width: 128, height: 128, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } })
  .composite([{ input: art, gravity: "center" }])
  .png({ compressionLevel: 9 })
  .toFile(join(out, "icon-128.png"));
console.log("  icon-128.png");
