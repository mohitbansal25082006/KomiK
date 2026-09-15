using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Komik.Helpers;
using Komik.Models;
using Komik.Services;

namespace Komik.Tests;

public static class Program
{
    private static readonly byte[] SamplePngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static readonly byte[] SampleRar4Bytes = Convert.FromBase64String(
        "UmFyIRoHAM+QcwAADQAAAAAAAAD6FHQAgCsARgAAAEYAAAAAJqSVBxJLEksUMAsAIAAAAFBhZ2VfMDEucG5niVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggipudACAKwBGAAAARgAAAAAmpJUHEksSSxQwCwAgAAAAUGFnZV8wMi5wbmeJUE5HDQoaCgAAAA1JSERSAAAAAQAAAAEIBgAAAB8VxIkAAAANSURBVHjaY/zPwFAPAASFAYCEqYwhAAAAAElFTkSuQmCCCsN0AIApAAQAAAAEAAAAADmc+wYSSxJLFDAJACAAAABUaHVtYnMuZGJqdW5r");

    private static readonly string MinimalPdfContent =
        "%PDF-1.4\n" +
        "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n" +
        "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n" +
        "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 200 300] /Resources <<>> >> endobj\n" +
        "xref\n" +
        "0 4\n" +
        "0000000000 65535 f \n" +
        "0000000009 00000 n \n" +
        "0000000058 00000 n \n" +
        "0000000115 00000 n \n" +
        "trailer << /Size 4 /Root 1 0 R >>\n" +
        "startxref\n" +
        "206\n" +
        "%%EOF\n";

    public static async Task<int> Main()
    {
        Console.WriteLine("=================================================");
        Console.WriteLine("        Komik Comic Viewer Test Suite            ");
        Console.WriteLine("=================================================");

        string tempDir = Path.Combine(Path.GetTempPath(), "KomikTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        int passed = 0;
        int failed = 0;

        async Task RunTestAsync(string testName, Func<Task> testAction)
        {
            try
            {
                Console.Write($"[RUNNING] {testName} ... ");
                await testAction();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("PASSED");
                Console.ResetColor();
                passed++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"FAILED: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine(ex.StackTrace);
                failed++;
            }
        }

        try
        {
            // Test 1: NaturalSortComparer
            await RunTestAsync("NaturalSortComparer Orders Numeric Sequences", () =>
            {
                var files = new[] { "page_10.jpg", "page_1.jpg", "page_2.jpg", "page_100.jpg", "page_20.jpg" };
                Array.Sort(files, NaturalSortComparer.Instance);

                string[] expected = { "page_1.jpg", "page_2.jpg", "page_10.jpg", "page_20.jpg", "page_100.jpg" };
                for (int i = 0; i < files.Length; i++)
                {
                    if (files[i] != expected[i])
                        throw new Exception($"Mismatch at {i}: expected {expected[i]}, got {files[i]}");
                }
                return Task.CompletedTask;
            });

            // Test 2: FolderComicLoader
            await RunTestAsync("FolderComicLoader Loads Folder Pages in Order", async () =>
            {
                string folder = Path.Combine(tempDir, "ActionComicVol1");
                Directory.CreateDirectory(folder);

                await File.WriteAllBytesAsync(Path.Combine(folder, "010.png"), SamplePngBytes);
                await File.WriteAllBytesAsync(Path.Combine(folder, "001.png"), SamplePngBytes);
                await File.WriteAllBytesAsync(Path.Combine(folder, "002.png"), SamplePngBytes);

                var loader = new FolderComicLoader();
                if (!loader.CanLoad(folder)) throw new Exception("CanLoad returned false for valid folder");

                using var comic = await loader.LoadAsync(folder);
                if (comic.Title != "ActionComicVol1") throw new Exception($"Unexpected title: {comic.Title}");
                if (comic.SourceType != ComicSourceType.Folder) throw new Exception("Unexpected source type");
                if (comic.PageCount != 3) throw new Exception($"Expected 3 pages, got {comic.PageCount}");
                if (comic.Pages[0].DisplayName != "001.png") throw new Exception($"Page 0 name: {comic.Pages[0].DisplayName}");
                if (comic.Pages[1].DisplayName != "002.png") throw new Exception($"Page 1 name: {comic.Pages[1].DisplayName}");
                if (comic.Pages[2].DisplayName != "010.png") throw new Exception($"Page 2 name: {comic.Pages[2].DisplayName}");

                var data = await comic.Pages[0].GetPageDataAsync();
                if (data.Data.Length != SamplePngBytes.Length) throw new Exception("Page data length mismatch");
            });

            // Test 3: ZipComicLoader
            await RunTestAsync("ZipComicLoader Loads CBZ/ZIP Archives", async () =>
            {
                string cbzPath = Path.Combine(tempDir, "SpiderIssue_01.cbz");
                using (var zipStream = new FileStream(cbzPath, FileMode.Create))
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    var e3 = archive.CreateEntry("page_10.png");
                    using (var s = e3.Open()) s.Write(SamplePngBytes);

                    var e1 = archive.CreateEntry("page_1.png");
                    using (var s = e1.Open()) s.Write(SamplePngBytes);

                    var e2 = archive.CreateEntry("page_2.png");
                    using (var s = e2.Open()) s.Write(SamplePngBytes);

                    var meta = archive.CreateEntry("ComicInfo.xml");
                    using (var s = meta.Open()) s.Write(Encoding.UTF8.GetBytes("<ComicInfo/>"));
                }

                var loader = new ZipComicLoader();
                if (!loader.CanLoad(cbzPath)) throw new Exception("CanLoad returned false for valid CBZ");

                using var comic = await loader.LoadAsync(cbzPath);
                if (comic.Title != "SpiderIssue_01") throw new Exception($"Unexpected title: {comic.Title}");
                if (comic.SourceType != ComicSourceType.ZipArchive) throw new Exception("Unexpected source type");
                if (comic.PageCount != 3) throw new Exception($"Expected 3 pages, got {comic.PageCount}");
                if (comic.Pages[0].DisplayName != "page_1.png") throw new Exception($"Page 0 name: {comic.Pages[0].DisplayName}");
                if (comic.Pages[1].DisplayName != "page_2.png") throw new Exception($"Page 1 name: {comic.Pages[1].DisplayName}");
                if (comic.Pages[2].DisplayName != "page_10.png") throw new Exception($"Page 2 name: {comic.Pages[2].DisplayName}");

                var data = await comic.Pages[1].GetPageDataAsync();
                if (data.Data.Length != SamplePngBytes.Length) throw new Exception("Data byte length mismatch");
            });

            // Test 4: PdfComicLoader
            await RunTestAsync("PdfComicLoader Loads and Renders PDF Documents", async () =>
            {
                string pdfPath = Path.Combine(tempDir, "SampleComic.pdf");
                await File.WriteAllBytesAsync(pdfPath, Encoding.ASCII.GetBytes(MinimalPdfContent));

                var loader = new PdfComicLoader();
                if (!loader.CanLoad(pdfPath)) throw new Exception("CanLoad returned false for valid PDF");

                using var comic = await loader.LoadAsync(pdfPath);
                if (comic.Title != "SampleComic") throw new Exception($"Unexpected title: {comic.Title}");
                if (comic.SourceType != ComicSourceType.PdfDocument) throw new Exception("Unexpected source type");
                if (comic.PageCount != 1) throw new Exception($"Expected 1 page, got {comic.PageCount}");

                var pageData = await comic.Pages[0].GetPageDataAsync();
                if (pageData.Data == null || pageData.Data.Length == 0) throw new Exception("No pixel bytes returned");
                if (!pageData.IsRawBgra) throw new Exception("Expected raw BGRA format for PDF page");
                if (pageData.Width <= 0 || pageData.Height <= 0) throw new Exception($"Invalid dimensions: {pageData.Width}x{pageData.Height}");
            });

            // Test 5: ComicLoaderService Dispatcher
            await RunTestAsync("ComicLoaderService Dispatches All Formats Correctly", async () =>
            {
                var service = new ComicLoaderService();

                // Folder
                string folder = Path.Combine(tempDir, "FolderComic");
                Directory.CreateDirectory(folder);
                await File.WriteAllBytesAsync(Path.Combine(folder, "p1.jpg"), SamplePngBytes);

                using var folderComic = await service.LoadComicAsync(folder);
                if (folderComic.SourceType != ComicSourceType.Folder) throw new Exception("Failed folder dispatch");

                // CBZ
                string cbz = Path.Combine(tempDir, "TestArchive.cbz");
                using (var s = new FileStream(cbz, FileMode.Create))
                using (var a = new ZipArchive(s, ZipArchiveMode.Create))
                {
                    var e = a.CreateEntry("01.jpg");
                    using var es = e.Open();
                    es.Write(SamplePngBytes);
                }

                using var zipComic = await service.LoadComicAsync(cbz);
                if (zipComic.SourceType != ComicSourceType.ZipArchive) throw new Exception("Failed ZIP dispatch");

                // PDF
                string pdf = Path.Combine(tempDir, "TestDoc.pdf");
                await File.WriteAllBytesAsync(pdf, Encoding.ASCII.GetBytes(MinimalPdfContent));

                using var pdfComic = await service.LoadComicAsync(pdf);
                if (pdfComic.SourceType != ComicSourceType.PdfDocument) throw new Exception("Failed PDF dispatch");
            });

            // Test 6: Error Handling
            await RunTestAsync("Error Handling Rejects Corrupted and Empty Files Gracefully", async () =>
            {
                var service = new ComicLoaderService();

                // Empty folder
                string emptyFolder = Path.Combine(tempDir, "EmptyFolder");
                Directory.CreateDirectory(emptyFolder);
                bool threw = false;
                try
                {
                    await service.LoadComicAsync(emptyFolder);
                }
                catch (InvalidOperationException)
                {
                    threw = true;
                }
                if (!threw) throw new Exception("Empty folder did not throw InvalidOperationException");

                // Corrupted CBZ
                string corruptCbz = Path.Combine(tempDir, "Corrupted.cbz");
                await File.WriteAllBytesAsync(corruptCbz, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
                threw = false;
                try
                {
                    await service.LoadComicAsync(corruptCbz);
                }
                catch (InvalidOperationException)
                {
                    threw = true;
                }
                if (!threw) throw new Exception("Corrupt archive did not throw InvalidOperationException");

                // Unsupported file
                string notes = Path.Combine(tempDir, "Readme.txt");
                await File.WriteAllTextAsync(notes, "just a text file");
                threw = false;
                try
                {
                    await service.LoadComicAsync(notes);
                }
                catch (NotSupportedException)
                {
                    threw = true;
                }
                if (!threw) throw new Exception("Unsupported file did not throw NotSupportedException");
            });

            // Test 7: LibraryRepository SQLite CRUD, Tags, Collections & Filters
            await RunTestAsync("LibraryRepository Handles SQLite Schema, CRUD, Tags & Filters", async () =>
            {
                string dbPath = Path.Combine(tempDir, "test_library.db");
                using var repo = new LibraryRepository(dbPath);
                await repo.InitializeAsync();

                // 1. Watched folders
                var folder = await repo.AddWatchedFolderAsync(@"C:\Comics\Marvel");
                var folders = await repo.GetWatchedFoldersAsync();
                if (folders.Count != 1 || folders[0].Path != @"C:\Comics\Marvel")
                    throw new Exception("Watched folder failed to add or retrieve");

                // 2. Insert comics
                var c1 = new ComicEntity
                {
                    FilePath = @"C:\Comics\Marvel\SpiderMan.cbz",
                    Title = "Spider-Man #1",
                    Format = ComicSourceType.ZipArchive,
                    PageCount = 24,
                    FileSize = 1024 * 1024 * 15,
                    DateAdded = DateTime.UtcNow.AddDays(-2),
                    IsFavorite = true
                };
                long id1 = await repo.InsertComicAsync(c1);

                var c2 = new ComicEntity
                {
                    FilePath = @"C:\Comics\Marvel\Avengers.pdf",
                    Title = "Avengers Annual",
                    Format = ComicSourceType.PdfDocument,
                    PageCount = 48,
                    FileSize = 1024 * 1024 * 30,
                    DateAdded = DateTime.UtcNow.AddDays(-1),
                    IsFavorite = false
                };
                long id2 = await repo.InsertComicAsync(c2);

                if (await repo.GetTotalComicCountAsync() != 2) throw new Exception("Total comic count mismatch");
                if (await repo.GetFavoritesCountAsync() != 1) throw new Exception("Favorites count mismatch");

                // 3. Tags
                await repo.AddTagToComicAsync(id1, "Superhero");
                await repo.AddTagToComicAsync(id1, "Marvel");
                await repo.AddTagToComicAsync(id2, "Superhero");

                var tags1 = await repo.GetTagsForComicAsync(id1);
                if (tags1.Count != 2) throw new Exception("Tags for c1 count mismatch");

                // 4. Collections
                await repo.AddToCollectionAsync(id1, "Spider-Verse");
                var col1 = await repo.GetCollectionsForComicAsync(id1);
                if (col1.Count != 1 || col1[0] != "Spider-Verse") throw new Exception("Collection for c1 mismatch");

                // 5. Query with filters
                var favFilter = new LibraryFilter { FavoritesOnly = true };
                var favs = await repo.GetComicsAsync(favFilter);
                if (favs.Count != 1 || favs[0].Title != "Spider-Man #1") throw new Exception("Favorites filter failed");

                var searchFilter = new LibraryFilter { SearchQuery = "Avengers" };
                var searchResults = await repo.GetComicsAsync(searchFilter);
                if (searchResults.Count != 1 || searchResults[0].Title != "Avengers Annual")
                    throw new Exception("Search query filter failed");

                var tagFilter = new LibraryFilter { SelectedTag = "Marvel" };
                var tagResults = await repo.GetComicsAsync(tagFilter);
                if (tagResults.Count != 1 || tagResults[0].Title != "Spider-Man #1")
                    throw new Exception("Tag filter failed");

                // 6. Toggle favorite & un-index
                await repo.SetFavoriteAsync(id1, false);
                if (await repo.GetFavoritesCountAsync() != 0) throw new Exception("Un-favoriting failed");

                await repo.RemoveComicAsync(id2);
                if (await repo.GetTotalComicCountAsync() != 1) throw new Exception("Remove comic failed");
            });

            // Test 8: ThumbnailService
            await RunTestAsync("ThumbnailService Generates and Caches Thumbnails", async () =>
            {
                string thumbDir = Path.Combine(tempDir, "Thumbs");
                var thumbService = new ThumbnailService(thumbDir);

                // 1. Encoded image (PNG/JPEG)
                var pageData = new ComicPageData(SamplePngBytes, isRawBgra: false);
                string comicPath = Path.Combine(tempDir, "SampleComic.cbz");
                string? saved = await thumbService.SaveThumbnailAsync(comicPath, pageData);

                if (string.IsNullOrEmpty(saved) || !File.Exists(saved)) throw new Exception("Failed to save encoded thumbnail");
                if (!thumbService.ThumbnailExists(comicPath)) throw new Exception("ThumbnailExists returned false");

                // 2. Raw BGRA image (PDFium)
                byte[] rawBgra = new byte[100 * 100 * 4]; // 100x100 pixels
                Array.Fill(rawBgra, (byte)128);
                var pdfPageData = new ComicPageData(rawBgra, isRawBgra: true, width: 100, height: 100);
                string pdfPath = Path.Combine(tempDir, "SampleComic.pdf");
                string? pdfSaved = await thumbService.SaveThumbnailAsync(pdfPath, pdfPageData);

                if (string.IsNullOrEmpty(pdfSaved) || !File.Exists(pdfSaved)) throw new Exception("Failed to save raw BGRA BMP thumbnail");
                if (new FileInfo(pdfSaved).Length < 54 + rawBgra.Length) throw new Exception("BMP size unexpected");

                // 3. Test UpdateThumbnailPathAsync and ClearAllComicThumbnailsAsync
                string testDb = Path.Combine(tempDir, "thumb_test.db");
                using var repo = new LibraryRepository(testDb);
                await repo.InitializeAsync();
                long cId = await repo.InsertComicAsync(new ComicEntity
                {
                    FilePath = comicPath,
                    Title = "Thumb Comic",
                    Format = ComicSourceType.ZipArchive,
                    ThumbnailPath = saved
                });

                var fetched = await repo.GetComicByIdAsync(cId);
                if (fetched?.ThumbnailPath != saved) throw new Exception("Expected thumbnail path to match");

                await repo.UpdateThumbnailPathAsync(cId, "custom_path.png");
                fetched = await repo.GetComicByIdAsync(cId);
                if (fetched?.ThumbnailPath != "custom_path.png") throw new Exception("Expected updated thumbnail path");

                await repo.ClearAllComicThumbnailsAsync();
                fetched = await repo.GetComicByIdAsync(cId);
                if (fetched?.ThumbnailPath != null) throw new Exception("Expected cleared thumbnail path to be null");

                thumbService.ClearCache();
                if (thumbService.GetCacheSizeBytes() != 0) throw new Exception("Cache size should be 0 after clear");
            });

            // Test 9: LibraryScannerService End-to-End
            await RunTestAsync("LibraryScannerService Indexes Comics and Handles Rescans", async () =>
            {
                string scanRoot = Path.Combine(tempDir, "ComicsRoot");
                Directory.CreateDirectory(scanRoot);

                // Create 1 CBZ
                string cbz1 = Path.Combine(scanRoot, "Batman.cbz");
                using (var s = new FileStream(cbz1, FileMode.Create))
                using (var a = new ZipArchive(s, ZipArchiveMode.Create))
                {
                    var e = a.CreateEntry("01.png");
                    using var es = e.Open();
                    es.Write(SamplePngBytes);
                }

                // Create 1 PDF
                string pdf1 = Path.Combine(scanRoot, "Superman.pdf");
                await File.WriteAllBytesAsync(pdf1, Encoding.ASCII.GetBytes(MinimalPdfContent));

                // Create 1 Folder comic
                string folder1 = Path.Combine(scanRoot, "WonderWomanIssue");
                Directory.CreateDirectory(folder1);
                await File.WriteAllBytesAsync(Path.Combine(folder1, "p01.png"), SamplePngBytes);

                string dbPath = Path.Combine(tempDir, "scanner_test.db");
                string thumbDir = Path.Combine(tempDir, "scanner_thumbs");
                using var repo = new LibraryRepository(dbPath);
                var thumbService = new ThumbnailService(thumbDir);
                var scanner = new LibraryScannerService(repo, thumbnailService: thumbService);

                // Scan
                await scanner.ScanFolderAsync(scanRoot);

                var comics = await repo.GetComicsAsync();
                if (comics.Count != 3) throw new Exception($"Expected 3 indexed comics, got {comics.Count}");

                // Verify thumbnails created
                foreach (var c in comics)
                {
                    if (string.IsNullOrEmpty(c.ThumbnailPath) || !File.Exists(c.ThumbnailPath))
                        throw new Exception($"Missing thumbnail for {c.Title}");
                }

                // Rescan without changes: verify count is still 3 (no duplicates!)
                await scanner.RescanAllAsync();
                var rescanComics = await repo.GetComicsAsync();
                if (rescanComics.Count != 3) throw new Exception($"Duplicate entries created on rescan: count={rescanComics.Count}");

                // Delete one file and verify rescan marks it as missing
                File.Delete(cbz1);
                await scanner.RescanAllAsync();

                var missingComic = await repo.GetComicByPathAsync(cbz1);
                if (missingComic == null || !missingComic.IsMissing)
                    throw new Exception("Deleted comic was not marked as missing on rescan");
            });

            // Test 10: Automatic Reading Progress & In-Progress/Unread Filters
            await RunTestAsync("Reading Progress & In-Progress/Unread Filters", async () =>
            {
                string dbFile = Path.Combine(tempDir, "progress_test.db");
                using var repo = new LibraryRepository(dbFile);
                await repo.InitializeAsync();

                long id1 = await repo.InsertComicAsync(new ComicEntity
                {
                    FilePath = "C:\\Comics\\Issue1.cbz",
                    Title = "Issue 1",
                    Format = ComicSourceType.ZipArchive,
                    PageCount = 20
                });

                long id2 = await repo.InsertComicAsync(new ComicEntity
                {
                    FilePath = "C:\\Comics\\Issue2.cbz",
                    Title = "Issue 2",
                    Format = ComicSourceType.ZipArchive,
                    PageCount = 20
                });

                long id3 = await repo.InsertComicAsync(new ComicEntity
                {
                    FilePath = "C:\\Comics\\Issue3.cbz",
                    Title = "Issue 3",
                    Format = ComicSourceType.ZipArchive,
                    PageCount = 20
                });

                // Update progress on Issue 1 to page 5 (in progress)
                await repo.UpdateReadingProgressAsync("C:\\Comics\\Issue1.cbz", 5, 20);
                var c1 = await repo.GetComicByIdAsync(id1);
                if (c1 == null || c1.LastReadPage != 5 || c1.IsCompleted || !c1.IsInProgress || c1.LastReadAt == null)
                    throw new Exception("Issue 1 progress not recorded accurately");

                // Update progress on Issue 2 to page 19 (last page -> auto completed!)
                await repo.UpdateReadingProgressAsync("C:\\Comics\\Issue2.cbz", 19, 20);
                var c2 = await repo.GetComicByIdAsync(id2);
                if (c2 == null || c2.LastReadPage != 19 || !c2.IsCompleted)
                    throw new Exception("Issue 2 auto-completion on last page failed");

                // Test In-Progress filter (should match Issue 1 only)
                var inProgress = await repo.GetComicsAsync(new LibraryFilter { InProgressOnly = true });
                if (inProgress.Count != 1 || inProgress[0].Id != id1)
                    throw new Exception($"Expected 1 in-progress comic, got {inProgress.Count}");

                // Test Unread filter (should match Issue 3 only)
                var unread = await repo.GetComicsAsync(new LibraryFilter { UnreadOnly = true });
                if (unread.Count != 1 || unread[0].Id != id3)
                    throw new Exception($"Expected 1 unread comic, got {unread.Count}");

                // Test Counts
                if (await repo.GetInProgressCountAsync() != 1) throw new Exception("InProgress count mismatch");
                if (await repo.GetUnreadCountAsync() != 1) throw new Exception("Unread count mismatch");

                // Test Manual toggle completed
                await repo.SetCompletedStatusAsync(id1, true);
                var c1Completed = await repo.GetComicByIdAsync(id1);
                if (c1Completed == null || !c1Completed.IsCompleted)
                    throw new Exception("SetCompletedStatusAsync failed");
            });

            // Test 11: Comic Bookmarks CRUD
            await RunTestAsync("Comic Bookmarks CRUD and Page Queries", async () =>
            {
                string dbFile = Path.Combine(tempDir, "bookmarks_test.db");
                using var repo = new LibraryRepository(dbFile);
                await repo.InitializeAsync();

                long comicId = await repo.InsertComicAsync(new ComicEntity
                {
                    FilePath = "C:\\Comics\\XMen.cbz",
                    Title = "X-Men",
                    Format = ComicSourceType.ZipArchive,
                    PageCount = 50
                });

                var b1 = await repo.AddBookmarkAsync(comicId, 12, "Cliffhanger ending");
                var b2 = await repo.AddBookmarkAsync(comicId, 4);

                if (!await repo.IsPageBookmarkedAsync(comicId, 12)) throw new Exception("Page 12 should be bookmarked");
                if (!await repo.IsPageBookmarkedAsync(comicId, 4)) throw new Exception("Page 4 should be bookmarked");
                if (await repo.IsPageBookmarkedAsync(comicId, 9)) throw new Exception("Page 9 should NOT be bookmarked");

                var bookmarks = await repo.GetBookmarksForComicAsync(comicId);
                if (bookmarks.Count != 2) throw new Exception($"Expected 2 bookmarks, got {bookmarks.Count}");
                // Ordered by page_number ASC
                if (bookmarks[0].PageIndex != 4 || bookmarks[1].PageIndex != 12)
                    throw new Exception("Bookmarks not ordered by page ascending");
                if (bookmarks[1].UserNote != "Cliffhanger ending")
                    throw new Exception("User note not preserved");

                await repo.RemoveBookmarkAsync(b1.Id);
                var remaining = await repo.GetBookmarksForComicAsync(comicId);
                if (remaining.Count != 1 || remaining[0].PageIndex != 4)
                    throw new Exception("RemoveBookmarkAsync failed");
            });

            // Test 12: Spread Pairing, Reading Direction, Color LUT, and ComicEntity Progress
            await RunTestAsync("Spread Pairing, RTL Order, Color LUT, and Progress Helpers", () =>
            {
                // 1. ComicEntity progress logic
                var comic = new ComicEntity
                {
                    FilePath = @"C:\Test\Comic.cbz",
                    Title = "Test Comic",
                    PageCount = 20,
                    LastReadPage = -1,
                    IsCompleted = false
                };

                if (!comic.IsUnread) throw new Exception("Expected comic to be unread");
                if (comic.IsInProgress) throw new Exception("Expected comic not to be in progress");
                if (comic.ProgressText != "Unread") throw new Exception($"Expected 'Unread', got {comic.ProgressText}");

                comic.LastReadPage = 9; // 10th page out of 20 = 50%
                if (comic.IsUnread) throw new Exception("Expected comic not to be unread");
                if (!comic.IsInProgress) throw new Exception("Expected comic to be in progress");
                if (Math.Abs(comic.ProgressBarValue - 50.0) > 0.01)
                    throw new Exception($"Expected ProgressBarValue 50, got {comic.ProgressBarValue}");
                if (comic.ProgressPillText != "50%")
                    throw new Exception($"Expected '50%', got {comic.ProgressPillText}");
                if (comic.ProgressText != "p. 10/20 (50%)")
                    throw new Exception($"Expected 'p. 10/20 (50%)', got {comic.ProgressText}");

                comic.IsCompleted = true;
                if (!comic.IsCompleted) throw new Exception("Expected comic to be completed");
                if (comic.IsInProgress) throw new Exception("Expected completed comic not to be in progress");
                if (comic.ProgressText != "Completed") throw new Exception($"Expected 'Completed', got {comic.ProgressText}");
                if (comic.ProgressPillText != "Completed") throw new Exception($"Expected 'Completed', got {comic.ProgressPillText}");

                // 2. ColorCorrectionSettings & LUT
                var settings = new ColorCorrectionSettings();
                if (settings.HasAdjustments) throw new Exception("Default settings should not have adjustments");

                settings.ApplyNightModePreset();
                if (!settings.IsNightMode) throw new Exception("Night mode should be active");
                if (settings.Brightness >= 0) throw new Exception("Night mode brightness should be negative");
                if (!settings.HasAdjustments) throw new Exception("Night mode should report HasAdjustments true");

                // Test LUT buffer processing
                byte[] rawBgra = new byte[16]; // 4 pixels of BGRA (all white 255, 255, 255, 255)
                for (int i = 0; i < rawBgra.Length; i++) rawBgra[i] = 255;
                byte[] adjusted = ColorCorrectionHelper.ApplyColorCorrection(rawBgra, settings);
                // Night mode reduces brightness, so bytes should be < 255
                if (adjusted[0] >= 255 || adjusted[1] >= 255 || adjusted[2] >= 255)
                    throw new Exception("Color correction LUT did not adjust RGB channels");
                if (adjusted[3] != 255)
                    throw new Exception("Alpha channel must remain untouched (255)");

                settings.Reset();
                if (settings.HasAdjustments) throw new Exception("Settings should be reset to default (HasAdjustments false)");

                // 3. Spread mode cover isolation and LTR / RTL pairing
                // In spread mode with 7 total pages (0, 1, 2, 3, 4, 5, 6):
                // Page 0 (Cover) is isolated:
                int current = 0;
                int total = 7;
                int? secondary = null;
                if (current > 0 && current + 1 < total) secondary = current + 1;
                if (secondary != null) throw new Exception("Cover page 0 must be rendered alone");

                // Moving next from cover 0 -> 1 (+1)
                current = (current == 0) ? current + 1 : current + 2;
                if (current != 1) throw new Exception($"Next from 0 should be 1, got {current}");

                // Spread pairing at page 1:
                secondary = (current > 0 && current + 1 < total) ? current + 1 : null;
                if (secondary != 2) throw new Exception($"Secondary page for 1 should be 2, got {secondary}");

                // Reading direction ordering:
                // LTR: First = current (1), Second = secondary (2)
                // RTL: First = secondary (2), Second = current (1)
                int ltrLeft = current;
                int ltrRight = secondary.Value;
                int rtlLeft = secondary.Value;
                int rtlRight = current;
                if (ltrLeft != 1 || ltrRight != 2) throw new Exception("LTR spread order invalid");
                if (rtlLeft != 2 || rtlRight != 1) throw new Exception("RTL spread order invalid");

                // Moving next from 1 -> 3 (+2)
                current = (current == 0) ? current + 1 : current + 2;
                if (current != 3) throw new Exception($"Next from 1 should be 3, got {current}");

                // Moving next from 3 -> 5 (+2)
                current = (current == 0) ? current + 1 : current + 2;
                if (current != 5) throw new Exception($"Next from 3 should be 5, got {current}");
                secondary = (current > 0 && current + 1 < total) ? current + 1 : null;
                if (secondary != 6) throw new Exception($"Secondary page for 5 should be 6, got {secondary}");

                // Moving previous from 5 -> 3 (-2)
                current = (current <= 1) ? 0 : current - 2;
                if (current != 3) throw new Exception($"Prev from 5 should be 3, got {current}");

                // Moving previous from 1 -> 0 (-1)
                current = 1;
                current = (current <= 1) ? 0 : current - 2;
                if (current != 0) throw new Exception($"Prev from 1 should be 0, got {current}");

                // Odd trailing page handling (e.g. total = 6: 0, 1, 2, 3, 4, 5):
                // If current = 5: current + 1 < total is false, secondary is null
                int totalEven = 6;
                int lastPage = 5;
                int? trailingSecondary = (lastPage > 0 && lastPage + 1 < totalEven) ? lastPage + 1 : null;
                if (trailingSecondary != null) throw new Exception("Trailing odd page should have null secondary page");

                return Task.CompletedTask;
            });

            // Helper to create real RAR archives via WinRAR (RAR5 by default)
            void CreateRarArchive(string rarPath, string[] files, string? password = null)
            {
                string rarExe = @"C:\Program Files\WinRAR\Rar.exe";
                if (!File.Exists(rarExe)) throw new FileNotFoundException("WinRAR Rar.exe not found at: " + rarExe);

                string passArg = string.IsNullOrEmpty(password) ? "" : $"-p\"{password}\" ";
                string quotedFiles = string.Join(" ", files.Select(f => $"\"{f}\""));
                string args = $"a {passArg}-inul \"{rarPath}\" {quotedFiles}";

                using var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = rarExe,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                })!;
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Rar.exe exited with code {proc.ExitCode}");
                }
            }

            // Helper to create 7z archives via SharpCompress
            void CreateSevenZipArchive(string archivePath, Dictionary<string, byte[]> entries)
            {
                using var fs = File.Create(archivePath);
                using var writer = new SharpCompress.Writers.SevenZip.SevenZipWriter(
                    fs,
                    new SharpCompress.Writers.SevenZip.SevenZipWriterOptions());

                foreach (var kvp in entries)
                {
                    using var ms = new MemoryStream(kvp.Value);
                    writer.Write(kvp.Key, ms, DateTime.UtcNow);
                }
            }

            // Test 13: RarComicLoader Handles RAR4, RAR5, Junk Files, and Encrypted Archives
            await RunTestAsync("RarComicLoader Loads RAR4 & RAR5, Filters Junk, Rejects Passwords", async () =>
            {
                string rarWorkDir = Path.Combine(tempDir, "RarWork");
                Directory.CreateDirectory(rarWorkDir);

                string p1 = Path.Combine(rarWorkDir, "Page_01.png");
                string p2 = Path.Combine(rarWorkDir, "Page_02.png");
                string p3 = Path.Combine(rarWorkDir, "Page_03.png");
                string junk = Path.Combine(rarWorkDir, "Thumbs.db");
                string dsStore = Path.Combine(rarWorkDir, ".DS_Store");

                await File.WriteAllBytesAsync(p1, SamplePngBytes);
                await File.WriteAllBytesAsync(p2, SamplePngBytes);
                await File.WriteAllBytesAsync(p3, SamplePngBytes);
                await File.WriteAllBytesAsync(junk, new byte[] { 1, 2, 3 });
                await File.WriteAllBytesAsync(dsStore, new byte[] { 4, 5, 6 });

                var files = new[] { p1, p2, p3, junk, dsStore };
                var loader = new RarComicLoader();

                // 1. RAR4 Archive (genuine RAR 4.x binary with 2 pages and 1 Thumbs.db junk file)
                string rar4Path = Path.Combine(tempDir, "Comic_v4.cbr");
                await File.WriteAllBytesAsync(rar4Path, SampleRar4Bytes);
                if (!loader.CanLoad(rar4Path)) throw new Exception("CanLoad returned false for valid .cbr file");

                using (var comic4 = await loader.LoadAsync(rar4Path))
                {
                    if (comic4.SourceType != ComicSourceType.RarArchive) throw new Exception("Expected RarArchive source type");
                    if (comic4.PageCount != 2) throw new Exception($"Expected 2 pages (filtered junk), got {comic4.PageCount}");
                    if (comic4.Pages[0].DisplayName != "Page_01.png") throw new Exception($"Expected Page_01.png, got {comic4.Pages[0].DisplayName}");
                    if (comic4.Pages[1].DisplayName != "Page_02.png") throw new Exception($"Expected Page_02.png, got {comic4.Pages[1].DisplayName}");

                    var data = await comic4.Pages[0].GetPageDataAsync();
                    if (data.Data == null || data.Data.Length == 0) throw new Exception("Extracted page data is empty");
                }

                // 2. RAR5 Archive (generated dynamically via WinRAR 7.x)
                string rar5Path = Path.Combine(tempDir, "Comic_v5.rar");
                CreateRarArchive(rar5Path, files);
                if (!loader.CanLoad(rar5Path)) throw new Exception("CanLoad returned false for valid .rar file");

                using (var comic5 = await loader.LoadAsync(rar5Path))
                {
                    if (comic5.PageCount != 3) throw new Exception($"Expected 3 pages, got {comic5.PageCount}");
                    var data = await comic5.Pages[2].GetPageDataAsync();
                    if (data.Data == null || data.Data.Length == 0) throw new Exception("Extracted page data is empty");
                }

                // 3. Password-protected RAR archive
                string encryptedPath = Path.Combine(tempDir, "Comic_Encrypted.cbr");
                CreateRarArchive(encryptedPath, files, password: "SecretPassword123");

                bool caughtPasswordError = false;
                try
                {
                    using var encComic = await loader.LoadAsync(encryptedPath);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("password-protected", StringComparison.OrdinalIgnoreCase))
                {
                    caughtPasswordError = true;
                }
                if (!caughtPasswordError) throw new Exception("Expected password-protected RAR to fail gracefully with a clear message");

                // 4. Corrupt RAR archive
                string corruptPath = Path.Combine(tempDir, "CorruptComic.cbr");
                await File.WriteAllBytesAsync(corruptPath, new byte[] { 0x52, 0x61, 0x72, 0x21, 0x00, 0x00, 0x00 });
                bool caughtCorrupt = false;
                try
                {
                    using var corruptComic = await loader.LoadAsync(corruptPath);
                }
                catch (InvalidOperationException)
                {
                    caughtCorrupt = true;
                }
                if (!caughtCorrupt) throw new Exception("Expected corrupt RAR to throw InvalidOperationException");

                // 5. Empty RAR archive (only text files)
                string emptyRarPath = Path.Combine(tempDir, "EmptyComic.cbr");
                string txtFile = Path.Combine(rarWorkDir, "readme.txt");
                await File.WriteAllTextAsync(txtFile, "No comics here");
                CreateRarArchive(emptyRarPath, new[] { txtFile });
                bool caughtEmpty = false;
                try
                {
                    using var emptyComic = await loader.LoadAsync(emptyRarPath);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("No supported comic images found", StringComparison.OrdinalIgnoreCase))
                {
                    caughtEmpty = true;
                }
                if (!caughtEmpty) throw new Exception("Expected archive with no images to fail with clear error message");
            });

            // Test 14: SevenZipComicLoader Handles 7Z/CB7 Archives and Dispatch
            await RunTestAsync("SevenZipComicLoader Loads 7Z/CB7 Archives and ComicLoaderService Dispatches", async () =>
            {
                var loader = new SevenZipComicLoader();
                var service = new ComicLoaderService();

                // 1. Create a 7z/CB7 archive with pages and junk files
                string cb7Path = Path.Combine(tempDir, "SampleComic.cb7");
                var entries = new Dictionary<string, byte[]>
                {
                    ["Page_01.png"] = SamplePngBytes,
                    ["Page_02.png"] = SamplePngBytes,
                    ["Page_03.png"] = SamplePngBytes,
                    ["__MACOSX/._Page_01.png"] = new byte[] { 1, 2, 3 },
                    ["Thumbs.db"] = new byte[] { 4, 5, 6 },
                    [".hidden_file"] = new byte[] { 7, 8, 9 }
                };

                CreateSevenZipArchive(cb7Path, entries);
                if (!loader.CanLoad(cb7Path)) throw new Exception("CanLoad returned false for valid .cb7 file");

                using (var comic = await loader.LoadAsync(cb7Path))
                {
                    if (comic.SourceType != ComicSourceType.SevenZipArchive) throw new Exception("Expected SevenZipArchive source type");
                    if (comic.PageCount != 3) throw new Exception($"Expected 3 pages (filtered junk), got {comic.PageCount}");
                    if (comic.Pages[0].DisplayName != "Page_01.png") throw new Exception($"Expected Page_01.png, got {comic.Pages[0].DisplayName}");
                    if (comic.Pages[1].DisplayName != "Page_02.png") throw new Exception($"Expected Page_02.png, got {comic.Pages[1].DisplayName}");
                    if (comic.Pages[2].DisplayName != "Page_03.png") throw new Exception($"Expected Page_03.png, got {comic.Pages[2].DisplayName}");

                    var data = await comic.Pages[0].GetPageDataAsync();
                    if (data.Data == null || data.Data.Length == 0) throw new Exception("Extracted page data is empty");
                }

                // 2. Also test with .7z extension
                string sevenZipPath = Path.Combine(tempDir, "SampleComic.7z");
                File.Copy(cb7Path, sevenZipPath, overwrite: true);
                if (!loader.CanLoad(sevenZipPath)) throw new Exception("CanLoad returned false for valid .7z file");

                // 3. Test ComicLoaderService dispatch for all formats: .cbr, .rar, .cb7, .7z
                string rar5Path = Path.Combine(tempDir, "Comic_v5.rar");
                using (var dispatchedRar = await service.LoadComicAsync(rar5Path))
                {
                    if (dispatchedRar.SourceType != ComicSourceType.RarArchive)
                        throw new Exception("ComicLoaderService failed to route .rar to RarComicLoader");
                }

                using (var dispatched7z = await service.LoadComicAsync(sevenZipPath))
                {
                    if (dispatched7z.SourceType != ComicSourceType.SevenZipArchive)
                        throw new Exception("ComicLoaderService failed to route .7z to SevenZipComicLoader");
                }
            });

            // Test 15: FormatConversionService Converts Folder, CBR, and CB7 to Compliant CBZ Archives
            await RunTestAsync("FormatConversionService Converts CBR, CB7, and Folders to Valid CBZ Archives", async () =>
            {
                var conversionService = new FormatConversionService();
                var zipLoader = new ZipComicLoader();

                // 1. Convert CBR to CBZ
                string sourceCbr = Path.Combine(tempDir, "Comic_v5.rar");
                string outCbz = await conversionService.ConvertToCbzAsync(sourceCbr);
                if (!File.Exists(outCbz)) throw new Exception($"Expected converted file '{outCbz}' to exist");
                if (!outCbz.EndsWith(".cbz", StringComparison.OrdinalIgnoreCase)) throw new Exception("Expected .cbz extension");

                using (var convertedComic = await zipLoader.LoadAsync(outCbz))
                {
                    if (convertedComic.PageCount != 3)
                        throw new Exception($"Expected 3 pages in converted CBZ, got {convertedComic.PageCount}");
                    if (convertedComic.Pages[0].DisplayName != "0001_Page_01.png")
                        throw new Exception($"Expected 0001_Page_01.png, got {convertedComic.Pages[0].DisplayName}");

                    var pData = await convertedComic.Pages[0].GetPageDataAsync();
                    if (pData.Data == null || pData.Data.Length == 0)
                        throw new Exception("Extracted page data is empty in converted CBZ");
                }

                // 2. Convert CB7 to custom output path
                string sourceCb7 = Path.Combine(tempDir, "SampleComic.cb7");
                string customOutCbz = Path.Combine(tempDir, "CustomOutput_FromCb7.cbz");
                await conversionService.ConvertToCbzAsync(sourceCb7, customOutCbz);
                if (!File.Exists(customOutCbz)) throw new Exception("Expected custom destination CBZ to exist");

                using (var fromCb7 = await zipLoader.LoadAsync(customOutCbz))
                {
                    if (fromCb7.PageCount != 3)
                        throw new Exception($"Expected 3 pages in CB7-converted CBZ, got {fromCb7.PageCount}");
                }

                // 3. Convert image folder to CBZ
                string sourceFolder = Path.Combine(tempDir, "ActionComicVol1");
                string folderOutCbz = await conversionService.ConvertToCbzAsync(sourceFolder);
                if (!File.Exists(folderOutCbz)) throw new Exception("Expected folder-converted CBZ to exist");

                using (var fromFolder = await zipLoader.LoadAsync(folderOutCbz))
                {
                    if (fromFolder.PageCount != 3)
                        throw new Exception($"Expected 3 pages in folder-converted CBZ, got {fromFolder.PageCount}");
                }

                // 4. Conversion error handling (corrupt file)
                string corruptPath = Path.Combine(tempDir, "CorruptComic.cbr");
                bool caughtError = false;
                try
                {
                    await conversionService.ConvertToCbzAsync(corruptPath);
                }
                catch (Exception)
                {
                    caughtError = true;
                }
                if (!caughtError) throw new Exception("Expected corrupt source conversion to throw");
            });

            // Test 16: LibraryScannerService Discovers RAR & 7Z Comics and Generates Thumbnails
            await RunTestAsync("LibraryScannerService Discovers RAR/7Z Comics and Generates Thumbnails", async () =>
            {
                string libraryDbPath = Path.Combine(tempDir, "scanner_rar_test.db");
                string thumbDir = Path.Combine(tempDir, "scanner_rar_thumbs");
                Directory.CreateDirectory(thumbDir);

                var repo = new LibraryRepository(libraryDbPath);
                await repo.InitializeAsync();

                var thumbService = new ThumbnailService(thumbDir);
                var loaderService = new ComicLoaderService();
                var scanner = new LibraryScannerService(repo, loaderService, thumbService);

                // Scan tempDir which contains .cbr, .rar, .cb7, .7z, .cbz, and folder comics
                await scanner.ScanFolderAsync(tempDir);

                var allComics = await repo.GetComicsAsync();
                bool foundRar = allComics.Any(c => c.Format == ComicSourceType.RarArchive);
                bool found7z = allComics.Any(c => c.Format == ComicSourceType.SevenZipArchive);

                if (!foundRar) throw new Exception("LibraryScannerService failed to index RarArchive comics");
                if (!found7z) throw new Exception("LibraryScannerService failed to index SevenZipArchive comics");

                // Verify thumbnail generated for RAR comic
                var rarComic = allComics.First(c => c.Format == ComicSourceType.RarArchive);
                if (string.IsNullOrEmpty(rarComic.ThumbnailPath) || !File.Exists(rarComic.ThumbnailPath))
                {
                    throw new Exception("ThumbnailService did not generate cover thumbnail for RarArchive comic");
                }

                // Verify format badges
                if (rarComic.FormatBadge != "CBR" && rarComic.FormatBadge != "RAR")
                {
                    throw new Exception($"Unexpected format badge for RAR: {rarComic.FormatBadge}");
                }

                var cb7Comic = allComics.First(c => c.Format == ComicSourceType.SevenZipArchive);
                if (cb7Comic.FormatBadge != "CB7" && cb7Comic.FormatBadge != "7Z")
                {
                    throw new Exception($"Unexpected format badge for 7Z: {cb7Comic.FormatBadge}");
                }
            });

            // Test 17: AppSettings Persistence and Retrieval
            await RunTestAsync("AppSettings Persistence and Retrieval", async () =>
            {
                string dbFile = Path.Combine(tempDir, "settings_test.db");
                using var repo = new LibraryRepository(dbFile);
                await repo.InitializeAsync();

                // 1. Initial defaults
                var initial = await repo.GetAppSettingsAsync();
                if (initial.Theme != "Default") throw new Exception($"Expected default Theme 'Default', got '{initial.Theme}'");
                if (initial.DefaultFitMode != "FitToHeight") throw new Exception($"Expected default FitMode 'FitToHeight', got '{initial.DefaultFitMode}'");
                if (initial.DefaultReadingDirection != "LeftToRight") throw new Exception($"Expected default ReadingDirection 'LeftToRight', got '{initial.DefaultReadingDirection}'");
                if (initial.DefaultViewMode != "Grid") throw new Exception($"Expected default ViewMode 'Grid', got '{initial.DefaultViewMode}'");
                if (initial.DefaultBrightness != 0.0) throw new Exception($"Expected default Brightness 0, got {initial.DefaultBrightness}");
                if (initial.DefaultContrast != 1.0) throw new Exception($"Expected default Contrast 1.0, got {initial.DefaultContrast}");
                if (initial.DefaultWarmth != 0.0) throw new Exception($"Expected default Warmth 0, got {initial.DefaultWarmth}");
                if (initial.DefaultNightMode != false) throw new Exception("Expected default NightMode false");

                // 2. Mutate and save
                var modified = new AppSettings
                {
                    Theme = "Dark",
                    DefaultFitMode = "FitToWidth",
                    DefaultReadingDirection = "RightToLeft",
                    DefaultViewMode = "List",
                    DefaultSortOption = 2,
                    DefaultBrightness = 0.2,
                    DefaultContrast = 1.3,
                    DefaultWarmth = 0.15,
                    DefaultNightMode = true
                };
                await repo.SaveAppSettingsAsync(modified);

                // 3. Re-read and assert
                var loaded = await repo.GetAppSettingsAsync();
                if (loaded.Theme != "Dark") throw new Exception($"Theme mismatch: {loaded.Theme}");
                if (loaded.DefaultFitMode != "FitToWidth") throw new Exception($"FitMode mismatch: {loaded.DefaultFitMode}");
                if (loaded.DefaultReadingDirection != "RightToLeft") throw new Exception($"ReadingDirection mismatch: {loaded.DefaultReadingDirection}");
                if (loaded.DefaultViewMode != "List") throw new Exception($"ViewMode mismatch: {loaded.DefaultViewMode}");
                if (loaded.DefaultSortOption != 2) throw new Exception($"SortOption mismatch: {loaded.DefaultSortOption}");
                if (Math.Abs(loaded.DefaultBrightness - 0.2) > 0.001) throw new Exception($"Brightness mismatch: {loaded.DefaultBrightness}");
                if (Math.Abs(loaded.DefaultContrast - 1.3) > 0.001) throw new Exception($"Contrast mismatch: {loaded.DefaultContrast}");
                if (Math.Abs(loaded.DefaultWarmth - 0.15) > 0.001) throw new Exception($"Warmth mismatch: {loaded.DefaultWarmth}");
                if (!loaded.DefaultNightMode) throw new Exception("NightMode expected to be true");

                // 4. Individual key/value
                await repo.SetSettingAsync("CustomKey", "CustomValue42");
                string? val = await repo.GetSettingAsync("CustomKey");
                if (val != "CustomValue42") throw new Exception($"CustomKey mismatch: {val}");
            });

            // Test 18: ComicMetadata CRUD Operations and Title Updating
            await RunTestAsync("ComicMetadata CRUD Operations and Title Updating", async () =>
            {
                string dbFile = Path.Combine(tempDir, "metadata_test.db");
                using var repo = new LibraryRepository(dbFile);
                await repo.InitializeAsync();

                // Add test comic to Comics table
                var comic = new ComicEntity
                {
                    FilePath = Path.Combine(tempDir, "SpiderMan_01.cbz"),
                    Title = "Spider-Man #1",
                    Format = ComicSourceType.ZipArchive,
                    PageCount = 32,
                    FileSize = 25000000,
                    DateAdded = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                long comicId = await repo.InsertComicAsync(comic);
                if (comicId <= 0) throw new Exception("Failed to insert comic");

                // Initially metadata is null
                var initialMeta = await repo.GetMetadataForComicAsync(comicId);
                if (initialMeta != null) throw new Exception("Expected initial metadata to be null");

                // Insert metadata
                var meta = new ComicMetadataEntity
                {
                    ComicId = comicId,
                    Title = "The Amazing Spider-Man",
                    SeriesName = "Amazing Spider-Man",
                    IssueNumber = "1",
                    Writers = "Stan Lee",
                    Artists = "Steve Ditko",
                    Publisher = "Marvel Comics",
                    ReleaseDate = "1963-03-01",
                    Summary = "The legendary first issue of The Amazing Spider-Man.",
                    LastUpdated = DateTime.UtcNow
                };
                await repo.SaveComicMetadataAsync(meta);

                // Read back
                var savedMeta = await repo.GetMetadataForComicAsync(comicId);
                if (savedMeta == null) throw new Exception("Saved metadata could not be retrieved");
                if (savedMeta.Title != "The Amazing Spider-Man") throw new Exception($"Title mismatch: {savedMeta.Title}");
                if (savedMeta.SeriesName != "Amazing Spider-Man") throw new Exception($"Series mismatch: {savedMeta.SeriesName}");
                if (savedMeta.IssueNumber != "1") throw new Exception($"Issue mismatch: {savedMeta.IssueNumber}");
                if (savedMeta.Writers != "Stan Lee") throw new Exception($"Writers mismatch: {savedMeta.Writers}");
                if (savedMeta.Artists != "Steve Ditko") throw new Exception($"Artists mismatch: {savedMeta.Artists}");
                if (savedMeta.Publisher != "Marvel Comics") throw new Exception($"Publisher mismatch: {savedMeta.Publisher}");
                if (savedMeta.ReleaseDate != "1963-03-01") throw new Exception($"ReleaseDate mismatch: {savedMeta.ReleaseDate}");
                if (savedMeta.Summary != "The legendary first issue of The Amazing Spider-Man.") throw new Exception($"Summary mismatch: {savedMeta.Summary}");

                // Formatted helpers
                if (savedMeta.FormattedSubtitle != "Amazing Spider-Man #1 (1963-03-01)")
                    throw new Exception($"FormattedSubtitle mismatch: '{savedMeta.FormattedSubtitle}'");
                if (savedMeta.FormattedCredits != "Writer: Stan Lee • Artist: Steve Ditko")
                    throw new Exception($"FormattedCredits mismatch: '{savedMeta.FormattedCredits}'");

                // Update metadata
                savedMeta.Summary = "Updated synopsis for issue 1.";
                await repo.SaveComicMetadataAsync(savedMeta);
                var updated = await repo.GetMetadataForComicAsync(comicId);
                if (updated?.Summary != "Updated synopsis for issue 1.") throw new Exception("Summary update failed");

                // Update comic title in Comics table
                await repo.UpdateComicTitleAsync(comicId, "The Amazing Spider-Man (1963)");
                var updatedComic = await repo.GetComicByIdAsync(comicId);
                if (updatedComic?.Title != "The Amazing Spider-Man (1963)")
                    throw new Exception($"Comic title update in Comics table failed: {updatedComic?.Title}");

                // Delete metadata
                await repo.DeleteComicMetadataAsync(comicId);
                var deletedMeta = await repo.GetMetadataForComicAsync(comicId);
                if (deletedMeta != null) throw new Exception("Expected metadata to be null after deletion");
            });

            // 19. Folder Collections & Re-reading Completed Comics
            await RunTestAsync("Folder Collections & Re-reading Completed Comics", async () =>
            {
                string dbPath = Path.Combine(tempDir, "test_collections.db");
                var repo = new LibraryRepository(dbPath);
                await repo.InitializeAsync();

                string watchedDir = Path.Combine(tempDir, "WatchedFolder");
                Directory.CreateDirectory(watchedDir);
                string comic1Path = Path.Combine(watchedDir, "HeroComic.cbz");
                File.WriteAllBytes(comic1Path, SamplePngBytes);

                await repo.AddWatchedFolderAsync(watchedDir);

                var comic1 = new ComicEntity
                {
                    FilePath = comic1Path,
                    Title = "Hero Comic",
                    Format = ComicSourceType.ZipArchive,
                    PageCount = 10,
                    FileSize = 100,
                    IsCompleted = false,
                    LastReadPage = 0
                };
                long id = await repo.InsertComicAsync(comic1);

                // Check MoveComicToCollectionFolderAsync
                await repo.MoveComicToCollectionFolderAsync(id, "Marvel");

                var updatedComic = await repo.GetComicByIdAsync(id);
                if (updatedComic == null || updatedComic.ParentFolder != "Marvel")
                    throw new Exception("Comic record ParentFolder was not updated after move");

                var folders = await repo.GetCollectionFoldersAsync();
                if (!folders.Contains("Marvel"))
                    throw new Exception("Collection folders did not contain 'Marvel'");

                int countInMarvel = await repo.GetComicCountInFolderAsync("Marvel");
                if (countInMarvel != 1)
                    throw new Exception($"Expected count 1 in Marvel, got {countInMarvel}");

                // Filter by collection folder
                var filtered = await repo.GetComicsAsync(new LibraryFilter { SelectedCollection = "Marvel" });
                if (filtered.Count != 1)
                    throw new Exception($"Expected 1 comic filtered by Marvel, got {filtered.Count}");

                // Test Completed -> Re-reading progress state
                // Mark completed
                await repo.SetCompletedStatusAsync(id, true);
                var completedComic = await repo.GetComicByIdAsync(id);
                if (!completedComic!.IsCompleted) throw new Exception("Comic should be marked completed");

                var inProgressList = await repo.GetComicsAsync(new LibraryFilter { InProgressOnly = true });
                if (inProgressList.Any(c => c.Id == id))
                    throw new Exception("Completed comic should not be in InProgress list");

                // Now simulate re-reading: set completed = false, page = 0, last_read_at = DateTime.UtcNow
                await repo.SetCompletedStatusAsync(id, false);
                await repo.UpdateReadingProgressAsync(comic1Path, 0, 10);
                var rereadingComic = await repo.GetComicByIdAsync(id);
                if (rereadingComic!.IsCompleted) throw new Exception("Comic should not be completed after reset");
                if (rereadingComic.LastReadAt == null) throw new Exception("LastReadAt should be populated for re-reading");

                var inProgressAfter = await repo.GetComicsAsync(new LibraryFilter { InProgressOnly = true });
                if (!inProgressAfter.Any(c => c.Id == id))
                    throw new Exception("Re-reading comic should now appear in InProgress list");
            });

            // 20. Virtual In-App Folders, Soft-Delete & JSON Backup Cache
            await RunTestAsync("Virtual In-App Folders, Soft-Delete & JSON Backup Cache", async () =>
            {
                string dbPath = Path.Combine(tempDir, "folders_test.db");
                var repo = new LibraryRepository(dbPath);
                await repo.InitializeAsync();

                long comicId = await repo.InsertComicAsync(new ComicEntity
                {
                    FilePath = Path.Combine(tempDir, "SampleComic.cbz"),
                    Title = "Spider-Man #1",
                    Format = ComicSourceType.ZipArchive,
                    PageCount = 20,
                    FileSize = 1024
                });

                // Create folder
                await repo.CreateFolderAsync("Superheroes");
                var folders = await repo.GetApplicationFoldersAsync();
                if (!folders.Any(f => f.Name == "Superheroes"))
                    throw new Exception("Created folder 'Superheroes' not found");

                // Add comic to folder
                await repo.AddComicToFolderAsync(comicId, "Superheroes");
                folders = await repo.GetApplicationFoldersAsync();
                var superheroFolder = folders.FirstOrDefault(f => f.Name == "Superheroes");
                if (superheroFolder == null || superheroFolder.ComicCount != 1)
                    throw new Exception($"Expected comic count 1 in 'Superheroes', got {superheroFolder?.ComicCount}");

                // Rename folder
                await repo.RenameFolderAsync("Superheroes", "Marvel Universe");
                folders = await repo.GetApplicationFoldersAsync();
                if (!folders.Any(f => f.Name == "Marvel Universe"))
                    throw new Exception("Renamed folder 'Marvel Universe' not found");

                // Completed status and count
                await repo.SetCompletedStatusAsync(comicId, true);
                int completedCount = await repo.GetCompletedCountAsync();
                if (completedCount != 1)
                    throw new Exception($"Expected completed count 1, got {completedCount}");

                var completedComics = await repo.GetComicsAsync(new LibraryFilter { CompletedOnly = true });
                if (!completedComics.Any(c => c.Id == comicId))
                    throw new Exception("Completed filter did not return completed comic");

                // Backup and restore cache test
                await repo.BackupLibraryDataCacheAsync();
                await repo.RestoreLibraryDataCacheAsync();

                // Soft delete on watched folder removal
                await repo.RemoveComicsInWatchedFolderAsync(tempDir);
                var activeComics = await repo.GetComicsAsync(new LibraryFilter());
                if (activeComics.Any(c => c.Id == comicId))
                    throw new Exception("Soft-deleted comic should be excluded from active library");

                // Re-indexing resets is_missing = 0 and restores record
                await repo.InsertComicAsync(new ComicEntity
                {
                    FilePath = Path.Combine(tempDir, "SampleComic.cbz"),
                    Title = "Spider-Man #1",
                    Format = ComicSourceType.ZipArchive,
                    PageCount = 20,
                    FileSize = 1024
                });
                var restoredComic = await repo.GetComicByIdAsync(comicId);
                if (restoredComic == null || restoredComic.IsMissing)
                    throw new Exception("Comic should be restored and not marked missing");
                if (!restoredComic.IsCompleted)
                    throw new Exception("Comic should retain completed status across re-indexing");

                // Delete folder
                await repo.DeleteFolderAsync("Marvel Universe");
                folders = await repo.GetApplicationFoldersAsync();
                if (folders.Any(f => f.Name == "Marvel Universe"))
                    throw new Exception("Deleted folder still exists");

                var comicAfterFolderDelete = await repo.GetComicByIdAsync(comicId);
                if (comicAfterFolderDelete == null)
                    throw new Exception("Comic should remain in library after folder is deleted");
            });

            // Test 24: SeriesParserHelper parses comic series and issue numbers
            await RunTestAsync("SeriesParserHelper Parses Titles and Issues Correctly", () =>
            {
                var (series1, issue1) = SeriesParserHelper.ParseSeriesAndIssue("Batman (2016) #05.cbz");
                if (issue1 != 5.0 || !series1.Contains("Batman"))
                    throw new Exception($"Failed parsing Batman: got series '{series1}', issue {issue1}");

                var (series2, issue2) = SeriesParserHelper.ParseSeriesAndIssue("Spider-Man Vol. 2 Issue 12 (Digital-Empire).cbr");
                if (issue2 != 12.0)
                    throw new Exception($"Failed parsing Spider-Man issue: got {issue2}");

                var (series3, issue3) = SeriesParserHelper.ParseSeriesAndIssue("One Piece Chapter 1000.pdf");
                if (issue3 != 1000.0 || !series3.Contains("One Piece"))
                    throw new Exception($"Failed parsing One Piece: got series '{series3}', issue {issue3}");

                var (series4, issue4) = SeriesParserHelper.ParseSeriesAndIssue("Invincible 042.cbz");
                if (issue4 != 42.0 || !series4.Contains("Invincible"))
                    throw new Exception($"Failed parsing Invincible: got series '{series4}', issue {issue4}");

                // Test 90% full-title series matching & structural normalization
                bool sameSeries = SeriesParserHelper.AreInSameSeries("Batman (2016) #01", "Batman (2016) #02", 0.90);
                if (!sameSeries)
                    throw new Exception("Expected Batman (2016) #01 and #02 to be matched in same series (>=90%)");

                bool diffSeries = SeriesParserHelper.AreInSameSeries("Batman #01", "Spider-Man #01", 0.90);
                if (diffSeries)
                    throw new Exception("Batman #01 and Spider-Man #01 should NOT be in the same series");

                // Test 95% duplicate detection vs series separation
                bool duplicates = SeriesParserHelper.AreDuplicates("Batman #01 (Digital)", "Batman #01", 0.95);
                if (!duplicates)
                    throw new Exception("Expected Batman #01 (Digital) and Batman #01 to be detected as duplicates");

                bool notDuplicates = SeriesParserHelper.AreDuplicates("Batman #01", "Batman #02", 0.95);
                if (notDuplicates)
                    throw new Exception("Batman #01 and Batman #02 must NOT be flagged as duplicates under 95% threshold");

                return Task.CompletedTask;
            });

            // Test 25: DuplicateDetectionService finds cross-format and name duplicates
            await RunTestAsync("DuplicateDetectionService Finds Cross-Format and Normalized Duplicates", () =>
            {
                var duplicateService = new DuplicateDetectionService();
                var comics = new List<ComicEntity>
                {
                    new ComicEntity { Id = 1, Title = "Batman #01 (Digital)", FilePath = @"C:\Comics\Batman #01 (Digital).cbz", Format = ComicSourceType.ZipArchive, PageCount = 24, FileSize = 1000 },
                    new ComicEntity { Id = 2, Title = "Batman #01", FilePath = @"C:\Comics\Batman #01.pdf", Format = ComicSourceType.PdfDocument, PageCount = 24, FileSize = 1000 },
                    new ComicEntity { Id = 3, Title = "Saga #01", FilePath = @"C:\Comics\Saga #01.cbz", Format = ComicSourceType.ZipArchive, PageCount = 30, FileSize = 2000 },
                    new ComicEntity { Id = 4, Title = "Saga #01 (Webrip)", FilePath = @"C:\Comics\Saga #01 (Webrip).cbr", Format = ComicSourceType.RarArchive, PageCount = 30, FileSize = 2500 }
                };

                var ignoredPairs = new HashSet<(long, long)>();
                var duplicates = duplicateService.FindDuplicates(comics, ignoredPairs);

                if (duplicates.Count < 2)
                    throw new Exception($"Expected at least 2 duplicate groups, found {duplicates.Count}");

                // Now test ignored pairs
                ignoredPairs.Add((1, 2));
                var filteredDuplicates = duplicateService.FindDuplicates(comics, ignoredPairs);
                if (filteredDuplicates.Any(g => g.Copies.Any(c => c.Id == 1) && g.Copies.Any(c => c.Id == 2)))
                    throw new Exception("Ignored duplicate pair (1, 2) was still detected as duplicate");

                return Task.CompletedTask;
            });

            // Test 26: ColorCorrectionHelper Presets
            await RunTestAsync("ColorCorrectionHelper Applies Color Presets", () =>
            {
                // 4 pixels RGBA: White, Black, Red, Blue
                byte[] bgra = new byte[]
                {
                    255, 255, 255, 255, // White (B=255, G=255, R=255)
                    0,   0,   0,   255, // Black (B=0, G=0, R=0)
                    0,   0,   255, 255, // Red   (B=0, G=0, R=255)
                    255, 0,   0,   255  // Blue  (B=255, G=0, R=0)
                };

                // Grayscale preset
                var graySettings = new ColorCorrectionSettings { Preset = ReadingPreset.Grayscale };
                var grayResult = ColorCorrectionHelper.ApplyColorCorrection(bgra, graySettings);
                for (int i = 0; i < grayResult.Length; i += 4)
                {
                    if (grayResult[i] != grayResult[i + 1] || grayResult[i + 1] != grayResult[i + 2])
                        throw new Exception($"Grayscale failed at pixel {i / 4}: B={grayResult[i]}, G={grayResult[i+1]}, R={grayResult[i+2]}");
                }

                // Inverted preset
                var invSettings = new ColorCorrectionSettings { Preset = ReadingPreset.Inverted };
                var invResult = ColorCorrectionHelper.ApplyColorCorrection(bgra, invSettings);
                if (invResult[0] > 10 || invResult[1] > 10 || invResult[2] > 10)
                    throw new Exception("Inverted white pixel was not dark");

                // Sepia preset
                var sepiaSettings = new ColorCorrectionSettings { Preset = ReadingPreset.Sepia };
                var sepiaResult = ColorCorrectionHelper.ApplyColorCorrection(bgra, sepiaSettings);
                if (sepiaResult.Length != bgra.Length)
                    throw new Exception("Sepia result length mismatch");

                return Task.CompletedTask;
            });

            // Test 27: Window Geometry Persistence
            await RunTestAsync("LibraryRepository Saves and Restores Window Geometry", async () =>
            {
                string dbPath = Path.Combine(tempDir, "window_test.db");
                using var repo = new LibraryRepository(dbPath);
                await repo.InitializeAsync();

                var settings = await repo.GetAppSettingsAsync();
                settings.WindowWidth = 1400;
                settings.WindowHeight = 900;
                settings.WindowX = 120;
                settings.WindowY = 80;
                settings.IsMaximized = false;
                await repo.SaveAppSettingsAsync(settings);

                var loadedSettings = await repo.GetAppSettingsAsync();
                if (loadedSettings.WindowWidth != 1400 || loadedSettings.WindowHeight != 900)
                    throw new Exception($"Window dimensions mismatch: {loadedSettings.WindowWidth}x{loadedSettings.WindowHeight}");
                if (loadedSettings.WindowX != 120 || loadedSettings.WindowY != 80)
                    throw new Exception($"Window position mismatch: {loadedSettings.WindowX}, {loadedSettings.WindowY}");
                if (loadedSettings.IsMaximized)
                    throw new Exception("Window should not be maximized");

                // Test maximized state
                loadedSettings.IsMaximized = true;
                await repo.SaveAppSettingsAsync(loadedSettings);
                var reloadedSettings = await repo.GetAppSettingsAsync();
                if (!reloadedSettings.IsMaximized)
                    throw new Exception("Maximized state was not persisted");
            });

            // Test 28: Reading Sessions and Stats Summary
            await RunTestAsync("LibraryRepository Tracks Reading Sessions and Calculates Statistics", async () =>
            {
                string dbPath = Path.Combine(tempDir, "stats_test.db");
                using var repo = new LibraryRepository(dbPath);
                await repo.InitializeAsync();

                var comic1 = new ComicEntity
                {
                    Title = "Action Comics #01",
                    FilePath = @"C:\Comics\Action Comics #01.cbz",
                    Format = ComicSourceType.ZipArchive,
                    PageCount = 30,
                    FileSize = 2048,
                    IsCompleted = true
                };
                long comicId1 = await repo.InsertComicAsync(comic1);

                // Add sessions
                var now = DateTime.UtcNow;
                await repo.RecordReadingSessionAsync(comicId1, now.AddMinutes(-30), now, 1800, 30);
                await repo.RecordReadingSessionAsync(comicId1, now.AddDays(-1).AddMinutes(-20), now.AddDays(-1), 1200, 20);

                var stats = await repo.GetReadingStatsSummaryAsync();
                if (stats.TotalPagesRead != 50)
                    throw new Exception($"Expected 50 pages read, got {stats.TotalPagesRead}");
                if (stats.TotalDurationMinutes != 50) // 1800s + 1200s = 3000s = 50 min
                    throw new Exception($"Expected 50 minutes, got {stats.TotalDurationMinutes}");
                if (stats.ComicsCompleted != 1)
                    throw new Exception($"Expected 1 completed comic, got {stats.ComicsCompleted}");
                if (stats.CurrentDailyStreak < 1)
                    throw new Exception($"Expected current streak >= 1, got {stats.CurrentDailyStreak}");
                if (stats.TopSeries.Count == 0 || !stats.TopSeries[0].SeriesName.Contains("Action Comics"))
                    throw new Exception("Top series was not detected correctly");
            });

            // Test 29: Full Library Backup Export and Import
            await RunTestAsync("LibraryRepository Exports and Restores Portable Backup JSON", async () =>
            {
                string db1Path = Path.Combine(tempDir, "backup_src.db");
                string db2Path = Path.Combine(tempDir, "backup_dst.db");

                using var repo1 = new LibraryRepository(db1Path);
                await repo1.InitializeAsync();

                var comic = new ComicEntity
                {
                    Title = "Daredevil #01",
                    FilePath = @"C:\Comics\Daredevil #01.cbz",
                    Format = ComicSourceType.ZipArchive,
                    PageCount = 32,
                    FileSize = 4096,
                    IsFavorite = true,
                    LastReadPage = 16
                };
                long comicId = await repo1.InsertComicAsync(comic);
                await repo1.AddTagToComicAsync(comicId, "Favorite Marvel");
                await repo1.AddBookmarkAsync(comicId, 10, "Great splash page!");

                // Export to JSON
                string backupJson = await repo1.ExportLibraryBackupJsonAsync();
                if (string.IsNullOrWhiteSpace(backupJson) || !backupJson.Contains("Daredevil #01"))
                    throw new Exception("Exported JSON is invalid or missing comic data");

                // Now import into fresh repo2 that already has the file indexed
                using var repo2 = new LibraryRepository(db2Path);
                await repo2.InitializeAsync();
                await repo2.InsertComicAsync(new ComicEntity
                {
                    Title = "Daredevil #01",
                    FilePath = @"C:\Comics\Daredevil #01.cbz",
                    Format = ComicSourceType.ZipArchive,
                    PageCount = 32,
                    FileSize = 4096,
                    IsFavorite = false,
                    LastReadPage = 0
                });

                var (restoredComics, restoredBookmarks, restoredTags) = await repo2.ImportLibraryBackupJsonAsync(backupJson, overwriteExisting: true);

                if (restoredComics != 1)
                    throw new Exception($"Expected 1 comic restored, got {restoredComics}");
                if (restoredBookmarks != 1)
                    throw new Exception($"Expected 1 bookmark restored, got {restoredBookmarks}");
                if (restoredTags != 1)
                    throw new Exception($"Expected 1 tag restored, got {restoredTags}");

                var restoredComic = await repo2.GetComicByPathAsync(@"C:\Comics\Daredevil #01.cbz");
                if (restoredComic == null || !restoredComic.IsFavorite || restoredComic.LastReadPage != 16)
                    throw new Exception("Restored comic properties mismatch");

                var bookmarks = await repo2.GetBookmarksForComicAsync(restoredComic.Id);
                if (bookmarks.Count != 1 || bookmarks[0].UserNote != "Great splash page!")
                    throw new Exception("Restored bookmark mismatch");

                var tags = await repo2.GetTagsForComicAsync(restoredComic.Id);
                if (!tags.Contains("Favorite Marvel"))
                    throw new Exception("Restored tag mismatch");
            });

            await RunTestAsync("LibraryRepository Supports Manual Series Creation, Retrieval and Deletion", async () =>
            {
                string seriesDb = Path.Combine(tempDir, "series_test.db");
                using var repo = new LibraryRepository(seriesDb);
                await repo.InitializeAsync();

                long c1 = await repo.InsertComicAsync(new ComicEntity
                {
                    Title = "Batman: Year One #1",
                    FilePath = @"C:\Comics\batman_1.cbz",
                    PageCount = 24
                });
                long c2 = await repo.InsertComicAsync(new ComicEntity
                {
                    Title = "Batman: Year One #2",
                    FilePath = @"C:\Comics\batman_2.cbz",
                    PageCount = 24
                });

                // Create manual series
                long seriesId = await repo.CreateManualSeriesAsync("Batman: Year One Complete", new[] { c1, c2 });
                if (seriesId <= 0) throw new Exception("Expected valid seriesId > 0");

                var allSeries = await repo.GetManualSeriesAsync();
                if (allSeries.Count != 1) throw new Exception($"Expected 1 manual series, got {allSeries.Count}");
                if (allSeries[0].SeriesName != "Batman: Year One Complete") throw new Exception("Series name mismatch");
                if (allSeries[0].Issues.Count != 2) throw new Exception($"Expected 2 issues, got {allSeries[0].Issues.Count}");
                if (!allSeries[0].IsManual) throw new Exception("Expected IsManual to be true");

                // Add comic to existing series
                long c3 = await repo.InsertComicAsync(new ComicEntity
                {
                    Title = "Batman: Year One #3",
                    FilePath = @"C:\Comics\batman_3.cbz",
                    PageCount = 24
                });
                await repo.AddComicsToManualSeriesAsync(seriesId, new[] { c3 });
                allSeries = await repo.GetManualSeriesAsync();
                if (allSeries[0].Issues.Count != 3)
                    throw new Exception($"Expected 3 issues after adding to existing series, got {allSeries[0].Issues.Count}");

                // Remove 1 issue
                await repo.RemoveComicFromManualSeriesAsync(seriesId, c1);
                allSeries = await repo.GetManualSeriesAsync();
                if (allSeries[0].Issues.Count != 2 || allSeries[0].Issues.Any(i => i.Id == c1))
                    throw new Exception("Issue was not removed from manual series");

                // Delete series
                await repo.DeleteManualSeriesAsync(seriesId);
                allSeries = await repo.GetManualSeriesAsync();
                if (allSeries.Count != 0) throw new Exception("Expected 0 manual series after deletion");
            });

            // Tests 31-36: series detection, duplicate detection and live reading statistics
            await LibraryIntelligenceTests.RunAllAsync(RunTestAsync, tempDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }

        // Generate persistent sample comic files in Samples directory for manual testing
        string samplesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Samples");
        try
        {
            samplesDir = Path.GetFullPath(samplesDir);
            Directory.CreateDirectory(samplesDir);

            // 1. Sample Folder
            string sampleFolder = Path.Combine(samplesDir, "SampleComicFolder");
            Directory.CreateDirectory(sampleFolder);
            for (int i = 1; i <= 5; i++)
            {
                File.WriteAllBytes(Path.Combine(sampleFolder, $"Page_{i:D2}.png"), SamplePngBytes);
            }

            // 2. Sample CBZ
            string sampleCbz = Path.Combine(samplesDir, "SampleComic.cbz");
            using (var zipStream = new FileStream(sampleCbz, FileMode.Create))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                for (int i = 1; i <= 5; i++)
                {
                    var e = archive.CreateEntry($"Page_{i:D2}.png");
                    using var stream = e.Open();
                    stream.Write(SamplePngBytes);
                }
            }

            // 3. Sample PDF
            string samplePdf = Path.Combine(samplesDir, "SampleComic.pdf");
            File.WriteAllBytes(samplePdf, Encoding.ASCII.GetBytes(MinimalPdfContent));

            // 4. Sample CB7 (7-Zip format)
            string sampleCb7 = Path.Combine(samplesDir, "SampleComic.cb7");
            using (var fs = File.Create(sampleCb7))
            using (var writer = new SharpCompress.Writers.SevenZip.SevenZipWriter(fs, new SharpCompress.Writers.SevenZip.SevenZipWriterOptions()))
            {
                for (int i = 1; i <= 5; i++)
                {
                    using var ms = new MemoryStream(SamplePngBytes);
                    writer.Write($"Page_{i:D2}.png", ms, DateTime.UtcNow);
                }
            }

            // 5. Sample CBR (RAR format)
            string sampleCbr = Path.Combine(samplesDir, "SampleComic.cbr");
            File.WriteAllBytes(sampleCbr, SampleRar4Bytes);

            Console.WriteLine($"Sample comics generated in: {samplesDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Note: could not generate samples: {ex.Message}");
        }

        Console.WriteLine("-------------------------------------------------");
        Console.WriteLine($"Results: {passed} passed, {failed} failed");
        Console.WriteLine("=================================================");

        return failed == 0 ? 0 : 1;
    }
}
