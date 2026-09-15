import { describe, expect, it } from "vitest";
import { unzipSync, strFromU8 } from "fflate";
import { buildComicInfoXml } from "@/shared/comicinfo";
import { finalizeMeta, metaForChapter } from "@/shared/enrich";
import { cleanTitleNoise, composeTitle, parseIdentity } from "@/shared/identity";
import { sniffImage, looksLikeText } from "@/shared/imageinfo";
import { pageFileName, planSave, renderTemplate, sanitizeSegment, templateTokens } from "@/shared/naming";
import { DEFAULT_SETTINGS, normalizeSettings } from "@/shared/settings";
import { cleanTags, splitPeople, titleCaseTag } from "@/shared/tags";
import { emptyMeta, extOf, formatBytes } from "@/shared/util";
import { buildZip } from "@/offscreen/pack";

const meta = (patch: Partial<ReturnType<typeof emptyMeta>>) => ({ ...emptyMeta(), ...patch });

describe("identity (port of Komik's ComicIdentityParser rules)", () => {
  it.each([
    ["Starlight Courier Vol. 2 Ch. 15", "Starlight Courier", 2, 15, "chapter"],
    ["Starlight Courier Chapter 150.5", "Starlight Courier", null, 150.5, "chapter"],
    ["Iron Orchard #7", "Iron Orchard", null, 7, "issue"],
    ["Iron Orchard Vol. 3 #12", "Iron Orchard", 3, 12, "issue"],
    ["Paper Moon Episode 42", "Paper Moon", null, 42, "episode"],
    ["Night Market Vol. 4", "Night Market", 4, null, "volume"],
    ["Night_Market_v01_c003", "Night Market", 1, 3, "chapter"],
    ["Quiet Harbor 042", "Quiet Harbor", null, 42, "issue"]
  ])("parses %s", (raw, series, volume, number, kind) => {
    const id = parseIdentity(raw);
    expect(id.series).toBe(series);
    expect(id.volume).toBe(volume);
    expect(id.number).toBe(number);
    expect(id.kind).toBe(kind);
  });

  it("keeps the chapter's own name as subtitle and a year out of the series", () => {
    const id = parseIdentity("Starlight Courier Ch. 12 - The Long Night");
    expect(id.series).toBe("Starlight Courier");
    expect(id.subtitle).toBe("The Long Night");
    expect(parseIdentity("Iron Orchard (2021) #3").year).toBe(2021);
  });

  it("strips reader-site noise from titles", () => {
    expect(cleanTitleNoise("Read Starlight Courier Chapter 3 Online Free", "")).toBe("Starlight Courier Chapter 3");
    expect(cleanTitleNoise("Starlight Courier Chapter 3 - ComicHaven", "ComicHaven")).toBe("Starlight Courier Chapter 3");
  });

  it("composes titles Komik reads back identically", () => {
    const title = composeTitle({ series: "Starlight Courier", volume: "2", number: "15", kind: "chapter" });
    expect(title).toBe("Starlight Courier Vol. 2 Ch. 15");
    const back = parseIdentity(title);
    expect([back.series, back.volume, back.number]).toEqual(["Starlight Courier", 2, 15]);
    expect(composeTitle({ series: "Iron Orchard", number: "7", kind: "issue" })).toBe("Iron Orchard #7");
    expect(composeTitle({ series: "S", number: "1", kind: "chapter", chapterTitle: "Begin", includeChapterTitle: true })).toBe("S Ch. 1 - Begin");
  });
});

describe("naming", () => {
  it("makes Windows-safe names", () => {
    expect(sanitizeSegment('What? "Now": A/B')).toBe("What 'Now' - A B");
    expect(sanitizeSegment("CON")).toBe("CON_");
    expect(sanitizeSegment("  trailing dots... ")).toBe("trailing dots");
    expect(sanitizeSegment("")).toBe("Untitled");
    expect(sanitizeSegment("x".repeat(300)).length).toBeLessThanOrEqual(120);
  });

  it("drops labels of empty tokens", () => {
    const tokens = templateTokens(meta({ title: "T", series: "Series", number: "5" }));
    expect(renderTemplate("{series} Vol. {volume} Ch. {chapter} ({year})", tokens)).toBe("Series Ch. 5");
    expect(renderTemplate("{series}/{series} #{issue}", tokens)).toBe("Series/Series #5");
    expect(renderTemplate("{series} Vol. {volume}", templateTokens(meta({ series: "S", volume: "2" })))).toBe("S Vol. 2");
  });

  it("plans Downloads/KomiK/<Series>/<Title>.cbz by default", () => {
    const plan = planSave(meta({ title: "Starlight Courier Vol. 2 Ch. 15", series: "Starlight Courier" }), "cbz", DEFAULT_SETTINGS);
    expect(plan.relativePath).toBe("KomiK/Starlight Courier/Starlight Courier Vol. 2 Ch. 15.cbz");
    expect(planSave(meta({ title: "X", series: "Y" }), "folder", DEFAULT_SETTINGS).fileName).toBe("X");
    expect(planSave(meta({ title: "X", series: "Y" }), "pdf", { ...DEFAULT_SETTINGS, folderMode: "custom" }).relativePath).toBe("Y/X.pdf");
  });

  it("keeps long paths under the Windows limit", () => {
    const long = "A very long comic title that keeps going ".repeat(10);
    const plan = planSave(meta({ title: long, series: long }), "cbz", DEFAULT_SETTINGS);
    expect(plan.relativePath.length).toBeLessThanOrEqual(200);
    expect(plan.fileName.endsWith(".cbz")).toBe(true);
  });

  it("pads page names and grows for long chapters", () => {
    expect(pageFileName(0, 20, "jpg", 3)).toBe("001.jpg");
    expect(pageFileName(1233, 1500, "png", 3)).toBe("1234.png");
  });
});

describe("tags", () => {
  it("cleans site junk, counts and duplicates", () => {
    const out = cleanTags(["Action (1,234)", "action", "Home", "READ NOW", "sci-fi", "Chapter 3", "#Isekai", "slice of life", "Dramas", "Drama"], { clean: true, max: 30 });
    expect(out).toEqual(["Action", "Sci-Fi", "Isekai", "Slice of Life", "Dramas"]);
  });
  it("excludes the comic's own title and respects the limit", () => {
    expect(cleanTags(["Starlight Courier", "Space", "Mystery", "Romance"], { clean: true, max: 2, exclude: ["starlight courier"] })).toEqual(["Space", "Mystery"]);
  });
  it("title-cases sensibly", () => {
    expect(titleCaseTag("school life")).toBe("School Life");
    expect(titleCaseTag("BL")).toBe("BL");
    expect(titleCaseTag("iPhone")).toBe("iPhone");
  });
  it("splits credit lines", () => {
    expect(splitPeople("Ada Ink, Rio Pen & Mo Lee (Art)")).toEqual(["Ada Ink", "Rio Pen", "Mo Lee"]);
    expect(splitPeople(["Updating", "Ada Ink", "ada ink"])).toEqual(["Ada Ink"]);
  });
});

describe("ComicInfo.xml", () => {
  it("writes every Komik field, escapes text and marks the cover", () => {
    const xml = buildComicInfoXml(
      meta({ title: "Tom & Jerry-ish <Ch. 1>", series: "Cat Chase", number: "001", volume: "2", summary: 'He said "hi"', year: "2023", month: "7", day: "4", writers: ["Ada Ink"], artists: ["Rio Pen", "Mo Lee"], publisher: "Ink & Co", genres: ["Comedy"], tags: ["Cats", "Slapstick"], web: "https://example.com/c/1", language: "en", manga: "YesAndRightToLeft" }),
      [{ width: 800, height: 1200, size: 1000 }, { width: 800, height: 1200 }],
      1,
      "note"
    );
    expect(xml).toContain("<Title>Tom &amp; Jerry-ish &lt;Ch. 1&gt;</Title>");
    expect(xml).toContain("<Number>1</Number>");
    expect(xml).toContain("<Volume>2</Volume>");
    expect(xml).toContain("<Summary>He said &quot;hi&quot;</Summary>");
    expect(xml).toContain("<Year>2023</Year>");
    expect(xml).toContain("<Month>7</Month>");
    expect(xml).toContain("<Day>4</Day>");
    expect(xml).toContain("<Writer>Ada Ink</Writer>");
    expect(xml).toContain("<Penciller>Rio Pen, Mo Lee</Penciller>");
    expect(xml).toContain("<Genre>Comedy</Genre>");
    expect(xml).toContain("<Tags>Cats, Slapstick</Tags>");
    expect(xml).toContain("<PageCount>2</PageCount>");
    expect(xml).toContain('<Page Image="1" Type="FrontCover" ImageWidth="800" ImageHeight="1200" />');
    const doc = new DOMParser().parseFromString(xml, "application/xml");
    expect(doc.querySelector("parsererror")).toBeNull();
    expect(doc.documentElement.nodeName).toBe("ComicInfo");
  });

  it("leaves out invalid dates", () => {
    const xml = buildComicInfoXml(meta({ title: "X", year: "21", month: "13" }), []);
    expect(xml).not.toContain("<Year>");
    expect(xml).not.toContain("<Month>");
  });
});

describe("enrichment", () => {
  it("finalizes metadata for Komik", () => {
    const out = finalizeMeta(meta({ series: " Starlight Courier ", number: "15", numberKind: "chapter", site: "comichaven.example", genres: ["action", "Action"], tags: ["Home", "space opera", "Action"], writers: ["Ada Ink, Rio Pen"] }), { ...DEFAULT_SETTINGS, addSiteTag: true });
    expect(out.title).toBe("Starlight Courier Ch. 15");
    expect(out.genres).toEqual(["Action"]);
    expect(out.tags).toEqual(["Space Opera", "Comichaven.example"]);
    expect(out.writers).toEqual(["Ada Ink", "Rio Pen"]);
  });

  it("builds per-chapter details from a series page", () => {
    const base = meta({ series: "Paper Moon", genres: ["Drama"], site: "x" });
    const m = metaForChapter(base, { url: "https://x/ep-3", title: "Episode 3: Lanterns", number: 3, volume: null }, DEFAULT_SETTINGS);
    expect(m.title).toBe("Paper Moon Episode 3");
    expect(m.chapterTitle).toBe("Lanterns");
    expect(m.web).toBe("https://x/ep-3");
    expect(m.genres).toEqual(["Drama"]);
  });
});

describe("images & utils", () => {
  const png = Uint8Array.from(atob("iVBORw0KGgoAAAANSUhEUgAAAAIAAAADCAYAAAC56t6BAAAAEklEQVR42mP8z8DwnwEIGGEMAB+iAv/xJk8XAAAAAElFTkSuQmCC"), (c) => c.charCodeAt(0));
  it("sniffs real image types and sizes", () => {
    expect(sniffImage(png)).toMatchObject({ ext: "png", width: 2, height: 3 });
    const gif = new Uint8Array([0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 10, 0, 20, 0]);
    expect(sniffImage(gif)).toMatchObject({ ext: "gif", width: 10, height: 20 });
    const jpeg = new Uint8Array([0xff, 0xd8, 0xff, 0xe0, 0, 4, 0, 0, 0xff, 0xc0, 0, 11, 8, 0x04, 0xb0, 0x03, 0x20, 3, 1, 0x22, 0]);
    expect(sniffImage(jpeg)).toMatchObject({ ext: "jpg", width: 800, height: 1200 });
    const avif = new Uint8Array([0, 0, 0, 0x1c, 0x66, 0x74, 0x79, 0x70, 0x61, 0x76, 0x69, 0x66]);
    expect(sniffImage(avif)?.ext).toBe("avif");
    expect(sniffImage(new TextEncoder().encode("<!doctype html><html>"))).toBeNull();
    expect(looksLikeText(new TextEncoder().encode("  <!DOCTYPE html>"))).toBe(true);
  });

  it("formats and parses helpers", () => {
    expect(formatBytes(1536)).toBe("1.5 KB");
    expect(extOf("https://x.test/a/Page%20001.WEBP?w=1")).toBe("webp");
    expect(normalizeSettings({ perHostConnections: 99, downloadsSubfolder: "\\KomiK\\Manga\\" }).perHostConnections).toBe(16);
    expect(normalizeSettings({ downloadsSubfolder: "\\KomiK\\Manga\\" }).downloadsSubfolder).toBe("KomiK/Manga");
  });

  it("builds a valid CBZ with pages stored and ComicInfo.xml inside", async () => {
    const pages = [0, 1, 2].map((i) => ({ name: `00${i + 1}.png`, data: png.slice().buffer, mime: "image/png", width: 2, height: 3 }));
    const blob = await buildZip(pages, buildComicInfoXml(meta({ title: "Zip Test #1", series: "Zip Test", number: "1" }), pages));
    const files = unzipSync(new Uint8Array(await blob.arrayBuffer()));
    expect(Object.keys(files)).toEqual(["001.png", "002.png", "003.png", "ComicInfo.xml"]);
    expect(files["002.png"]).toEqual(png);
    expect(strFromU8(files["ComicInfo.xml"])).toContain("<Series>Zip Test</Series>");
  });
});
