// Reads the real type and pixel size of an image from its first bytes, without decoding it.

export interface ImageInfo {
  mime: string;
  ext: string;
  width?: number;
  height?: number;
}

export function sniffImage(bytes: Uint8Array): ImageInfo | null {
  const b = bytes;
  const len = b.length;
  const u16be = (o: number) => (b[o] << 8) | b[o + 1];
  const u16le = (o: number) => b[o] | (b[o + 1] << 8);
  const u32be = (o: number) => ((b[o] << 24) >>> 0) + (b[o + 1] << 16) + (b[o + 2] << 8) + b[o + 3];

  if (len >= 3 && b[0] === 0xff && b[1] === 0xd8 && b[2] === 0xff) {
    let o = 2;
    while (o + 9 < len) {
      if (b[o] !== 0xff) { o++; continue; }
      const marker = b[o + 1];
      if (marker === 0xd8 || marker === 0x01 || (marker >= 0xd0 && marker <= 0xd7)) { o += 2; continue; }
      const size = u16be(o + 2);
      if (marker >= 0xc0 && marker <= 0xcf && marker !== 0xc4 && marker !== 0xc8 && marker !== 0xcc) {
        return { mime: "image/jpeg", ext: "jpg", height: u16be(o + 5), width: u16be(o + 7) };
      }
      if (size < 2) break;
      o += 2 + size;
    }
    return { mime: "image/jpeg", ext: "jpg" };
  }
  if (len >= 24 && b[0] === 0x89 && b[1] === 0x50 && b[2] === 0x4e && b[3] === 0x47) {
    return { mime: "image/png", ext: "png", width: u32be(16), height: u32be(20) };
  }
  if (len >= 10 && b[0] === 0x47 && b[1] === 0x49 && b[2] === 0x46) {
    return { mime: "image/gif", ext: "gif", width: u16le(6), height: u16le(8) };
  }
  if (len >= 30 && b[0] === 0x52 && b[1] === 0x49 && b[2] === 0x46 && b[3] === 0x46 && b[8] === 0x57 && b[9] === 0x45 && b[10] === 0x42 && b[11] === 0x50) {
    const chunk = String.fromCharCode(b[12], b[13], b[14], b[15]);
    if (chunk === "VP8 ") return { mime: "image/webp", ext: "webp", width: u16le(26) & 0x3fff, height: u16le(28) & 0x3fff };
    if (chunk === "VP8L") {
      const bits = b[21] | (b[22] << 8) | (b[23] << 16) | (b[24] << 24);
      return { mime: "image/webp", ext: "webp", width: (bits & 0x3fff) + 1, height: ((bits >> 14) & 0x3fff) + 1 };
    }
    if (chunk === "VP8X") {
      return { mime: "image/webp", ext: "webp", width: 1 + (b[24] | (b[25] << 8) | (b[26] << 16)), height: 1 + (b[27] | (b[28] << 8) | (b[29] << 16)) };
    }
    return { mime: "image/webp", ext: "webp" };
  }
  if (len >= 12 && b[4] === 0x66 && b[5] === 0x74 && b[6] === 0x79 && b[7] === 0x70) {
    const brand = String.fromCharCode(b[8], b[9], b[10], b[11]);
    if (brand === "avif" || brand === "avis") return { mime: "image/avif", ext: "avif" };
    if (/^hei|^hev|^mif1|^msf1/.test(brand)) return { mime: "image/heic", ext: "heic" };
  }
  if (len >= 26 && b[0] === 0x42 && b[1] === 0x4d) {
    return { mime: "image/bmp", ext: "bmp", width: b[18] | (b[19] << 8), height: Math.abs((b[22] | (b[23] << 8) | (b[24] << 16) | (b[25] << 24)) >> 0) };
  }
  if (len >= 4 && ((b[0] === 0x49 && b[1] === 0x49 && b[2] === 0x2a) || (b[0] === 0x4d && b[1] === 0x4d && b[3] === 0x2a))) {
    return { mime: "image/tiff", ext: "tif" };
  }
  if (len >= 2 && ((b[0] === 0xff && b[1] === 0x0a) || (len >= 12 && b[4] === 0x4a && b[5] === 0x58 && b[6] === 0x4c))) {
    return { mime: "image/jxl", ext: "jxl" };
  }
  return null;
}

/** True when the bytes look like an HTML/JSON error page instead of an image (hotlink blocks, 404 pages). */
export function looksLikeText(bytes: Uint8Array): boolean {
  const head = new TextDecoder().decode(bytes.subarray(0, Math.min(bytes.length, 256))).trimStart().toLowerCase();
  return head.startsWith("<!doctype") || head.startsWith("<html") || head.startsWith("<?xml") || head.startsWith("{") || head.startsWith("<head") || head.startsWith("<body");
}
