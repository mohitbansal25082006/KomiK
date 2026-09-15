// Renders the toolbar / store icons from the official Komik crest (Assets/app-icon.png in the app).
import sharp from "sharp";
import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";
import { mkdir } from "node:fs/promises";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const source = resolve(root, "..", "Assets", "app-icon.png");
const out = resolve(root, "public", "icons");
await mkdir(out, { recursive: true });
for (const size of [16, 32, 48, 128]) {
  await sharp(source).resize(size, size, { kernel: size <= 32 ? "lanczos3" : "mitchell" }).png({ compressionLevel: 9 }).toFile(resolve(out, `icon-${size}.png`));
}
console.log("Icons written to public/icons");
