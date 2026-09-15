import { describe, expect, it } from "vitest";
import { scanDocument } from "@/engine/scan";
import { fillSequenceGaps } from "@/engine/detect/cluster";
import { findFileLinks, findPagination } from "@/engine/detect/chapters";

function doc(html: string): Document {
  return new DOMParser().parseFromString(html, "text/html");
}

const scan = (html: string, url = "https://comics.example/series/starlight-courier/chapter-15") =>
  scanDocument(doc(html), { url, live: false, rules: [], minPageWidth: 280 });

const pageImgs = (n: number, attr = "src", base = "https://cdn.example/sc/15") =>
  Array.from({ length: n }, (_, i) => `<img ${attr}="${base}/${String(i + 1).padStart(3, "0")}.jpg" width="800" height="1200" />`).join("\n");

describe("universal page detection", () => {
  it("finds the reader column and ignores logos, avatars and sidebar art", () => {
    const r = scan(`
      <header><img src="https://comics.example/logo.png" width="200" height="60"></header>
      <main><div class="reader-area">${pageImgs(12)}</div></main>
      <aside class="sidebar"><img src="https://cdn.example/covers/a.jpg" width="300" height="450"><img src="https://cdn.example/covers/b.jpg" width="300" height="450"></aside>
      <div class="comments"><img src="https://comics.example/avatar/u1.png" width="64" height="64"></div>`);
    expect(r.pages).toHaveLength(12);
    expect(r.pages[0].url).toBe("https://cdn.example/sc/15/001.jpg");
    expect(r.pages[11].url).toBe("https://cdn.example/sc/15/012.jpg");
    expect(r.confidence).toBe("high");
    expect(r.pages[0].referer).toContain("chapter-15");
  });

  it("reads lazy-load attributes instead of placeholders", () => {
    const imgs = Array.from({ length: 6 }, (_, i) => `<img src="data:image/gif;base64,R0lGODlhAQABAAAAACw=" data-src="https://cdn.example/lazy/p${i + 1}.webp" class="lazyload">`).join("");
    const r = scan(`<div class="chapter-content">${imgs}</div>`);
    expect(r.pages.map((p) => p.url)).toEqual(Array.from({ length: 6 }, (_, i) => `https://cdn.example/lazy/p${i + 1}.webp`));
  });

  it("picks the largest srcset candidate", () => {
    const imgs = Array.from({ length: 4 }, (_, i) => `<img src="https://cdn.example/s/${i}-small.jpg" srcset="https://cdn.example/s/${i}-small.jpg 400w, https://cdn.example/s/${i}-large.jpg 1600w">`).join("");
    const r = scan(`<section class="webtoon-viewer">${imgs}</section>`);
    expect(r.pages[0].url).toBe("https://cdn.example/s/0-large.jpg");
  });

  it("finds pages listed only in a reader script", () => {
    const list = Array.from({ length: 8 }, (_, i) => `"https:\\/\\/img.example\\/ch15\\/${i + 1}.png"`).join(",");
    const r = scan(`<div id="viewer"></div><script>window.pages = [${list}];</script>`);
    expect(r.pages).toHaveLength(8);
    expect(r.pages[7].url).toBe("https://img.example/ch15/8.png");
  });

  it("fills gaps in numbered page URLs", () => {
    const urls = ["001", "002", "003", "005", "006"].map((n) => `https://cdn.example/x/${n}.jpg`);
    expect(fillSequenceGaps(urls)).toEqual(["001", "002", "003", "004", "005", "006"].map((n) => `https://cdn.example/x/${n}.jpg`));
    expect(fillSequenceGaps(["https://a/1.jpg", "https://a/40.jpg", "https://a/41.jpg", "https://a/90.jpg"])).toHaveLength(4);
  });
});

describe("adapters", () => {
  it("reads the WP manga reader layout with its chapter list and genres", () => {
    const series = scan(
      `<div class="post-title"><h1>Starlight Courier</h1></div>
       <div class="author-content"><a>Ada Ink</a></div><div class="artist-content"><a>Rio Pen</a></div>
       <div class="genres-content"><a>Action</a><a>Sci-fi</a></div>
       <div class="summary__content"><p>A courier crosses the stars.</p></div>
       <ul class="main version-chap">
         ${[3, 2, 1].map((n) => `<li class="wp-manga-chapter"><a href="https://comics.example/series/starlight-courier/chapter-${n}/">Chapter ${n}</a></li>`).join("")}
       </ul>`,
      "https://comics.example/series/starlight-courier/"
    );
    expect(series.adapter).toBe("WP Manga reader theme");
    expect(series.isSeriesPage).toBe(true);
    expect(series.chapters.map((c) => c.number)).toEqual([1, 2, 3]);
    expect(series.meta.series).toBe("Starlight Courier");
    expect(series.meta.title).toBe("Starlight Courier");
    expect(series.meta.writers).toEqual(["Ada Ink"]);
    expect(series.meta.artists).toEqual(["Rio Pen"]);
    expect(series.meta.genres).toEqual(["Action", "Sci-fi"]);
    expect(series.meta.summary).toBe("A courier crosses the stars.");

    const chapter = scan(`<ol class="breadcrumb"><li><a>Home</a></li><li><a>Starlight Courier</a></li><li>Chapter 2</li></ol><h1 id="chapter-heading">Starlight Courier - Chapter 2</h1><div class="reading-content">${pageImgs(5, "data-src")}</div>`, "https://comics.example/series/starlight-courier/chapter-2/");
    expect(chapter.pages).toHaveLength(5);
    expect(chapter.meta.number).toBe("2");
    expect(chapter.meta.title).toBe("Starlight Courier Ch. 2");
  });

  it("reads ts_reader script sources", () => {
    const images = JSON.stringify(Array.from({ length: 7 }, (_, i) => `https://img.example/tsr/${i + 1}.jpg`));
    const r = scan(`<div id="readerarea"></div><script>ts_reader.run({"sources":[{"source":"S1","images":${images}}]});</script>`);
    expect(r.adapter).toBe("TS reader theme");
    expect(r.pages).toHaveLength(7);
  });

  it("applies user site rules first", () => {
    const d = doc(`<div class="odd"><span data-page="https://x.example/p1.jpg"></span><img class="pg" src="https://x.example/p1.jpg"><img class="pg" src="https://x.example/p2.jpg"></div><h2 class="name">Rule Title</h2>`);
    const r = scanDocument(d, { url: "https://www.x.example/read/1", live: false, minPageWidth: 280, rules: [{ id: "r", host: "x.example", pageSelector: "img.pg", chapterSelector: "", titleSelector: "h2.name", seriesSelector: "", tagSelector: "", enabled: true }] });
    expect(r.adapter).toBe("Site rule · x.example");
    expect(r.pages).toHaveLength(2);
    expect(r.meta.title).toBe("Rule Title");
  });
});

describe("metadata scraping", () => {
  it("reads JSON-LD, labelled fields and tag links", () => {
    const r = scan(
      `<html lang="en"><head>
        <title>Read Iron Orchard #7 Online Free - PanelPress</title>
        <meta property="og:site_name" content="PanelPress">
        <meta property="og:image" content="https://panelpress.example/covers/io7.jpg">
        <script type="application/ld+json">{"@context":"https://schema.org","@type":"ComicIssue","name":"Iron Orchard #7","isPartOf":{"@type":"ComicSeries","name":"Iron Orchard"},"author":{"@type":"Person","name":"Ada Ink"},"illustrator":[{"name":"Rio Pen"}],"publisher":{"name":"Orchard House"},"datePublished":"2022-05-19","description":"Roots run deep.","genre":["Mystery","Horror"]}</script>
      </head><body>
        <div class="info"><p><b>Status:</b> Ongoing</p><p><b>Tags:</b> <a href="/tag/robots">Robots</a>, <a href="/tag/farming">Farming</a></p></div>
        <div class="page-list">${pageImgs(4, "src", "https://panelpress.example/io/7")}</div>
      </body></html>`,
      "https://panelpress.example/iron-orchard/issue-7"
    );
    expect(r.meta.series).toBe("Iron Orchard");
    expect(r.meta.number).toBe("7");
    expect(r.meta.numberKind).toBe("issue");
    expect(r.meta.title).toBe("Iron Orchard #7");
    expect(r.meta.writers).toEqual(["Ada Ink"]);
    expect(r.meta.artists).toEqual(["Rio Pen"]);
    expect(r.meta.publisher).toBe("Orchard House");
    expect([r.meta.year, r.meta.month, r.meta.day]).toEqual(["2022", "5", "19"]);
    expect(r.meta.summary).toBe("Roots run deep.");
    expect(r.meta.genres).toEqual(["Mystery", "Horror"]);
    expect(r.meta.tags).toEqual(expect.arrayContaining(["Robots", "Farming"]));
    expect(r.meta.status).toBe("Ongoing");
    expect(r.meta.language).toBe("en");
    expect(r.meta.site).toBe("PanelPress");
    expect(r.meta.coverUrl).toBe("https://panelpress.example/covers/io7.jpg");
  });

  it("marks manga for right-to-left reading and webtoons as long strip", () => {
    const manga = scan(`<h1>Night Market Chapter 3</h1><dl><dt>Genres</dt><dd><a href="/genre/seinen">Seinen</a><a href="/genre/manga">Manga</a></dd></dl>${pageImgs(3)}`);
    expect(manga.meta.manga).toBe("YesAndRightToLeft");
    const webtoon = scan(`<h1>Paper Moon Episode 4</h1><dl><dt>Type</dt><dd>Manhwa</dd></dl>${pageImgs(3)}`);
    expect(webtoon.meta.manga).toBe("Yes");
    expect(webtoon.meta.numberKind).toBe("episode");
  });
});

describe("chapters, files and pagination", () => {
  it("finds a generic chapter list, sorted, without the current page", () => {
    const list = Array.from({ length: 5 }, (_, i) => `<li><a href="/paper-moon/episode-${5 - i}">Episode ${5 - i}</a><span class="date">Jan ${i + 1}</span></li>`).join("");
    const r = scan(`<h1>Paper Moon</h1><ul class="episode-list">${list}</ul>`, "https://toons.example/paper-moon");
    expect(r.chapters.map((c) => c.number)).toEqual([1, 2, 3, 4, 5]);
    expect(r.chapters[0].url).toBe("https://toons.example/paper-moon/episode-1");
    expect(r.isSeriesPage).toBe(true);
  });

  it("finds the site's own comic files and download buttons", () => {
    const d = doc(`<a href="/files/iron-orchard-07.cbz">Iron Orchard 07</a><a href="/get?id=9">Download CBR (12.5 MB)</a><a href="/about.html">About</a><a href="/f/vol1.pdf" download="Iron Orchard Vol 1.pdf">PDF</a>`);
    const files = findFileLinks(d, "https://files.example/series/io");
    expect(files.map((f) => f.ext)).toEqual(["cbz", "cbr", "pdf"]);
    expect(files[1].size).toBe(Math.round(12.5 * 1024 ** 2));
    expect(files[2].name).toBe("Iron Orchard Vol 1.pdf");
  });

  it("finds one-image-per-page reader pagination", () => {
    const d = doc(`<select id="page-select">${Array.from({ length: 6 }, (_, i) => `<option value="/read/io/7/${i + 1}">${i + 1}</option>`).join("")}</select><img src="https://cdn.example/io/7/1.jpg">`);
    expect(findPagination(d, "https://reader.example/read/io/7/1", "https://reader.example/read/io/7/1")).toHaveLength(6);
  });
});
