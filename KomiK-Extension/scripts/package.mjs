// Zips dist/ into release/komik-downloader-<version>.zip, ready to upload to the Chrome Web Store or
// Microsoft Edge Add-ons (Brave installs from the Chrome Web Store).
import { zipSync } from "fflate";
import { fileURLToPath } from "node:url";
import { dirname, join, relative, resolve } from "node:path";
import { mkdir, readdir, readFile, stat, writeFile } from "node:fs/promises";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const dist = join(root, "dist");
const manifest = JSON.parse(await readFile(join(dist, "manifest.json"), "utf8"));

async function collect(dir, out = {}) {
  for (const name of await readdir(dir)) {
    const full = join(dir, name);
    if ((await stat(full)).isDirectory()) await collect(full, out);
    else if (!name.endsWith(".map")) out[relative(dist, full).split("\\").join("/")] = new Uint8Array(await readFile(full));
  }
  return out;
}

const files = await collect(dist);
const zip = zipSync(files, { level: 9 });
const outDir = join(root, "release");
await mkdir(outDir, { recursive: true });
const outFile = join(outDir, `komik-downloader-${manifest.version}.zip`);
await writeFile(outFile, zip);
console.log(`Packaged ${Object.keys(files).length} files (${(zip.length / 1024).toFixed(0)} KB) → ${relative(root, outFile)}`);
