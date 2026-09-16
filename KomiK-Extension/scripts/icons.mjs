// Renders the toolbar / store icons from the official Komik crest (Assets/app-icon.png in the app).
// The empty margin around the artwork is trimmed first, so the crest fills the icon instead of floating
// inside it: a pinned toolbar icon is only 16-32px across, where every wasted pixel shows.
import sharp from "sharp";
import { fileURLToPath } from "node:url";
import { dirname, resolve } from "node:path";
import { mkdir } from "node:fs/promises";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const source = resolve(root, "..", "Assets", "app-icon.png");
const out = resolve(root, "public", "icons");
await mkdir(out, { recursive: true });

/** How much of the icon the crest covers; the rest is a hairline of breathing room. */
const FILL = 0.96;
const trimmed = await sharp(source).trim({ threshold: 1 }).toBuffer();

for (const size of [16, 32, 48, 128]) {
  const inner = Math.round(size * FILL);
  const art = await sharp(trimmed)
    .resize(inner, inner, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 }, kernel: size <= 32 ? "lanczos3" : "mitchell" })
    .toBuffer();
  await sharp({ create: { width: size, height: size, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } } })
    .composite([{ input: art, gravity: "center" }])
    .png({ compressionLevel: 9 })
    .toFile(resolve(out, `icon-${size}.png`));
}

console.log(`Icons written to public/icons (crest fills ${Math.round(FILL * 100)}% of each icon)`);
