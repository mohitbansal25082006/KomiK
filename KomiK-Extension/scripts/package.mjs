// Zips dist/ into release/komik-downloader-<version>.zip, ready to upload to the Chrome Web Store or
// Microsoft Edge Add-ons (Brave installs from the Chrome Web Store). The fonts and libraries bundled into
// the extension are shared under licenses that ask for their notices to travel with them, so the zip also
// carries THIRD-PARTY-NOTICES.txt, built from the dependencies that actually ship.
import { strToU8, zipSync } from "fflate";
import { existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join, relative, resolve } from "node:path";
import { mkdir, readdir, readFile, stat, writeFile } from "node:fs/promises";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const dist = join(root, "dist");
const modules = join(root, "node_modules");
const manifest = JSON.parse(await readFile(join(dist, "manifest.json"), "utf8"));

async function collect(dir, out = {}) {
  for (const name of await readdir(dir)) {
    const full = join(dir, name);
    if ((await stat(full)).isDirectory()) await collect(full, out);
    else if (!name.endsWith(".map")) out[relative(dist, full).split("\\").join("/")] = new Uint8Array(await readFile(full));
  }
  return out;
}

/** Every package that ends up in the bundle: the runtime dependencies and theirs, never dev tools. */
async function shippedPackages() {
  const own = JSON.parse(await readFile(join(root, "package.json"), "utf8"));
  const found = new Map();
  const queue = Object.keys(own.dependencies ?? {});
  while (queue.length) {
    const name = queue.shift();
    if (found.has(name)) continue;
    const dir = join(modules, ...name.split("/"));
    if (!existsSync(join(dir, "package.json"))) continue;
    const pkg = JSON.parse(await readFile(join(dir, "package.json"), "utf8"));
    found.set(name, { dir, pkg });
    queue.push(...Object.keys(pkg.dependencies ?? {}));
  }
  return [...found.entries()].sort(([a], [b]) => a.localeCompare(b));
}

async function licenseText(dir) {
  for (const name of await readdir(dir)) {
    if (/^(licen[cs]e|copying)(\.(md|txt))?$/i.test(name)) return (await readFile(join(dir, name), "utf8")).trim();
  }
  return "";
}

async function thirdPartyNotices() {
  const sections = [];
  for (const [name, { dir, pkg }] of await shippedPackages()) {
    const text = await licenseText(dir);
    const license = typeof pkg.license === "string" ? pkg.license : pkg.license?.type ?? "see package";
    sections.push([`${name} ${pkg.version} (${license})`, pkg.homepage ?? "", "", text || `Licensed under ${license}.`].join("\n"));
  }
  const rule = "-".repeat(78);
  return [
    `KomiK Downloader ${manifest.version} includes the following third-party software and fonts.`,
    "",
    ...sections.flatMap((s) => [rule, s, ""])
  ].join("\n");
}

const files = await collect(dist);
files["THIRD-PARTY-NOTICES.txt"] = strToU8(await thirdPartyNotices());
const zip = zipSync(files, { level: 9 });
const outDir = join(root, "release");
await mkdir(outDir, { recursive: true });
const outFile = join(outDir, `komik-downloader-${manifest.version}.zip`);
await writeFile(outFile, zip);
console.log(`Packaged ${Object.keys(files).length} files (${(zip.length / 1024).toFixed(0)} KB) → ${relative(root, outFile)}`);
