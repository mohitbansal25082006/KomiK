// End-to-end: loads the built extension into Chromium, scans local fixture comic pages and checks the
// real files written to the Downloads folder (name, folder, pages and ComicInfo.xml).
import { chromium, expect, test, type Browser, type BrowserContext, type Page } from "@playwright/test";
import { spawn, type ChildProcess } from "node:child_process";
import { existsSync, mkdirSync, readdirSync, readFileSync, rmSync, statSync, writeFileSync } from "node:fs";
import { createServer as createNetServer } from "node:net";
import { dirname, join, resolve } from "node:path";
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

/** Opens an extension page as a browser-initiated tab (web pages may not navigate to extension URLs). */
async function extPage(path = "options.html"): Promise<Page> {
  const url = `chrome-extension://${extensionId}/${path}`;
  const cdp = await browser.newBrowserCDPSession();
  const waiter = context.waitForEvent("page", { predicate: (p) => p.url().startsWith(url.split("#")[0].split("?")[0]), timeout: 20_000 });
  await cdp.send("Target.createTarget", { url });
  const page = await waiter;
  await page.waitForLoadState("load");
  await cdp.detach();
  return page;
}

async function tabIdFor(ext: Page, url: string): Promise<number> {
  return ext.evaluate(async (u) => (await chrome.tabs.query({})).find((t) => t.url === u)!.id!, url);
}

async function message<T>(ext: Page, type: string, payload: unknown): Promise<T> {
  return ext.evaluate(({ type, payload }) => chrome.runtime.sendMessage({ komik: true, type, payload, target: "background" }), { type, payload }) as Promise<T>;
}

async function waitForJobs(ext: Page, ids: string[], timeout = 90_000) {
  const started = Date.now();
  for (;;) {
    const snap = await message<{ jobs: Array<{ id: string; status: string; error?: string; savedPath?: string }> }>(ext, "jobs-snapshot", {});
    const mine = snap.jobs.filter((j) => ids.includes(j.id));
    const failed = mine.find((j) => j.status === "error");
    if (failed) throw new Error(`Job failed: ${failed.error}`);
    if (mine.length === ids.length && mine.every((j) => j.status === "done")) return mine;
    if (Date.now() - started > timeout) throw new Error(`Timed out: ${JSON.stringify(mine.map((j) => [j.status, j.error]))}`);
    await new Promise((r) => setTimeout(r, 400));
  }
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
  writeFileSync(join(TMP, "profile", "Default", "Preferences"), JSON.stringify({ download: { default_directory: DOWNLOADS, prompt_for_download: false, directory_upgrade: true }, savefile: { default_directory: DOWNLOADS } }));

  const fixture = await startFixtureServer();
  origin = fixture.origin;
  closeServer = () => fixture.server.close();

  const port = await freePort();
  chrome = spawn(chromium.executablePath(), [
    `--user-data-dir=${join(TMP, "profile")}`,
    `--remote-debugging-port=${port}`,
    `--disable-extensions-except=${DIST}`,
    `--load-extension=${DIST}`,
    "--headless=new",
    "--no-first-run",
    "--no-default-browser-check",
    "--disable-features=DownloadBubble,DownloadBubbleV2",
    "--window-size=1280,900",
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
  await session.send("Browser.setDownloadBehavior", { behavior: "default" });
  await session.detach();
  // Chromium ships hidden component extensions too; ours is the one running background.js.
  const ours = (url: string) => url.startsWith("chrome-extension://") && url.endsWith("/background.js");
  let worker = context.serviceWorkers().find((w) => ours(w.url()));
  worker ??= await context.waitForEvent("serviceworker", { predicate: (w) => ours(w.url()), timeout: 20_000 });
  extensionId = new URL(worker.url()).host;
});

test.afterAll(async () => {
  await browser?.close().catch(() => undefined);
  chrome?.kill();
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

test("site CBZ link: saved through KomiK with fresh ComicInfo.xml", async () => {
  const url = `${origin}/files`;
  const comic = await context.newPage();
  await comic.goto(url);
  const ext = await extPage();
  const tabId = await tabIdFor(ext, url);
  const result = await message<{ files: Array<{ url: string; ext: string; name: string; label: string }>; meta: Record<string, unknown> }>(ext, "scan-tab", { tabId, force: true });
  expect(result.files).toHaveLength(1);
  const meta = { ...result.meta, tags: ["Night Life"], title: "Night Market Vol. 1", series: "Night Market", volume: "1" };
  const { ids } = await message<{ ids: string[] }>(ext, "queue-jobs", { jobs: [{ meta, format: "cbz", pages: [], sourceUrl: url, tabId, file: result.files[0] }] });
  await waitForJobs(ext, ids);
  const file = join(DOWNLOADS, "KomiK", "Night Market", "Night Market Vol. 1.cbz");
  expect(existsSync(file), `found ${walk(DOWNLOADS).join(", ")}`).toBe(true);
  const { names, xml } = readCbz(file);
  expect(names).toEqual(["001.png", "002.png", "ComicInfo.xml"]);
  expect(xml).toContain("<Title>Night Market Vol. 1</Title>");
  expect(xml).toContain("<Tags>Night Life</Tags>");
  expect(xml).not.toContain("Old title");
  await comic.close();
  await ext.close();
});

test("clicking the site's own download button routes the file into the KomiK folder", async () => {
  const comic = await context.newPage();
  await comic.goto(`${origin}/files`);
  const before = new Set(walk(DOWNLOADS));
  await comic.getByText("Download CBZ").click();
  let created: string[] = [];
  for (let i = 0; i < 60 && !created.length; i++) {
    await new Promise((r) => setTimeout(r, 250));
    created = walk(DOWNLOADS).filter((f) => !before.has(f) && f.endsWith(".cbz"));
  }
  expect(created, `found ${walk(DOWNLOADS).join(", ")}`).toHaveLength(1);
  expect(created[0].replace(/\\/g, "/")).toContain("/KomiK/");
  await comic.close();
});

test("options and history pages render", async () => {
  const options = await extPage("options.html#welcome");
  await options.setViewportSize({ width: 1280, height: 900 });
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
  await expect(panel.getByText("Starlight Courier Ch. 15")).toBeVisible();
  await panel.waitForTimeout(500);
  await panel.screenshot({ path: join(SHOTS, "sidepanel-history.png") });
});
