// End-to-end: loads the built extension into Chromium, scans local fixture comic pages and checks the
// real files written to the Downloads folder (name, folder, pages and ComicInfo.xml).
import { chromium, expect, test, type Browser, type BrowserContext, type Page } from "@playwright/test";
import { spawn, spawnSync, type ChildProcess } from "node:child_process";
import { existsSync, mkdirSync, readdirSync, readFileSync, rmSync, statSync, writeFileSync } from "node:fs";
import { createServer as createNetServer } from "node:net";
import { basename, dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { unzipSync, strFromU8 } from "fflate";
import { startFixtureServer } from "./fixture-server.mjs";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const DIST = join(ROOT, "dist");
const TMP = join(ROOT, "tests", ".tmp", `e2e-${Date.now()}`);
const DOWNLOADS = join(TMP, "Downloads");
const SHOTS = join(ROOT, "test-results", "screens");

let chrome: ChildProcess;
let browser: Browser;
let context: BrowserContext;
let extensionId = "";
let origin = "";
let debugPort = 0;
let closeServer: () => void;

function freePort(): Promise<number> {
  return new Promise((res) => {
    const srv = createNetServer();
    srv.listen(0, "127.0.0.1", () => {
      const port = (srv.address() as { port: number }).port;
      srv.close(() => res(port));
    });
  });
}

function walk(dir: string): string[] {
  if (!existsSync(dir)) return [];
  return readdirSync(dir).flatMap((name) => {
    const full = join(dir, name);
    return statSync(full).isDirectory() ? walk(full) : [full];
  });
}

async function extPage(path = "options.html"): Promise<Page> {
  const page = await context.newPage();
  await page.goto(`chrome-extension://${extensionId}/${path}`);
  return page;
}

async function tabIdFor(ext: Page, url: string): Promise<number> {
  return ext.evaluate(async (u) => (await chrome.tabs.query({})).find((t) => t.url === u)!.id!, url);
}

async function message<T>(ext: Page, type: string, payload: unknown): Promise<T> {
  return ext.evaluate(({ type, payload }) => chrome.runtime.sendMessage({ komik: true, type, payload, target: "background" }), { type, payload }) as Promise<T>;
}

async function waitForJobs(ext: Page, ids: string[], timeout = 40_000) {
  const started = Date.now();
  for (;;) {
    const snap = await message<{ jobs: Array<{ id: string; status: string; error?: string; savedPath?: string }> }>(ext, "jobs-snapshot", {});
    const mine = snap.jobs.filter((j) => ids.includes(j.id));
    const failed = mine.find((j) => j.status === "error");
    if (failed) throw new Error(`Job failed: ${failed.error}`);
    if (mine.length === ids.length && mine.every((j) => j.status === "done")) return mine;
    if (Date.now() - started > timeout) {
      const downloads = await ext.evaluate(async () => (await chrome.downloads.search({})).map((d) => [d.state, d.error, d.filename, d.url.slice(0, 50)]));
      throw new Error(`Timed out: ${JSON.stringify(mine.map((j) => [j.status, j.error]))}; downloads: ${JSON.stringify(downloads)}`);
    }
    await new Promise((r) => setTimeout(r, 1000));
  }
}

function emptyMetaForTest() {
  return { title: "", series: "", number: "", volume: "", chapterTitle: "", summary: "", year: "", month: "", day: "", writers: [], artists: [], publisher: "", genres: [], tags: [], language: "", manga: "", ageRating: "", status: "", web: "", site: "", coverUrl: "", numberKind: "none" };
}

function readCbz(path: string) {
  const files = unzipSync(new Uint8Array(readFileSync(path)));
  return { names: Object.keys(files), xml: strFromU8(files["ComicInfo.xml"] ?? new Uint8Array()) };
}

test.beforeAll(async () => {
  expect(existsSync(join(DIST, "manifest.json")), "run npm run build first").toBe(true);
  mkdirSync(join(TMP, "profile", "Default"), { recursive: true });
  mkdirSync(DOWNLOADS, { recursive: true });
  mkdirSync(SHOTS, { recursive: true });
  writeFileSync(join(TMP, "profile", "Default", "Preferences"), JSON.stringify({ download: { default_directory: DOWNLOADS, prompt_for_download: false } }));

  const fixture = await startFixtureServer();
  origin = fixture.origin;
  closeServer = () => fixture.server.close();

  const port = await freePort();
  debugPort = port;
  chrome = spawn(chromium.executablePath(), [
    `--user-data-dir=${join(TMP, "profile")}`,
    `--remote-debugging-port=${port}`,
    `--disable-extensions-except=${DIST}`,
    `--load-extension=${DIST}`,
    "--headless=new",
    "--no-first-run",
    "about:blank"
  ], { stdio: "ignore" });

  for (let i = 0; i < 60; i++) {
    try {
      browser = await chromium.connectOverCDP(`http://127.0.0.1:${port}`);
      break;
    } catch {
      await new Promise((r) => setTimeout(r, 250));
    }
  }
  context = browser.contexts()[0];
  // Let Chromium save downloads itself (Playwright otherwise renames or denies them).
  const session = await browser.newBrowserCDPSession();
  // Keep this session attached: Chromium resets the download behaviour when it detaches.
  await session.send("Browser.setDownloadBehavior", { behavior: "default" });
  // Chromium runs hidden component extensions with a background.js too: find ours by its manifest name.
  for (let i = 0; i < 80 && !extensionId; i++) {
    for (const worker of context.serviceWorkers()) {
      if (!worker.url().startsWith("chrome-extension://")) continue;
      const name = await worker.evaluate(() => chrome.runtime.getManifest().name).catch(() => "");
      if (name === "KomiK Downloader") extensionId = new URL(worker.url()).host;
    }
    if (!extensionId) await new Promise((r) => setTimeout(r, 250));
  }
  expect(extensionId, "KomiK Downloader service worker did not start").not.toBe("");
  // Give the freshly installed extension a moment to finish onInstalled (settings, menus, welcome tab).
  await new Promise((r) => setTimeout(r, 1500));
});

test.afterAll(async () => {
  // Kill the whole Chromium process tree first (closing the browser orphans its GPU/renderer processes).
  if (chrome?.pid) spawnSync("taskkill", ["/PID", String(chrome.pid), "/T", "/F"], { stdio: "ignore" });
  // Fallback: anything still running with this run's profile (Chromium can re-parent helper processes).
  const profileTag = basename(TMP);
  spawnSync("powershell", ["-NoProfile", "-Command", `Get-CimInstance Win32_Process -Filter "Name='chrome.exe'" | Where-Object { $_.CommandLine -like "*${profileTag}*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }`], { stdio: "ignore" });
  await browser?.close().catch(() => undefined);
  closeServer?.();
  await new Promise((r) => setTimeout(r, 500));
  try {
    rmSync(TMP, { recursive: true, force: true });
  } catch {
    /* Chrome may still hold the profile briefly */
  }
});

test("lazy reader: scans with auto-scroll and saves a tagged CBZ named by its title", async () => {
  const comic = await context.newPage();
  await comic.goto(`${origin}/chapter/lazy`);
  const ext = await extPage();
  const tabId = await tabIdFor(ext, `${origin}/chapter/lazy`);

  const result = await message<{ pages: Array<{ url: string }>; meta: Record<string, unknown>; confidence: string }>(ext, "scan-tab", { tabId, force: true });
  expect(result.pages.map((p) => p.url)).toEqual(Array.from({ length: 6 }, (_, i) => `${origin}/img/sc15/${String(i + 1).padStart(3, "0")}.png`));
  expect(result.meta).toMatchObject({ series: "Starlight Courier", number: "15", title: "Starlight Courier Ch. 15", publisher: "Orbit Ink", year: "2024", summary: "A courier races a solar storm." });
  expect(result.meta.writers).toEqual(["Ada Ink"]);

  // Screenshot the popup UI for this page.
  const popup = await extPage(`popup.html?tabId=${tabId}`);
  await popup.setViewportSize({ width: 430, height: 600 });
  await expect(popup.getByText("PERFECT MATCH!")).toBeVisible({ timeout: 20_000 });
  await popup.waitForTimeout(900);
  await popup.screenshot({ path: join(SHOTS, "popup-pages.png") });
  await popup.getByRole("tab", { name: /Details/ }).click();
  await popup.waitForTimeout(500);
  await popup.screenshot({ path: join(SHOTS, "popup-details.png") });
  await popup.close();

  const { ids } = await message<{ ids: string[] }>(ext, "queue-jobs", { jobs: [{ meta: result.meta, format: "cbz", pages: result.pages, coverIndex: 0, sourceUrl: `${origin}/chapter/lazy`, tabId }] });
  await waitForJobs(ext, ids);

  const file = join(DOWNLOADS, "KomiK", "Starlight Courier", "Starlight Courier Ch. 15.cbz");
  expect(existsSync(file), `expected ${file}; found ${walk(DOWNLOADS).join(", ")}`).toBe(true);
  const { names, xml } = readCbz(file);
  expect(names).toEqual(["001.png", "002.png", "003.png", "004.png", "005.png", "006.png", "ComicInfo.xml"]);
  expect(xml).toContain("<Title>Starlight Courier Ch. 15</Title>");
  expect(xml).toContain("<Series>Starlight Courier</Series>");
  expect(xml).toContain("<Number>15</Number>");
  expect(xml).toContain("<Writer>Ada Ink</Writer>");
  expect(xml).toContain("<Penciller>Rio Pen</Penciller>");
  expect(xml).toContain("<Publisher>Orbit Ink</Publisher>");
  expect(xml).toContain("<Genre>Action, Sci-Fi</Genre>");
  expect(xml).toMatch(/<Tags>[^<]*Space[^<]*<\/Tags>/);
  expect(xml).not.toContain("Home");
  expect(xml).toContain('<Page Image="0" Type="FrontCover" ImageSize="');
  expect(xml).toContain('ImageWidth="800" ImageHeight="1200"');
  await comic.close();
  await ext.close();
});

test("series page: batch-downloads chapters resolved in the background", async () => {
  const comic = await context.newPage();
  await comic.goto(`${origin}/paper-moon/`);
  const ext = await extPage();
  const tabId = await tabIdFor(ext, `${origin}/paper-moon/`);
  const result = await message<{ chapters: Array<{ url: string; number: number }>; meta: { title: string; genres: string[] }; isSeriesPage: boolean; adapter: string }>(ext, "scan-tab", { tabId, force: true });
  expect(result.isSeriesPage).toBe(true);
  expect(result.adapter).toBe("WP Manga reader theme");
  expect(result.chapters.map((c) => c.number)).toEqual([1, 2, 3]);
  expect(result.meta.title).toBe("Paper Moon");

  const panel = await extPage(`sidepanel.html?tabId=${tabId}#page`);
  await panel.setViewportSize({ width: 400, height: 820 });
  await expect(panel.getByRole("tab", { name: /Chapters/ })).toBeVisible({ timeout: 20_000 });
  await panel.waitForTimeout(700);
  await panel.screenshot({ path: join(SHOTS, "sidepanel-chapters.png") });

  const { ids } = await message<{ ids: string[] }>(ext, "queue-chapters", { base: result, meta: result.meta, chapters: result.chapters.slice(0, 2), format: "cbz", tabId });
  expect(ids).toHaveLength(2);
  await panel.getByRole("tab", { name: /Queue/ }).click();
  await waitForJobs(ext, ids);
  await panel.waitForTimeout(600);
  await panel.screenshot({ path: join(SHOTS, "sidepanel-queue.png") });

  for (const n of [1, 2]) {
    const file = join(DOWNLOADS, "KomiK", "Paper Moon", `Paper Moon Ch. ${n}.cbz`);
    expect(existsSync(file), `expected ${file}; found ${walk(DOWNLOADS).join(", ")}`).toBe(true);
    const { names, xml } = readCbz(file);
    expect(names.filter((x) => x.endsWith(".png"))).toHaveLength(3 + n);
    expect(xml).toContain(`<Number>${n}</Number>`);
    expect(xml).toContain("<Writer>Mo Lee</Writer>");
    expect(xml).toContain("<Genre>Drama, Manhwa</Genre>");
    expect(xml).toContain("<Manga>Yes</Manga>");
  }
  await panel.close();
  await comic.close();
  await ext.close();
});

test("hotlink-protected images download with the reader page as Referer", async () => {
  const url = `${origin}/hotlink/iron-orchard-7`;
  const comic = await context.newPage();
  await comic.goto(url);
  const ext = await extPage();
  const tabId = await tabIdFor(ext, url);
  const result = await message<{ pages: Array<{ url: string }>; meta: { title: string } }>(ext, "scan-tab", { tabId, force: true });
  expect(result.pages).toHaveLength(4);
  expect(result.meta.title).toBe("Iron Orchard #7");
  const { ids } = await message<{ ids: string[] }>(ext, "queue-jobs", { jobs: [{ meta: result.meta, format: "cbz", pages: result.pages, sourceUrl: url, tabId }] });
  await waitForJobs(ext, ids);
  const file = join(DOWNLOADS, "KomiK", "Iron Orchard", "Iron Orchard #7.cbz");
  expect(existsSync(file), `found ${walk(DOWNLOADS).join(", ")}`).toBe(true);
  expect(readCbz(file).names.filter((x) => x.endsWith(".png"))).toHaveLength(4);
  await comic.close();
  await ext.close();
});

test("gallery of previews: every page saved at full size, with the reader page as fallback", async () => {
  const url = `${origin}/g/9912/`;
  const comic = await context.newPage();
  await comic.goto(url);
  const ext = await extPage();
  const tabId = await tabIdFor(ext, url);

  const result = await message<{ pages: Array<{ url: string; candidates?: string[]; pageUrl?: string }>; meta: Record<string, unknown>; adapter: string; confidence: string }>(ext, "scan-tab", { tabId, force: true });
  expect(result.pages).toHaveLength(9);
  expect(result.confidence).toBe("high");
  // The small preview is never the first choice, and each page keeps its own reader page.
  expect(result.pages[0].candidates?.[0]).toContain("/images/9912/1.jpg");
  expect(result.pages[0].pageUrl).toBe(`${origin}/g/9912/1/`);

  // The page grid shows the site's own previews, so it fills in immediately.
  const popup = await extPage(`popup.html?tabId=${tabId}`);
  await popup.setViewportSize({ width: 430, height: 600 });
  await expect(popup.locator('img[src*="/thumbs/9912/"]').first()).toBeVisible({ timeout: 20_000 });
  expect(await popup.locator('img[src*="/thumbs/9912/"]').count()).toBeGreaterThanOrEqual(8);
  await popup.waitForTimeout(600);
  await popup.screenshot({ path: join(SHOTS, "popup-gallery.png") });
  await popup.close();

  const meta = { ...result.meta, title: "Night Market Stories", series: "Night Market Stories" };
  const { ids } = await message<{ ids: string[] }>(ext, "queue-jobs", { jobs: [{ meta, format: "cbz", pages: result.pages, sourceUrl: url, tabId }] });
  await waitForJobs(ext, ids);

  const file = join(DOWNLOADS, "KomiK", "Night Market Stories", "Night Market Stories.cbz");
  expect(existsSync(file), `found ${walk(DOWNLOADS).join(", ")}`).toBe(true);
  const { names, xml } = readCbz(file);
  expect(names.filter((n) => n.endsWith(".png"))).toHaveLength(9);
  // Every page is the 1200px scan, including page 5, whose full image only its reader page knows about.
  const widths = Array.from(xml.matchAll(/ImageWidth="(\d+)"/g)).map((m) => Number(m[1]));
  expect(widths).toHaveLength(9);
  expect(widths.filter((w) => w === 1200)).toHaveLength(9);
  await comic.close();
  await ext.close();
});

test("gallery whose full-size pages can't be guessed: the pattern is learned from one page", async () => {
  const url = `${origin}/g/7745/`;
  const comic = await context.newPage();
  await comic.goto(url);
  const ext = await extPage();
  const tabId = await tabIdFor(ext, url);

  const result = await message<{ pages: Array<{ url: string }>; meta: Record<string, unknown> }>(ext, "scan-tab", { tabId, force: true });
  expect(result.pages).toHaveLength(9);

  const meta = { ...result.meta, title: "Lantern Hours", series: "Lantern Hours" };
  const { ids } = await message<{ ids: string[] }>(ext, "queue-jobs", { jobs: [{ meta, format: "cbz", pages: result.pages, sourceUrl: url, tabId }] });
  await waitForJobs(ext, ids);

  const file = join(DOWNLOADS, "KomiK", "Lantern Hours", "Lantern Hours.cbz");
  expect(existsSync(file), `found ${walk(DOWNLOADS).join(", ")}`).toBe(true);
  const { names, xml } = readCbz(file);
  expect(names.filter((n) => n.endsWith(".png"))).toHaveLength(9);
  const widths = Array.from(xml.matchAll(/ImageWidth="(\d+)"/g)).map((m) => Number(m[1]));
  expect(widths.filter((w) => w === 1400)).toHaveLength(9);

  // The naming was learned from one page instead of opening all nine.
  const stats = await comic.evaluate(async (base) => (await fetch(`${base}/__stats`)).json(), origin);
  expect((stats as { hiddenReaderPages: number }).hiddenReaderPages).toBeLessThanOrEqual(2);
  await comic.close();
  await ext.close();
});

test("settings change what is saved: folder, file name, page numbering and tag limit", async () => {
  const ext = await extPage();
  const before = await ext.evaluate(async () => (await chrome.storage.local.get("settings")).settings);
  try {
    await ext.evaluate(async (current) => {
      await chrome.storage.local.set({
        settings: { ...(current as object), folderTemplate: "{site}/{series}", fileTemplate: "{series} - Ch {chapter}", padPages: 4, maxTags: 2, addSiteTag: false }
      });
    }, before);

    const comic = await context.newPage();
    await comic.goto(`${origin}/chapter/lazy`);
    const tabId = await tabIdFor(ext, `${origin}/chapter/lazy`);
    const result = await message<{ pages: Array<{ url: string }>; meta: Record<string, unknown> }>(ext, "scan-tab", { tabId, force: true });
    const { ids } = await message<{ ids: string[] }>(ext, "queue-jobs", { jobs: [{ meta: result.meta, format: "cbz", pages: result.pages, sourceUrl: `${origin}/chapter/lazy`, tabId }] });
    await waitForJobs(ext, ids);

    const file = join(DOWNLOADS, "KomiK", "Fixture Comics", "Starlight Courier", "Starlight Courier - Ch 15.cbz");
    expect(existsSync(file), `found ${walk(DOWNLOADS).join(", ")}`).toBe(true);
    const { names, xml } = readCbz(file);
    expect(names[0]).toBe("0001.png"); // padPages: 4
    expect(xml).toContain("<Genre>Action, Sci-Fi</Genre>"); // maxTags: 2, filled by the genres
    expect(xml).not.toContain("<Tags>");
    await comic.close();
  } finally {
    await ext.evaluate(async (restore) => chrome.storage.local.set({ settings: restore }), before);
    await ext.close();
  }
});

/** Watches the engine's own console for anything the browser blocked. */
async function watchEngineLog(): Promise<() => string[]> {
  const targets = (await (await fetch(`http://127.0.0.1:${debugPort}/json/list`)).json()) as Array<{ url: string; webSocketDebuggerUrl?: string }>;
  const engine = targets.find((t) => t.url.endsWith("offscreen.html") && t.webSocketDebuggerUrl);
  expect(engine, "the download engine should be running").toBeTruthy();
  const socket = new WebSocket(engine!.webSocketDebuggerUrl!);
  const lines: string[] = [];
  await new Promise((done) => socket.addEventListener("open", done, { once: true }));
  socket.addEventListener("message", (event) => {
    const msg = JSON.parse(String(event.data)) as { method?: string; params?: { entry?: { text?: string }; args?: Array<{ value?: string }> } };
    if (msg.method === "Log.entryAdded") lines.push(msg.params?.entry?.text ?? "");
    if (msg.method === "Runtime.consoleAPICalled") lines.push((msg.params?.args ?? []).map((a) => a.value ?? "").join(" "));
  });
  socket.send(JSON.stringify({ id: 1, method: "Log.enable" }));
  socket.send(JSON.stringify({ id: 2, method: "Runtime.enable" }));
  return () => {
    socket.close();
    return lines;
  };
}

test("reading a page full of scripts never asks the browser to load them", async () => {
  const ext = await extPage();

  // One job first, so the engine is running and can be listened to.
  const warmMeta = { ...emptyMetaForTest(), title: "Warm Up", series: "Warm Up" };
  const warm = await message<{ ids: string[] }>(ext, "queue-jobs", { jobs: [{ meta: warmMeta, format: "cbz", pages: [], sourceUrl: `${origin}/paper-moon/`, chapterUrl: `${origin}/paper-moon/chapter-1/` }] });
  await waitForJobs(ext, warm.ids);

  const readLog = await watchEngineLog();
  const meta = { ...emptyMetaForTest(), title: "Quiet Harbor Ch. 4", series: "Quiet Harbor" };
  const { ids } = await message<{ ids: string[] }>(ext, "queue-jobs", { jobs: [{ meta, format: "cbz", pages: [], sourceUrl: `${origin}/csp/chapter`, chapterUrl: `${origin}/csp/chapter` }] });
  await waitForJobs(ext, ids);
  await new Promise((r) => setTimeout(r, 1500));
  const lines = readLog();

  // The page's scripts, stylesheets and frames are never fetched…
  const blocked = lines.filter((l) => /content security policy|refused to load|violates/i.test(l));
  expect(blocked, `engine log: ${blocked.join(" | ")}`).toHaveLength(0);
  // …while its pages still download normally.
  const file = join(DOWNLOADS, "KomiK", "Quiet Harbor", "Quiet Harbor Ch. 4.cbz");
  expect(existsSync(file), `found ${walk(DOWNLOADS).join(", ")}`).toBe(true);
  expect(readCbz(file).names.filter((n) => n.endsWith(".png"))).toHaveLength(4);
  await ext.close();
});

test("options and history pages render", async () => {
  const options = await extPage("options.html#welcome");
  await options.setViewportSize({ width: 1280, height: 900 });
  // Paper (light) is the theme a fresh install starts in.
  expect(await options.evaluate(() => document.documentElement.classList.contains("dark"))).toBe(false);
  await expect(options.getByText("Let's go!")).toBeVisible();
  await options.waitForTimeout(900);
  await options.screenshot({ path: join(SHOTS, "options-welcome.png") });
  await options.evaluate(() => document.documentElement.classList.add("dark"));
  await options.getByText("Let's go!").click();
  await options.locator("#folder").scrollIntoViewIfNeeded();
  await options.waitForTimeout(700);
  await options.screenshot({ path: join(SHOTS, "options-dark.png") });

  const panel = await extPage("sidepanel.html");
  await panel.setViewportSize({ width: 400, height: 820 });
  await panel.getByRole("tab", { name: /History/ }).click();
  await expect(panel.getByText("Starlight Courier Ch. 15").first()).toBeVisible();
  await panel.waitForTimeout(500);
  await panel.screenshot({ path: join(SHOTS, "sidepanel-history.png") });
});

// Runs last: it reloads the extension, which ends every page and worker the other tests use.
test("pages left open while KomiK updates never throw 'Extension context invalidated'", async () => {
  const comic = await context.newPage();
  await comic.goto(`${origin}/chapter/lazy`);
  // Let the page script count the pages and show its button.
  await comic.waitForTimeout(2500);

  // Listen to every script context in the tab, including the one KomiK's page script runs in.
  const targets = (await (await fetch(`http://127.0.0.1:${debugPort}/json/list`)).json()) as Array<{ type: string; url: string; webSocketDebuggerUrl?: string }>;
  const tab = targets.find((t) => t.type === "page" && t.url === `${origin}/chapter/lazy` && t.webSocketDebuggerUrl);
  expect(tab, "the comic tab should be listed").toBeTruthy();
  const socket = new WebSocket(tab!.webSocketDebuggerUrl!);
  const errors: string[] = [];
  await new Promise((done) => socket.addEventListener("open", done, { once: true }));
  socket.addEventListener("message", (event) => {
    const msg = JSON.parse(String(event.data)) as { method?: string; params?: { exceptionDetails?: { text?: string; exception?: { description?: string } } } };
    if (msg.method === "Runtime.exceptionThrown") {
      const d = msg.params?.exceptionDetails;
      errors.push(`${d?.text ?? ""} ${d?.exception?.description ?? ""}`);
    }
  });
  socket.send(JSON.stringify({ id: 1, method: "Runtime.enable" }));
  await new Promise((r) => setTimeout(r, 500));

  // The browser updates the extension while the tab stays open.
  const ext = await extPage();
  await ext.evaluate(() => setTimeout(() => chrome.runtime.reload(), 50)).catch(() => undefined);
  await new Promise((r) => setTimeout(r, 3000));

  // The page keeps changing, which is what used to make the old script call into the dead extension.
  for (let i = 0; i < 4; i++) {
    await comic.evaluate((n) => {
      const img = document.createElement("img");
      img.width = 800;
      img.height = 1200;
      img.src = `/img/extra/${String(n + 40).padStart(3, "0")}.png`;
      document.body.appendChild(img);
      window.scrollBy(0, 400);
    }, i);
    await comic.waitForTimeout(1600);
  }
  await comic.waitForTimeout(500);
  socket.close();

  const invalidated = errors.filter((e) => /context invalidated/i.test(e));
  expect(invalidated, `page errors: ${errors.join(" | ")}`).toHaveLength(0);
  await comic.close().catch(() => undefined);
});
