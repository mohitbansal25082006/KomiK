using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Komik.Models;
using Komik.Services;

namespace Komik.Tests;

/// <summary>
/// Embedded ComicInfo.xml metadata (v1.2.0): reading it, indexing it with tags, never overwriting the
/// user's own edits, picking up new downloads and keeping it through CBZ conversion.
/// </summary>
public static class EmbeddedMetadataTests
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private const string FullComicInfo = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ComicInfo xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <Title>Solo Leveling Vol. 2 Ch. 15</Title>
  <Series>Solo Leveling</Series>
  <Number>015</Number>
  <Volume>2</Volume>
  <Summary>The weakest hunter gets a second chance.</Summary>
  <Year>2021</Year>
  <Month>3</Month>
  <Day>14</Day>
  <Writer>Chugong</Writer>
  <Penciller>Jang Sung-rak; DUBU</Penciller>
  <Inker>DUBU</Inker>
  <Publisher>D&amp;C Media</Publisher>
  <Genre>Action, Fantasy, action</Genre>
  <Tags>Dungeons, #Leveling,  Fantasy ,System</Tags>
  <Web>https://example.com/solo-leveling/15</Web>
  <LanguageISO>en</LanguageISO>
  <Manga>YesAndRightToLeft</Manga>
</ComicInfo>";

    public static async Task RunAllAsync(Func<string, Func<Task>, Task> runTest, string tempDir)
    {
        await runTest("ComicInfoReader Parses Metadata, Credits, Dates and Tags", () =>
        {
            var info = ComicInfoReader.Parse(FullComicInfo) ?? throw new Exception("Valid ComicInfo.xml was not parsed");
            Expect(info.Title, "Solo Leveling Vol. 2 Ch. 15", "Title");
            Expect(info.Series, "Solo Leveling", "Series");
            Expect(info.Number, "15", "Number");
            Expect(info.Volume, "2", "Volume");
            Expect(info.ReleaseDate, "2021-03-14", "ReleaseDate");
            Expect(info.Publisher, "D&C Media", "Publisher");
            Expect(string.Join("|", info.Writers), "Chugong", "Writers");
            Expect(string.Join("|", info.Artists), "Jang Sung-rak|DUBU", "Artists");
            Expect(string.Join("|", info.Tags), "Action|Fantasy|Dungeons|Leveling|System", "Tags");

            var meta = info.ToMetadata(7, info.Title!);
            if (meta.ComicId != 7 || meta.SeriesName != "Solo Leveling" || meta.IssueNumber != "15" || meta.Artists != "Jang Sung-rak, DUBU")
                throw new Exception("ToMetadata mapped fields incorrectly");

            if (ComicInfoReader.Parse("<ComicInfo><Title>Broken") != null) throw new Exception("Malformed XML should be ignored");
            if (ComicInfoReader.Parse("<Book><Title>Other</Title></Book>") != null) throw new Exception("A different root element should be ignored");
            const string dtd = "<?xml version=\"1.0\"?><!DOCTYPE x [<!ENTITY e SYSTEM \"file:///c:/windows/win.ini\">]><ComicInfo><Title>&e;</Title></ComicInfo>";
            if (ComicInfoReader.Parse(dtd) != null) throw new Exception("DTDs must be rejected");

            var partial = ComicInfoReader.Parse("<ComicInfo><Year>1999</Year><Day>4</Day><Number>1.50</Number></ComicInfo>")!;
            Expect(partial.ReleaseDate, "1999", "Year-only date");
            Expect(partial.Number, "1.5", "Decimal number");
            return Task.CompletedTask;
        });

        await runTest("ComicInfo Title Resolution Keeps the Series in the Library Title", () =>
        {
            var named = ComicInfoReader.Parse("<ComicInfo><Title>Saga #12</Title><Series>Saga</Series></ComicInfo>")!;
            Expect(named.ResolveLibraryTitle("download-12345"), "Saga #12", "Title that names the series");

            var bare = ComicInfoReader.Parse("<ComicInfo><Title>The Fight</Title><Series>Saga</Series></ComicInfo>")!;
            Expect(bare.ResolveLibraryTitle("Saga 012"), "Saga 012", "Bare title keeps a file name holding the series");
            Expect(bare.ResolveLibraryTitle("12345"), "Saga - The Fight", "Bare title with a meaningless file name");

            var noTitle = ComicInfoReader.Parse("<ComicInfo><Series>Saga</Series></ComicInfo>")!;
            Expect(noTitle.ResolveLibraryTitle("Saga 001"), "Saga 001", "Missing title");
            return Task.CompletedTask;
        });

        await runTest("Scanner Indexes Embedded Metadata and Tags from CBZ, CB7 and Folders", async () =>
        {
            string root = Path.Combine(tempDir, "EmbeddedRoot");
            Directory.CreateDirectory(root);

            string cbz = Path.Combine(root, "Solo Leveling Vol. 2 Ch. 15.cbz");
            WriteCbz(cbz, FullComicInfo, infoFolder: null);

            string nested = Path.Combine(root, "nested-info.cbz");
            WriteCbz(nested, "<ComicInfo><Title>Nested Book #3</Title><Series>Nested Book</Series><Number>3</Number><Tags>Mystery</Tags></ComicInfo>", infoFolder: "Nested Book 3/");

            string cb7 = Path.Combine(root, "Berserk Vol. 41.cb7");
            using (var fs = File.Create(cb7))
            using (var writer = new SharpCompress.Writers.SevenZip.SevenZipWriter(fs, new SharpCompress.Writers.SevenZip.SevenZipWriterOptions()))
            {
                using (var page = new MemoryStream(Png)) writer.Write("001.png", page, DateTime.UtcNow);
                using var xml = new MemoryStream(Encoding.UTF8.GetBytes("<ComicInfo><Series>Berserk</Series><Volume>41</Volume><Writer>Kentaro Miura</Writer><Genre>Dark Fantasy</Genre></ComicInfo>"));
                writer.Write("ComicInfo.xml", xml, DateTime.UtcNow);
            }

            string folder = Path.Combine(root, "Folder Comic 7");
            Directory.CreateDirectory(folder);
            await File.WriteAllBytesAsync(Path.Combine(folder, "001.png"), Png);
            await File.WriteAllTextAsync(Path.Combine(folder, "ComicInfo.xml"), "<ComicInfo><Series>Folder Comic</Series><Number>7</Number><Tags>Indie</Tags></ComicInfo>");

            string plain = Path.Combine(root, "No Metadata 1.cbz");
            WriteCbz(plain, comicInfo: null, infoFolder: null);

            using var repo = new LibraryRepository(Path.Combine(tempDir, "embedded_scan.db"));
            var scanner = new LibraryScannerService(repo, thumbnailService: new ThumbnailService(Path.Combine(tempDir, "embedded_thumbs")));
            await scanner.ScanFolderAsync(root);

            var comics = await repo.GetComicsAsync();
            if (comics.Count != 5) throw new Exception($"Expected 5 comics, got {comics.Count}");

            var solo = await repo.GetComicByPathAsync(cbz) ?? throw new Exception("CBZ was not indexed");
            if (solo.PageCount != 2) throw new Exception($"ComicInfo.xml must not count as a page (got {solo.PageCount})");
            Expect(solo.Title, "Solo Leveling Vol. 2 Ch. 15", "CBZ library title");
            var soloMeta = await repo.GetMetadataForComicAsync(solo.Id) ?? throw new Exception("CBZ metadata missing");
            Expect(soloMeta.SeriesName, "Solo Leveling", "CBZ series");
            Expect(soloMeta.IssueNumber, "15", "CBZ issue");
            Expect(soloMeta.Writers, "Chugong", "CBZ writers");
            Expect(soloMeta.Publisher, "D&C Media", "CBZ publisher");
            Expect(soloMeta.ReleaseDate, "2021-03-14", "CBZ release date");
            Expect(soloMeta.Summary, "The weakest hunter gets a second chance.", "CBZ summary");
            var soloTags = (await repo.GetTagsForComicAsync(solo.Id)).OrderBy(t => t).ToList();
            Expect(string.Join("|", soloTags), "Action|Dungeons|Fantasy|Leveling|System", "CBZ tags");

            var nestedComic = await repo.GetComicByPathAsync(nested) ?? throw new Exception("Nested CBZ was not indexed");
            Expect(nestedComic.Title, "Nested Book #3", "Nested ComicInfo title");
            Expect(string.Join("|", await repo.GetTagsForComicAsync(nestedComic.Id)), "Mystery", "Nested tags");

            var berserk = await repo.GetComicByPathAsync(cb7) ?? throw new Exception("CB7 was not indexed");
            var berserkMeta = await repo.GetMetadataForComicAsync(berserk.Id) ?? throw new Exception("CB7 metadata missing");
            Expect(berserkMeta.Writers, "Kentaro Miura", "CB7 writer");
            Expect(berserk.Title, "Berserk Vol. 41", "CB7 keeps its file name without an embedded title");
            Expect(string.Join("|", await repo.GetTagsForComicAsync(berserk.Id)), "Dark Fantasy", "CB7 tags");

            var folderComic = await repo.GetComicByPathAsync(folder) ?? throw new Exception("Folder comic was not indexed");
            Expect((await repo.GetMetadataForComicAsync(folderComic.Id))?.IssueNumber, "7", "Folder issue");

            var plainComic = await repo.GetComicByPathAsync(plain) ?? throw new Exception("Plain CBZ was not indexed");
            if (await repo.GetMetadataForComicAsync(plainComic.Id) != null) throw new Exception("A comic without ComicInfo.xml must not get metadata");
        });

        await runTest("Embedded Metadata Import Never Overwrites Edits and Runs Once", async () =>
        {
            string root = Path.Combine(tempDir, "ImportRoot");
            Directory.CreateDirectory(root);
            string edited = Path.Combine(root, "edited.cbz");
            string fresh = Path.Combine(root, "fresh.cbz");
            WriteCbz(edited, "<ComicInfo><Title>Embedded Title #1</Title><Series>Embedded</Series><Tags>FromFile</Tags></ComicInfo>", null);
            WriteCbz(fresh, "<ComicInfo><Title>Fresh Series #2</Title><Series>Fresh Series</Series><Number>2</Number><Tags>New Tag</Tags></ComicInfo>", null);

            using var repo = new LibraryRepository(Path.Combine(tempDir, "embedded_import.db"));
            // Comics indexed by an older Komik: rows exist, embedded metadata never read.
            long editedId = await repo.InsertComicAsync(new ComicEntity { FilePath = edited, Title = "edited", Format = ComicSourceType.ZipArchive, PageCount = 2 });
            long freshId = await repo.InsertComicAsync(new ComicEntity { FilePath = fresh, Title = "fresh", Format = ComicSourceType.ZipArchive, PageCount = 2 });
            await repo.SaveComicMetadataAsync(new ComicMetadataEntity { ComicId = editedId, Title = "My Own Title", SeriesName = "My Series" });

            var scanner = new LibraryScannerService(repo, thumbnailService: new ThumbnailService(Path.Combine(tempDir, "embedded_import_thumbs")));
            int updated = await scanner.ImportEmbeddedMetadataAsync();
            if (updated != 1) throw new Exception($"Expected 1 comic updated, got {updated}");

            Expect((await repo.GetMetadataForComicAsync(editedId))?.SeriesName, "My Series", "User metadata kept");
            if ((await repo.GetTagsForComicAsync(editedId)).Count != 0) throw new Exception("An edited comic must not be touched");

            var freshComic = await repo.GetComicByPathAsync(fresh);
            Expect(freshComic!.Title, "Fresh Series #2", "Backfilled title");
            Expect((await repo.GetMetadataForComicAsync(freshId))?.IssueNumber, "2", "Backfilled issue");
            Expect(string.Join("|", await repo.GetTagsForComicAsync(freshId)), "New Tag", "Backfilled tags");

            await repo.DeleteComicMetadataAsync(freshId);
            if (await scanner.ImportEmbeddedMetadataAsync() != 0) throw new Exception("The import must only run once per library");
            if (await scanner.ImportEmbeddedMetadataAsync(force: true) != 1) throw new Exception("A forced import should run again");
        });

        await runTest("New Downloads in Watched Folders Are Indexed, Removed Comics Stay Out", async () =>
        {
            string root = Path.Combine(tempDir, "DownloadsKomiK");
            Directory.CreateDirectory(root);
            using var repo = new LibraryRepository(Path.Combine(tempDir, "embedded_downloads.db"));
            var scanner = new LibraryScannerService(repo, thumbnailService: new ThumbnailService(Path.Combine(tempDir, "embedded_downloads_thumbs")));
            await scanner.ScanFolderAsync(root);

            string first = Path.Combine(root, "Series A", "Series A Ch. 1.cbz");
            Directory.CreateDirectory(Path.GetDirectoryName(first)!);
            WriteCbz(first, "<ComicInfo><Title>Series A Ch. 1</Title><Series>Series A</Series><Number>1</Number><Tags>Romance</Tags></ComicInfo>", null);
            string partial = Path.Combine(root, "Series A", "Series A Ch. 2.cbz.crdownload");
            await File.WriteAllBytesAsync(partial, new byte[] { 1, 2, 3 });

            if (await scanner.IndexNewSourcesAsync(new[] { first, partial }) != 1) throw new Exception("Expected exactly the finished download to be indexed");
            if (await scanner.IndexNewSourcesAsync(new[] { first }) != 0) throw new Exception("A known comic must not be indexed twice");

            var comic = await repo.GetComicByPathAsync(first) ?? throw new Exception("Download not indexed");
            Expect(string.Join("|", await repo.GetTagsForComicAsync(comic.Id)), "Romance", "Download tags");

            await repo.RemoveComicAsync(comic.Id);
            if (await scanner.IndexNewSourcesAsync(new[] { first }) != 0) throw new Exception("A removed comic must stay out");

            string second = Path.Combine(root, "Series A", "Series A Ch. 3.cbz");
            WriteCbz(second, "<ComicInfo><Series>Series A</Series><Number>3</Number></ComicInfo>", null);
            if (await scanner.SyncWatchedFoldersQuietlyAsync() < 1) throw new Exception("Startup sync should pick up comics added while closed");
            if (await repo.GetComicByPathAsync(second) == null) throw new Exception("Startup sync did not index the new comic");
            if (await repo.GetComicByPathAsync(first) != null) throw new Exception("Startup sync must not bring back a removed comic");
        });

        await runTest("A Comic Downloaded Again Under the Same Name Gets All Its New Tags", async () =>
        {
            string root = Path.Combine(tempDir, "Redownload");
            Directory.CreateDirectory(root);
            using var repo = new LibraryRepository(Path.Combine(tempDir, "embedded_redownload.db"));
            var scanner = new LibraryScannerService(repo, thumbnailService: new ThumbnailService(Path.Combine(tempDir, "embedded_redownload_thumbs")));
            await repo.AddWatchedFolderAsync(root);

            // First download: an older downloader kept only 29 tags and a genre.
            string file = Path.Combine(root, "Long Tag List.cbz");
            var firstTags = Enumerable.Range(1, 29).Select(i => $"First Tag {(char)('a' + i % 26)}{i}").ToList();
            WriteCbz(file, $"<ComicInfo><Title>Long Tag List</Title><Genre>Doujinshi</Genre><Tags>{string.Join(", ", firstTags)}</Tags></ComicInfo>", null);
            if (await scanner.IndexNewSourcesAsync(new[] { file }) != 1) throw new Exception("First download not indexed");
            var comic = await repo.GetComicByPathAsync(file) ?? throw new Exception("Comic missing");
            if ((await repo.GetTagsForComicAsync(comic.Id)).Count != 30) throw new Exception("First download should carry 30 tags");

            // Downloaded again under the same name, now with 80 tags: every one must reach the library.
            var allTags = firstTags.Concat(Enumerable.Range(1, 51).Select(i => $"Later Tag {(char)('a' + i % 26)}{i}")).ToList();
            WriteCbz(file, $"<ComicInfo><Title>Long Tag List</Title><Genre>Doujinshi</Genre><Tags>{string.Join(", ", allTags)}</Tags></ComicInfo>", null);
            File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddMinutes(5));

            var (added, refreshed) = await scanner.IndexOrRefreshSourcesAsync(new[] { file });
            if (added != 0 || refreshed != 1) throw new Exception($"Expected the known comic to be refreshed, got added={added} refreshed={refreshed}");
            int stored = (await repo.GetTagsForComicAsync(comic.Id)).Count;
            if (stored != 81) throw new Exception($"Expected all 81 tags (80 + genre) after the new download, got {stored}");

            // Nothing changed since: a second pass leaves it alone.
            if ((await scanner.IndexOrRefreshSourcesAsync(new[] { file })).Refreshed != 0) throw new Exception("An unchanged file must not be re-read");

            // A file replaced while Komik was closed is caught by the startup sync as well.
            WriteCbz(file, $"<ComicInfo><Title>Long Tag List</Title><Genre>Doujinshi</Genre><Tags>{string.Join(", ", allTags)}, Closed App Tag</Tags></ComicInfo>", null);
            File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddMinutes(10));
            await scanner.SyncWatchedFoldersQuietlyAsync();
            if (!(await repo.GetTagsForComicAsync(comic.Id)).Contains("Closed App Tag")) throw new Exception("Startup sync should read a file replaced while Komik was closed");
        });

        await runTest("CBZ Conversion Keeps the Embedded ComicInfo.xml", async () =>
        {
            string source = Path.Combine(tempDir, "Convert Me 5.cb7");
            using (var fs = File.Create(source))
            using (var writer = new SharpCompress.Writers.SevenZip.SevenZipWriter(fs, new SharpCompress.Writers.SevenZip.SevenZipWriterOptions()))
            {
                using (var page = new MemoryStream(Png)) writer.Write("001.png", page, DateTime.UtcNow);
                using var xml = new MemoryStream(Encoding.UTF8.GetBytes("<ComicInfo><Series>Convert Me</Series><Number>5</Number><Tags>Kept</Tags></ComicInfo>"));
                writer.Write("ComicInfo.xml", xml, DateTime.UtcNow);
            }

            string output = await new FormatConversionService().ConvertToCbzAsync(source, Path.Combine(tempDir, "Convert Me 5.cbz"));
            var info = ComicInfoReader.TryRead(output) ?? throw new Exception("Converted CBZ lost ComicInfo.xml");
            Expect(info.Number, "5", "Converted number");
            Expect(string.Join("|", info.Tags), "Kept", "Converted tags");
            using var loaded = await new ZipComicLoader().LoadAsync(output);
            if (loaded.PageCount != 1) throw new Exception($"Converted CBZ should have 1 page, got {loaded.PageCount}");
        });
    }

    private static void WriteCbz(string path, string? comicInfo, string? infoFolder)
    {
        using var stream = new FileStream(path, FileMode.Create);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var name in new[] { "001.png", "002.png" })
        {
            var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
            using var s = entry.Open();
            s.Write(Png);
        }
        if (comicInfo != null)
        {
            var info = archive.CreateEntry((infoFolder ?? string.Empty) + "ComicInfo.xml");
            using var s = info.Open();
            s.Write(Encoding.UTF8.GetBytes(comicInfo));
        }
    }

    private static void Expect(string? actual, string? expected, string what)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new Exception($"{what}: expected '{expected}', got '{actual}'");
    }
}
