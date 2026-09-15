// Builds the output file: CBZ/ZIP (pages stored without recompression + ComicInfo.xml), PDF with
// document info, or repacks a site's own CBZ/PDF with metadata added.
import { unzip, Zip, ZipPassThrough, strToU8 } from "fflate";
import { PDFDocument } from "pdf-lib";
import { buildComicInfoXml, type ComicInfoPage } from "@/shared/comicinfo";
import { sniffImage } from "@/shared/imageinfo";
import type { ComicMeta } from "@/shared/types";
import { IMAGE_EXTENSIONS } from "@/shared/util";
import { convertImage } from "./fetcher";

export interface PackPage {
  name: string;
  data: ArrayBuffer;
  mime: string;
  width?: number;
  height?: number;
}

const XML_NOTES = (source: string) => `Downloaded with KomiK Downloader from ${source} on ${new Date().toISOString().slice(0, 10)}.`;

export function comicInfoFor(meta: ComicMeta, pages: PackPage[], coverIndex: number, source: string): string {
  const info: ComicInfoPage[] = pages.map((p) => ({ width: p.width, height: p.height, size: p.data.byteLength }));
  return buildComicInfoXml(meta, info, coverIndex, XML_NOTES(source));
}

/** ZIP with every page STORED (images are already compressed) and ComicInfo.xml deflated. */
export async function buildZip(pages: PackPage[], comicInfoXml: string, onProgress?: (done: number) => void): Promise<Blob> {
  const chunks: Uint8Array[] = [];
  let finished!: () => void;
  let failed!: (err: Error) => void;
  const done = new Promise<void>((resolve, reject) => {
    finished = resolve;
    failed = reject;
  });
  const zip = new Zip((err, chunk, final) => {
    if (err) return failed(err);
    chunks.push(chunk);
    if (final) finished();
  });
  const mtime = new Date();
  for (const [i, page] of pages.entries()) {
    const entry = new ZipPassThrough(page.name);
    entry.mtime = mtime;
    zip.add(entry);
    entry.push(new Uint8Array(page.data), true);
    onProgress?.(i + 1);
    // Let the event loop breathe on huge chapters.
    if (i % 25 === 24) await new Promise((r) => setTimeout(r, 0));
  }
  const xml = new ZipPassThrough("ComicInfo.xml");
  xml.mtime = mtime;
  zip.add(xml);
  xml.push(strToU8(comicInfoXml), true);
  zip.end();
  await done;
  return new Blob(chunks as BlobPart[], { type: "application/vnd.comicbook+zip" });
}

export async function buildPdf(pages: PackPage[], meta: ComicMeta, quality: number, onProgress?: (done: number) => void): Promise<Blob> {
  const pdf = await PDFDocument.create();
  applyPdfInfo(pdf, meta);
  for (const [i, page] of pages.entries()) {
    let bytes = new Uint8Array(page.data);
    let kind = sniffImage(bytes)?.ext;
    if (kind !== "jpg" && kind !== "png") {
      const converted = await convertImage(bytes, "image/jpeg", quality);
      bytes = new Uint8Array(converted.data);
      kind = "jpg";
    }
    const image = kind === "png" ? await pdf.embedPng(bytes) : await pdf.embedJpg(bytes);
    const p = pdf.addPage([image.width, image.height]);
    p.drawImage(image, { x: 0, y: 0, width: image.width, height: image.height });
    onProgress?.(i + 1);
  }
  const out = await pdf.save({ useObjectStreams: true });
  return new Blob([out as BlobPart], { type: "application/pdf" });
}

function applyPdfInfo(pdf: PDFDocument, meta: ComicMeta) {
  pdf.setTitle(meta.title || meta.series);
  const authors = [...meta.writers, ...meta.artists.filter((a) => !meta.writers.includes(a))];
  if (authors.length) pdf.setAuthor(authors.join(", "));
  if (meta.series) pdf.setSubject(meta.number ? `${meta.series} #${meta.number}` : meta.series);
  const keywords = [...meta.genres, ...meta.tags];
  if (keywords.length) pdf.setKeywords(keywords);
  pdf.setCreator("KomiK Downloader");
  pdf.setProducer("KomiK Downloader");
  if (meta.language) pdf.setLanguage(meta.language);
}

/** Adds ComicInfo.xml to a site's own CBZ/ZIP (replacing an older one) without recompressing pages. */
export async function repackArchive(data: ArrayBuffer, meta: ComicMeta, source: string): Promise<{ blob: Blob; pages: number }> {
  const files = await new Promise<Record<string, Uint8Array>>((resolve, reject) =>
    unzip(new Uint8Array(data), (err, out) => (err ? reject(err) : resolve(out)))
  );
  const names = Object.keys(files).filter((n) => !n.endsWith("/") && !/(^|\/)comicinfo\.xml$/i.test(n) && !n.startsWith("__MACOSX"));
  const collator = new Intl.Collator(undefined, { numeric: true, sensitivity: "base" });
  names.sort(collator.compare);
  const pages: PackPage[] = [];
  const others: PackPage[] = [];
  for (const name of names) {
    const bytes = files[name];
    const ext = name.split(".").pop()?.toLowerCase() ?? "";
    const info = IMAGE_EXTENSIONS.includes(ext) ? sniffImage(bytes) : null;
    const entry = { name, data: bytes.slice().buffer, mime: info?.mime ?? "", width: info?.width, height: info?.height };
    (info ? pages : others).push(entry);
  }
  const xml = comicInfoFor(meta, pages, 0, source);
  const blob = await buildZip([...pages, ...others], xml);
  return { blob, pages: pages.length };
}

export async function retagPdf(data: ArrayBuffer, meta: ComicMeta): Promise<Blob> {
  const pdf = await PDFDocument.load(data, { ignoreEncryption: true, updateMetadata: false });
  applyPdfInfo(pdf, meta);
  const out = await pdf.save({ useObjectStreams: false });
  return new Blob([out as BlobPart], { type: "application/pdf" });
}

/** A small JPEG data URL of the cover for the history list. */
export async function coverThumbnail(data: ArrayBuffer): Promise<string | undefined> {
  try {
    const bitmap = await createImageBitmap(new Blob([data]), { resizeWidth: 144, resizeQuality: "medium" });
    const canvas = new OffscreenCanvas(bitmap.width, bitmap.height);
    canvas.getContext("2d")!.drawImage(bitmap, 0, 0);
    bitmap.close();
    const blob = await canvas.convertToBlob({ type: "image/jpeg", quality: 0.78 });
    return await new Promise<string>((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(String(reader.result));
      reader.onerror = () => reject(reader.error);
      reader.readAsDataURL(blob);
    });
  } catch {
    return undefined;
  }
}
