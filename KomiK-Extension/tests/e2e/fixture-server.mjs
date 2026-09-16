// A tiny local comic site for end-to-end tests. All comics, names and pages are made up; page images
// are generated solid-colour PNGs.
import { createServer } from "node:http";
import { deflateSync } from "node:zlib";
import { zipSync, strToU8 } from "fflate";

const CRC_TABLE = Array.from({ length: 256 }, (_, n) => {
  let c = n;
  for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  return c >>> 0;
});
function crc32(buf) {
  let c = 0xffffffff;
  for (const b of buf) c = CRC_TABLE[(c ^ b) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}
function chunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}
const pngCache = new Map();
export function makePng(width, height, seed) {
  const key = `${width}x${height}:${seed}`;
  if (pngCache.has(key)) return pngCache.get(key);
  const r = (seed * 67) % 255, g = (seed * 131) % 255, b = (seed * 29 + 90) % 255;
  const row = Buffer.alloc(1 + width * 3);
  for (let x = 0; x < width; x++) row.set([r, g, (b + (x >> 4)) % 255], 1 + x * 3);
  const raw = Buffer.concat(Array.from({ length: height }, () => row));
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr.set([8, 2, 0, 0, 0], 8);
  const png = Buffer.concat([Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]), chunk("IHDR", ihdr), chunk("IDAT", deflateSync(raw)), chunk("IEND", Buffer.alloc(0))]);
  pngCache.set(key, png);
  return png;
}

const page = (title, body, head = "") => `<!doctype html><html lang="en"><head><meta charset="utf-8"><title>${title}</title><meta property="og:site_name" content="Fixture Comics">${head}</head><body style="margin:0;background:#222">
<header><img src="/img/logo.png" width="180" height="50" alt="logo"><nav><a href="/">Home</a> <a href="/genre/all">Genres</a></nav></header>
<main style="max-width:820px;margin:0 auto">${body}</main>
<aside class="sidebar"><img src="/img/ad/1.png" width="300" height="250"></aside>
<footer>Fixture Comics</footer></body></html>`;

const lazyChapter = (base) => page(
  "Read Starlight Courier Chapter 15 Online Free - Fixture Comics",
  `<h1>Starlight Courier Chapter 15</h1>
   <div class="info"><p><b>Author:</b> <a href="/author/ada">Ada Ink</a></p><p><b>Artist:</b> Rio Pen</p>
   <p><b>Genres:</b> <a href="/genre/action">Action</a>, <a href="/genre/sci-fi">Sci-fi</a>, <a href="/genre/home">Home</a></p></div>
   <div class="reading-area">${Array.from({ length: 6 }, (_, i) => `<img class="lazyload" src="data:image/gif;base64,R0lGODlhAQABAAAAACw=" data-src="${base}/img/sc15/${String(i + 1).padStart(3, "0")}.png" style="display:block;width:100%;min-height:900px">`).join("")}</div>`,
  `<script type="application/ld+json">{"@context":"https://schema.org","@type":"ComicIssue","name":"Starlight Courier Chapter 15","isPartOf":{"@type":"ComicSeries","name":"Starlight Courier"},"author":{"name":"Ada Ink"},"publisher":{"name":"Orbit Ink"},"datePublished":"2024-03-09","description":"A courier races a solar storm.","keywords":"space, couriers"}</script>
   <script>addEventListener('scroll',()=>document.querySelectorAll('img[data-src]').forEach(i=>{if(i.getBoundingClientRect().top<innerHeight*2){i.src=i.dataset.src;i.removeAttribute('data-src');i.style.minHeight=''}}),{passive:true});</script>`
);

const series = () => page(
  "Paper Moon - Fixture Comics",
  `<div class="post-title"><h1>Paper Moon</h1></div>
   <div class="author-content"><a href="/a/mo">Mo Lee</a></div>
   <div class="genres-content"><a href="/genre/drama">Drama</a><a href="/genre/manhwa">Manhwa</a></div>
   <div class="summary__content"><p>Lanterns over a quiet harbor.</p></div>
   <ul class="main version-chap">${[3, 2, 1].map((n) => `<li class="wp-manga-chapter"><a href="/paper-moon/chapter-${n}/">Chapter ${n}<span class="chapter-release-date">1${n}/04/2025</span></a></li>`).join("")}</ul>`
);

// A gallery: every page shown as a small preview linking to its own reader page. Page 5's full-size image
// is somewhere the naming rules can't guess, so it has to come from that page's reader page.
const GALLERY_PAGES = 9;
const ODD_PAGE = 5;

const gallery = () => page(
  "[Ada Ink] Night Market Stories (Winter Event) [English] - Fixture Comics",
  `<h1>[Ada Ink] Night Market Stories (Winter Event) [English]</h1>
   <div class="info"><p><b>Artist:</b> <a href="/artist/ada">Ada Ink</a></p>
   <p><b>Tags:</b> <a href="/tag/romance">Romance</a>, <a href="/tag/slice-of-life">Slice of life</a></p>
   <p><b>Released:</b> 12/04/2025</p><p><b>Pages:</b> ${GALLERY_PAGES}</p></div>
   <div class="thumb-container">${Array.from({ length: GALLERY_PAGES }, (_, i) =>
     `<a class="gallerythumb" href="/g/9912/${i + 1}/"><img src="/thumbs/9912/${i + 1}t.jpg" width="200" height="290"></a>`
   ).join("")}</div>`
);

const galleryPage = (n) => page(
  `Night Market Stories - page ${n}`,
  `<section id="image-container"><a href="/g/9912/${n + 1}/"><img src="${n === ODD_PAGE ? "/alt/9912/five-full.jpg" : `/images/9912/${n}.jpg`}" width="1200" height="1700"></a></section>`
);

// A second gallery whose full-size pages live somewhere no naming rule could guess: the only way to
// find them is to open one page and learn the pattern from it.
const HIDDEN_ID = 7745;
const HIDDEN_PAGES = 9;

const hiddenGallery = () => page(
  "[Rio Pen] Lantern Hours [English] - Fixture Comics",
  `<h1>[Rio Pen] Lantern Hours [English]</h1>
   <div class="thumb-container">${Array.from({ length: HIDDEN_PAGES }, (_, i) =>
     `<a class="gallerythumb" href="/g/${HIDDEN_ID}/${i + 1}/"><img src="/thumbs/${HIDDEN_ID}/${i + 1}t.jpg" width="200" height="290"></a>`
   ).join("")}</div>`
);

const hiddenGalleryPage = (n) => page(
  `Lantern Hours - page ${n}`,
  `<section id="image-container"><img src="/pages-full/${HIDDEN_ID}/${n}.webp" width="1400" height="2000"></section>`
);

const seriesChapter = (n) => page(
  `Paper Moon - Chapter ${n} - Fixture Comics`,
  `<h1 id="chapter-heading">Paper Moon - Chapter ${n}</h1><div class="reading-content">${Array.from({ length: 3 + n }, (_, i) => `<div class="page-break"><img class="wp-manga-chapter-img" src="/img/pm${n}/${i + 1}.png"></div>`).join("")}</div>`
);

const hotlink = (base) => page(
  "Iron Orchard #7 - Fixture Comics",
  `<h1>Iron Orchard #7</h1><div class="comic-pages">${Array.from({ length: 4 }, (_, i) => `<img src="${base}/protected/io7/${i + 1}.png" width="700" height="1050">`).join("")}</div>`
);

const files = () => page(
  "Night Market downloads - Fixture Comics",
  `<h1>Night Market Vol. 1</h1><p><a class="btn" href="/files/night-market-v01.cbz">Download CBZ</a></p>`
);

function sampleCbz() {
  return Buffer.from(zipSync({ "001.png": makePng(400, 600, 1), "002.png": makePng(400, 600, 2), "ComicInfo.xml": strToU8("<ComicInfo><Title>Old title</Title></ComicInfo>") }, { level: 0 }));
}

export function startFixtureServer() {
  return new Promise((resolve) => {
    // Lets a test check how many single-page reader pages were opened.
    const hits = { hiddenReaderPages: 0 };
    const server = createServer((req, res) => {
      const url = new URL(req.url, "http://localhost");
      const base = `http://${req.headers.host}`;
      const send = (status, type, body) => {
        res.writeHead(status, { "content-type": type, "cache-control": "no-store" });
        res.end(body);
      };
      const p = url.pathname;
      if (p === "/chapter/lazy") return send(200, "text/html", lazyChapter(base));
      if (p === "/paper-moon/") return send(200, "text/html", series());
      const ch = /^\/paper-moon\/chapter-(\d+)\/$/.exec(p);
      if (ch) return send(200, "text/html", seriesChapter(Number(ch[1])));
      if (p === "/hotlink/iron-orchard-7") return send(200, "text/html", hotlink(base));
      if (p === "/g/9912/" || p === "/g/9912") return send(200, "text/html", gallery());
      const galleryReader = /^\/g\/9912\/(\d+)\/$/.exec(p);
      if (galleryReader) return send(200, "text/html", galleryPage(Number(galleryReader[1])));
      const thumb = /^\/thumbs\/9912\/(\d+)t\.jpg$/.exec(p);
      if (thumb) return send(200, "image/png", makePng(200, 290, Number(thumb[1])));
      const full = /^\/images\/9912\/(\d+)\.jpg$/.exec(p);
      if (full) {
        // The odd page out is only reachable through its reader page.
        if (Number(full[1]) === ODD_PAGE) return send(404, "text/html", "<!doctype html><h1>Not found</h1>");
        return send(200, "image/png", makePng(1200, 1700, Number(full[1])));
      }
      if (p === "/alt/9912/five-full.jpg") return send(200, "image/png", makePng(1200, 1700, ODD_PAGE));
      if (p === "/__stats") return send(200, "application/json", JSON.stringify(hits));
      if (p === `/g/${HIDDEN_ID}/` || p === `/g/${HIDDEN_ID}`) return send(200, "text/html", hiddenGallery());
      const hiddenReader = new RegExp(`^/g/${HIDDEN_ID}/(\\d+)/$`).exec(p);
      if (hiddenReader) {
        hits.hiddenReaderPages++;
        return send(200, "text/html", hiddenGalleryPage(Number(hiddenReader[1])));
      }
      const hiddenThumb = new RegExp(`^/thumbs/${HIDDEN_ID}/(\\d+)t\\.jpg$`).exec(p);
      if (hiddenThumb) return send(200, "image/png", makePng(200, 290, Number(hiddenThumb[1])));
      const hiddenFull = new RegExp(`^/pages-full/${HIDDEN_ID}/(\\d+)\\.webp$`).exec(p);
      if (hiddenFull) return send(200, "image/png", makePng(1400, 2000, Number(hiddenFull[1])));
      if (p === "/files") return send(200, "text/html", files());
      if (p === "/files/night-market-v01.cbz") {
        res.writeHead(200, { "content-type": "application/octet-stream", "content-disposition": 'attachment; filename="night-market-v01.cbz"' });
        return res.end(sampleCbz());
      }
      if (p === "/img/logo.png") return send(200, "image/png", makePng(180, 50, 99));
      if (p.startsWith("/img/ad/")) return send(200, "image/png", makePng(300, 250, 77));
      const img = /^\/img\/[a-z0-9]+\/0*(\d+)\.png$/.exec(p);
      if (img) return send(200, "image/png", makePng(800, 1200, Number(img[1])));
      const prot = /^\/protected\/io7\/(\d+)\.png$/.exec(p);
      if (prot) {
        const ref = req.headers.referer ?? "";
        if (!ref.includes("/hotlink/")) return send(403, "text/html", "<!doctype html><h1>Hotlinking not allowed</h1>");
        return send(200, "image/png", makePng(700, 1050, 40 + Number(prot[1])));
      }
      send(404, "text/html", "<!doctype html><h1>Not found</h1>");
    });
    server.listen(0, "127.0.0.1", () => resolve({ server, origin: `http://127.0.0.1:${server.address().port}` }));
  });
}
