// Builds KomiK Downloader into dist/ as an unpacked Chromium extension (Chrome, Edge, Brave).
//   1. Extension pages (popup, side panel, options, offscreen engine) as ES modules
//   2. Background service worker and content scripts as self-contained IIFE bundles
//   3. manifest.json generated from src/manifest.ts
import { build } from "vite";
import react from "@vitejs/plugin-react";
import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";
import { mkdir, rm, writeFile } from "node:fs/promises";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const dist = resolve(root, "dist");
const watch = process.argv.includes("--watch");
const production = !watch;

const shared = {
  root,
  configFile: false,
  logLevel: "warn",
  define: { "process.env.NODE_ENV": JSON.stringify(production ? "production" : "development") },
  resolve: { alias: { "@": resolve(root, "src") } }
};

async function buildPages() {
  await build({
    ...shared,
    root: resolve(root, "src/pages"),
    base: "./",
    publicDir: resolve(root, "public"),
    plugins: [react()],
    css: { postcss: resolve(root) },
    build: {
      outDir: dist,
      emptyOutDir: false,
      target: "chrome116",
      minify: production,
      sourcemap: !production,
      modulePreload: false,
      chunkSizeWarningLimit: 900,
      watch: watch ? {} : null,
      rollupOptions: {
        input: {
          popup: resolve(root, "src/pages/popup.html"),
          sidepanel: resolve(root, "src/pages/sidepanel.html"),
          options: resolve(root, "src/pages/options.html"),
          offscreen: resolve(root, "src/pages/offscreen.html")
        },
        output: {
          entryFileNames: "assets/[name].js",
          chunkFileNames: "assets/[name]-[hash].js",
          assetFileNames: "assets/[name]-[hash][extname]"
        }
      }
    }
  });
}

async function buildScript(entry, outFile, name) {
  await build({
    ...shared,
    publicDir: false,
    build: {
      outDir: dist,
      emptyOutDir: false,
      target: "chrome116",
      minify: production,
      sourcemap: false,
      watch: watch ? {} : null,
      lib: {
        entry: resolve(root, entry),
        name,
        formats: ["iife"],
        fileName: () => outFile
      }
    }
  });
}

async function writeManifest() {
  const { buildManifest } = await import(`../src/manifest.mjs?${Date.now()}`);
  await writeFile(resolve(dist, "manifest.json"), JSON.stringify(buildManifest(), null, 2));
}

const started = Date.now();
if (!watch) await rm(dist, { recursive: true, force: true });
await mkdir(dist, { recursive: true });
await writeManifest();
await Promise.all([
  buildPages(),
  buildScript("src/background/index.ts", "background.js", "KomikBackground"),
  buildScript("src/content/sentinel.ts", "content/sentinel.js", "KomikSentinel"),
  buildScript("src/content/engine.ts", "content/engine.js", "KomikEngine")
]);
console.log(`KomiK Downloader built into dist/ in ${((Date.now() - started) / 1000).toFixed(1)}s${watch ? " (watching)" : ""}`);
