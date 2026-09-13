using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Komik.Models;

namespace Komik.Services;

/// <summary>
/// SQLite implementation of ILibraryRepository using Microsoft.Data.Sqlite.
/// </summary>
public sealed class LibraryRepository : ILibraryRepository
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;
    private readonly string _databaseFilePath;

    public static string ResolveDatabasePath()
    {
        string userProfileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".komik");
        string localAppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Komik");

        string durableDbPath = Path.Combine(userProfileDir, "komik_library.db");
        string localDbPath = Path.Combine(localAppDataDir, "komik_library.db");

        // 1. If durable db exists in user profile, use it
        if (File.Exists(durableDbPath))
        {
            return durableDbPath;
        }

        // 2. Auto-detect from local app data and migrate/copy to durable store
        if (File.Exists(localDbPath))
        {
            try
            {
                Directory.CreateDirectory(userProfileDir);
                File.Copy(localDbPath, durableDbPath, overwrite: false);
                return durableDbPath;
            }
            catch
            {
                return localDbPath;
            }
        }

        // 3. Auto-detect from any MSIX package LocalState / LocalCache folder
        try
        {
            string packagesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
            if (Directory.Exists(packagesDir))
            {
                var match = Directory.GetFiles(packagesDir, "komik_library.db", SearchOption.AllDirectories).FirstOrDefault();
                if (match != null && File.Exists(match))
                {
                    Directory.CreateDirectory(userProfileDir);
                    File.Copy(match, durableDbPath, overwrite: false);
                    return durableDbPath;
                }
            }
        }
        catch { }

        try
        {
            Directory.CreateDirectory(userProfileDir);
        }
        catch { }

        return durableDbPath;
    }

    public static string DefaultDatabasePath => ResolveDatabasePath();

    public LibraryRepository(string? databasePath = null)
    {
        string path = databasePath ?? DefaultDatabasePath;
        _databaseFilePath = path;
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            using var conn = CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                PRAGMA foreign_keys = ON;
                PRAGMA journal_mode = WAL;

                CREATE TABLE IF NOT EXISTS WatchedFolders (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    path TEXT UNIQUE NOT NULL,
                    date_added TEXT NOT NULL,
                    last_scanned TEXT
                );

                CREATE TABLE IF NOT EXISTS Comics (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    file_path TEXT UNIQUE NOT NULL,
                    title TEXT NOT NULL,
                    format TEXT NOT NULL,
                    page_count INTEGER NOT NULL DEFAULT 0,
                    thumbnail_path TEXT,
                    date_added TEXT NOT NULL,
                    last_modified TEXT NOT NULL,
                    parent_folder TEXT,
                    file_size INTEGER NOT NULL DEFAULT 0,
                    is_favorite INTEGER NOT NULL DEFAULT 0,
                    is_missing INTEGER NOT NULL DEFAULT 0,
                    last_read_page INTEGER NOT NULL DEFAULT 0,
                    last_read_at TEXT,
                    is_completed INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS Tags (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT UNIQUE NOT NULL COLLATE NOCASE
                );

                CREATE TABLE IF NOT EXISTS ComicTags (
                    comic_id INTEGER NOT NULL,
                    tag_id INTEGER NOT NULL,
                    PRIMARY KEY(comic_id, tag_id),
                    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE,
                    FOREIGN KEY(tag_id) REFERENCES Tags(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Collections (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT UNIQUE NOT NULL COLLATE NOCASE
                );

                CREATE TABLE IF NOT EXISTS ComicCollections (
                    comic_id INTEGER NOT NULL,
                    collection_id INTEGER NOT NULL,
                    PRIMARY KEY(comic_id, collection_id),
                    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE,
                    FOREIGN KEY(collection_id) REFERENCES Collections(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS Bookmarks (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    comic_id INTEGER NOT NULL,
                    page_number INTEGER NOT NULL,
                    user_note TEXT,
                    created_at TEXT NOT NULL,
                    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ComicMetadata (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    comic_id INTEGER UNIQUE NOT NULL,
                    title TEXT,
                    issue_number TEXT,
                    series_name TEXT,
                    writers TEXT,
                    artists TEXT,
                    publisher TEXT,
                    release_date TEXT,
                    summary TEXT,
                    last_updated TEXT NOT NULL,
                    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS AppSettings (
                    key TEXT PRIMARY KEY NOT NULL,
                    value TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_comics_file_path ON Comics(file_path);
                CREATE INDEX IF NOT EXISTS idx_comics_title ON Comics(title);
                CREATE INDEX IF NOT EXISTS idx_comics_is_favorite ON Comics(is_favorite);
                CREATE INDEX IF NOT EXISTS idx_comics_last_read_at ON Comics(last_read_at);
                CREATE INDEX IF NOT EXISTS idx_comic_tags_comic ON ComicTags(comic_id);
                CREATE INDEX IF NOT EXISTS idx_comic_tags_tag ON ComicTags(tag_id);
                CREATE INDEX IF NOT EXISTS idx_comic_colls_comic ON ComicCollections(comic_id);
                CREATE INDEX IF NOT EXISTS idx_comic_colls_coll ON ComicCollections(collection_id);
                CREATE INDEX IF NOT EXISTS idx_bookmarks_comic ON Bookmarks(comic_id);
                CREATE INDEX IF NOT EXISTS idx_comic_metadata_comic ON ComicMetadata(comic_id);
            ";

            await cmd.ExecuteNonQueryAsync();
            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    #region Watched Folders

    public async Task<IReadOnlyList<WatchedFolder>> GetWatchedFoldersAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, path, date_added, last_scanned FROM WatchedFolders ORDER BY date_added DESC;";

        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<WatchedFolder>();
        while (await reader.ReadAsync())
        {
            list.Add(new WatchedFolder
            {
                Id = reader.GetInt64(0),
                Path = reader.GetString(1),
                DateAdded = DateTime.Parse(reader.GetString(2)),
                LastScanned = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3))
            });
        }
        return list;
    }

    public async Task<WatchedFolder> AddWatchedFolderAsync(string path)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        string now = DateTime.UtcNow.ToString("O");
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO WatchedFolders (path, date_added, last_scanned)
            VALUES (@path, @date_added, NULL)
            ON CONFLICT(path) DO UPDATE SET date_added = excluded.date_added
            RETURNING id;
        ";
        cmd.Parameters.AddWithValue("@path", path);
        cmd.Parameters.AddWithValue("@date_added", now);

        long id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
        return new WatchedFolder
        {
            Id = id,
            Path = path,
            DateAdded = DateTime.Parse(now),
            LastScanned = null
        };
    }

    public async Task RemoveWatchedFolderAsync(long id)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM WatchedFolders WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateWatchedFolderLastScannedAsync(long id, DateTime lastScanned)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE WatchedFolders SET last_scanned = @last_scanned WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@last_scanned", lastScanned.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    #endregion

    #region Comics

    public async Task<IReadOnlyList<ComicEntity>> GetComicsAsync(LibraryFilter? filter = null, LibrarySortOption sort = LibrarySortOption.TitleAscending)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var sb = new StringBuilder(@"
            SELECT c.id, c.file_path, c.title, c.format, c.page_count, c.thumbnail_path,
                   c.date_added, c.last_modified, c.parent_folder, c.file_size,
                   c.is_favorite, c.is_missing, c.last_read_page, c.last_read_at, c.is_completed
            FROM Comics c
        ");

        var whereClauses = new List<string>();
        using var cmd = conn.CreateCommand();

        if (filter == null || !filter.IncludeMissing)
        {
            whereClauses.Add("c.is_missing = 0");
        }

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                whereClauses.Add("c.title LIKE @query");
                cmd.Parameters.AddWithValue("@query", $"%{filter.SearchQuery.Trim()}%");
            }

            if (filter.FavoritesOnly)
            {
                whereClauses.Add("c.is_favorite = 1");
            }

            if (filter.InProgressOnly)
            {
                whereClauses.Add("c.is_completed = 0 AND (c.last_read_page > 0 OR c.last_read_at IS NOT NULL)");
            }
            else if (filter.UnreadOnly)
            {
                whereClauses.Add("c.is_completed = 0 AND (c.last_read_page = 0 OR c.last_read_page IS NULL) AND c.last_read_at IS NULL");
            }
            else if (filter.CompletedOnly)
            {
                whereClauses.Add("c.is_completed = 1");
            }

            if (filter.SelectedFormat.HasValue)
            {
                whereClauses.Add("c.format = @format");
                cmd.Parameters.AddWithValue("@format", filter.SelectedFormat.Value.ToString());
            }

            if (!string.IsNullOrWhiteSpace(filter.SelectedTag))
            {
                whereClauses.Add(@"
                    c.id IN (
                        SELECT ct.comic_id FROM ComicTags ct
                        JOIN Tags t ON ct.tag_id = t.id
                        WHERE t.name = @tag
                    )
                ");
                cmd.Parameters.AddWithValue("@tag", filter.SelectedTag);
            }

            if (!string.IsNullOrWhiteSpace(filter.SelectedCollection))
            {
                whereClauses.Add(@"
                    (c.parent_folder = @collection OR c.id IN (
                        SELECT cc.comic_id FROM ComicCollections cc
                        JOIN Collections col ON cc.collection_id = col.id
                        WHERE col.name = @collection
                    ))
                ");
                cmd.Parameters.AddWithValue("@collection", filter.SelectedCollection);
            }
        }

        if (whereClauses.Count > 0)
        {
            sb.Append(" WHERE ");
            sb.Append(string.Join(" AND ", whereClauses));
        }

        // Sorting
        sb.Append(" ORDER BY ");
        sb.Append(sort switch
        {
            LibrarySortOption.TitleDescending => "c.title DESC",
            LibrarySortOption.DateAddedDescending => "c.date_added DESC",
            LibrarySortOption.DateAddedAscending => "c.date_added ASC",
            LibrarySortOption.PageCountDescending => "c.page_count DESC",
            LibrarySortOption.FileSizeDescending => "c.file_size DESC",
            LibrarySortOption.LastReadDescending => "c.last_read_at DESC, c.title ASC",
            _ => "c.title ASC"
        });

        cmd.CommandText = sb.ToString();

        using var reader = await cmd.ExecuteReaderAsync();
        var comics = new List<ComicEntity>();
        while (await reader.ReadAsync())
        {
            comics.Add(MapComicEntity(reader));
        }

        // Populate tags and collections
        foreach (var comic in comics)
        {
            comic.Tags = (await GetTagsForComicInternalAsync(conn, comic.Id));
            comic.Collections = (await GetCollectionsForComicInternalAsync(conn, comic.Id));
        }

        return comics;
    }

    public async Task<ComicEntity?> GetComicByIdAsync(long id)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, file_path, title, format, page_count, thumbnail_path,
                   date_added, last_modified, parent_folder, file_size,
                   is_favorite, is_missing, last_read_page, last_read_at, is_completed
            FROM Comics WHERE id = @id;
        ";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var comic = MapComicEntity(reader);
            comic.Tags = await GetTagsForComicInternalAsync(conn, comic.Id);
            comic.Collections = await GetCollectionsForComicInternalAsync(conn, comic.Id);
            return comic;
        }
        return null;
    }

    public async Task<ComicEntity?> GetComicByPathAsync(string path)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, file_path, title, format, page_count, thumbnail_path,
                   date_added, last_modified, parent_folder, file_size,
                   is_favorite, is_missing, last_read_page, last_read_at, is_completed
            FROM Comics WHERE file_path = @file_path;
        ";
        cmd.Parameters.AddWithValue("@file_path", path);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var comic = MapComicEntity(reader);
            comic.Tags = await GetTagsForComicInternalAsync(conn, comic.Id);
            comic.Collections = await GetCollectionsForComicInternalAsync(conn, comic.Id);
            return comic;
        }
        return null;
    }

    public async Task<long> InsertComicAsync(ComicEntity comic)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Comics (
                file_path, title, format, page_count, thumbnail_path,
                date_added, last_modified, parent_folder, file_size,
                is_favorite, is_missing, last_read_page, last_read_at, is_completed
            ) VALUES (
                @file_path, @title, @format, @page_count, @thumbnail_path,
                @date_added, @last_modified, @parent_folder, @file_size,
                @is_favorite, @is_missing, @last_read_page, @last_read_at, @is_completed
            )
            ON CONFLICT(file_path) DO UPDATE SET
                title = excluded.title,
                page_count = excluded.page_count,
                thumbnail_path = COALESCE(excluded.thumbnail_path, Comics.thumbnail_path),
                last_modified = excluded.last_modified,
                parent_folder = COALESCE(excluded.parent_folder, Comics.parent_folder),
                file_size = excluded.file_size,
                is_missing = 0
            RETURNING id;
        ";

        cmd.Parameters.AddWithValue("@file_path", comic.FilePath);
        cmd.Parameters.AddWithValue("@title", comic.Title);
        cmd.Parameters.AddWithValue("@format", comic.Format.ToString());
        cmd.Parameters.AddWithValue("@page_count", comic.PageCount);
        cmd.Parameters.AddWithValue("@thumbnail_path", (object?)comic.ThumbnailPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@date_added", comic.DateAdded.ToString("O"));
        cmd.Parameters.AddWithValue("@last_modified", comic.LastModified.ToString("O"));
        cmd.Parameters.AddWithValue("@parent_folder", (object?)comic.ParentFolder ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@file_size", comic.FileSize);
        cmd.Parameters.AddWithValue("@is_favorite", comic.IsFavorite ? 1 : 0);
        cmd.Parameters.AddWithValue("@is_missing", comic.IsMissing ? 1 : 0);
        cmd.Parameters.AddWithValue("@last_read_page", comic.LastReadPage);
        cmd.Parameters.AddWithValue("@last_read_at", (object?)comic.LastReadAt?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@is_completed", comic.IsCompleted ? 1 : 0);

        long id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
        comic.Id = id;
        return id;
    }

    public async Task UpdateComicAsync(ComicEntity comic)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Comics SET
                title = @title,
                page_count = @page_count,
                thumbnail_path = @thumbnail_path,
                last_modified = @last_modified,
                file_size = @file_size,
                is_favorite = @is_favorite,
                is_missing = @is_missing,
                last_read_page = @last_read_page,
                last_read_at = @last_read_at,
                is_completed = @is_completed
            WHERE id = @id;
        ";

        cmd.Parameters.AddWithValue("@id", comic.Id);
        cmd.Parameters.AddWithValue("@title", comic.Title);
        cmd.Parameters.AddWithValue("@page_count", comic.PageCount);
        cmd.Parameters.AddWithValue("@thumbnail_path", (object?)comic.ThumbnailPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@last_modified", comic.LastModified.ToString("O"));
        cmd.Parameters.AddWithValue("@file_size", comic.FileSize);
        cmd.Parameters.AddWithValue("@is_favorite", comic.IsFavorite ? 1 : 0);
        cmd.Parameters.AddWithValue("@is_missing", comic.IsMissing ? 1 : 0);
        cmd.Parameters.AddWithValue("@last_read_page", comic.LastReadPage);
        cmd.Parameters.AddWithValue("@last_read_at", (object?)comic.LastReadAt?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@is_completed", comic.IsCompleted ? 1 : 0);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SetFavoriteAsync(long comicId, bool isFavorite)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Comics SET is_favorite = @fav WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", comicId);
        cmd.Parameters.AddWithValue("@fav", isFavorite ? 1 : 0);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SetMissingStatusAsync(long comicId, bool isMissing)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Comics SET is_missing = @missing WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", comicId);
        cmd.Parameters.AddWithValue("@missing", isMissing ? 1 : 0);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RemoveComicAsync(long comicId)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Comics WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", comicId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateThumbnailPathAsync(long comicId, string? thumbnailPath)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Comics SET thumbnail_path = @thumb WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", comicId);
        cmd.Parameters.AddWithValue("@thumb", (object?)thumbnailPath ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ClearAllComicThumbnailsAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Comics SET thumbnail_path = NULL;";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateReadingProgressAsync(string filePath, int pageIndex, int totalPages)
    {
        try
        {
            await InitializeAsync();
            using var conn = CreateConnection();
            await conn.OpenAsync();

            string now = DateTime.UtcNow.ToString("O");
            bool isCompleted = totalPages > 0 && pageIndex >= totalPages - 1;

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Comics SET
                    last_read_page = @page,
                    last_read_at = @now,
                    is_completed = @isCompleted
                WHERE file_path = @file_path;
            ";
            cmd.Parameters.AddWithValue("@page", pageIndex);
            cmd.Parameters.AddWithValue("@now", now);
            cmd.Parameters.AddWithValue("@isCompleted", isCompleted ? 1 : 0);
            cmd.Parameters.AddWithValue("@file_path", filePath);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LibraryRepository] Failed to update progress: {ex.Message}");
        }
    }

    public async Task SetCompletedStatusAsync(long comicId, bool isCompleted)
    {
        try
        {
            await InitializeAsync();
            using var conn = CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            if (isCompleted)
            {
                cmd.CommandText = "UPDATE Comics SET is_completed = 1, last_read_at = @now WHERE id = @id;";
                cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
            }
            else
            {
                cmd.CommandText = "UPDATE Comics SET is_completed = 0, last_read_page = 0, last_read_at = NULL WHERE id = @id;";
            }
            cmd.Parameters.AddWithValue("@id", comicId);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LibraryRepository] Failed to set completed status: {ex.Message}");
        }
    }

    #endregion

    #region Tags

    public async Task<IReadOnlyList<string>> GetAllTagsAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM Tags ORDER BY name COLLATE NOCASE ASC;";

        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<string>();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }
        return list;
    }

    public async Task<IReadOnlyList<string>> GetTagsForComicAsync(long comicId)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        return await GetTagsForComicInternalAsync(conn, comicId);
    }

    private static async Task<List<string>> GetTagsForComicInternalAsync(SqliteConnection conn, long comicId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT t.name FROM Tags t
            JOIN ComicTags ct ON t.id = ct.tag_id
            WHERE ct.comic_id = @comic_id
            ORDER BY t.name COLLATE NOCASE ASC;
        ";
        cmd.Parameters.AddWithValue("@comic_id", comicId);

        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<string>();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }
        return list;
    }

    public async Task AddTagToComicAsync(long comicId, string tagName)
    {
        string trimmed = tagName.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var transaction = conn.BeginTransaction();

        // 1. Get or create tag
        using var cmdTag = conn.CreateCommand();
        cmdTag.Transaction = transaction;
        cmdTag.CommandText = @"
            INSERT INTO Tags (name) VALUES (@name)
            ON CONFLICT(name) DO UPDATE SET name = excluded.name
            RETURNING id;
        ";
        cmdTag.Parameters.AddWithValue("@name", trimmed);
        long tagId = Convert.ToInt64(await cmdTag.ExecuteScalarAsync());

        // 2. Link comic and tag
        using var cmdLink = conn.CreateCommand();
        cmdLink.Transaction = transaction;
        cmdLink.CommandText = @"
            INSERT OR IGNORE INTO ComicTags (comic_id, tag_id)
            VALUES (@comic_id, @tag_id);
        ";
        cmdLink.Parameters.AddWithValue("@comic_id", comicId);
        cmdLink.Parameters.AddWithValue("@tag_id", tagId);
        await cmdLink.ExecuteNonQueryAsync();

        await transaction.CommitAsync();
    }

    public async Task RemoveTagFromComicAsync(long comicId, string tagName)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM ComicTags
            WHERE comic_id = @comic_id
              AND tag_id IN (SELECT id FROM Tags WHERE name = @name);
        ";
        cmd.Parameters.AddWithValue("@comic_id", comicId);
        cmd.Parameters.AddWithValue("@name", tagName.Trim());
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task CreateTagAsync(string tagName)
    {
        string trimmed = tagName.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO Tags (name) VALUES (@name);";
        cmd.Parameters.AddWithValue("@name", trimmed);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteTagAsync(string tagName)
    {
        string trimmed = tagName.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Tags WHERE name = @name;";
        cmd.Parameters.AddWithValue("@name", trimmed);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> GetComicCountForTagAsync(string tagName)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*) FROM ComicTags ct
            JOIN Tags t ON ct.tag_id = t.id
            WHERE t.name = @name;
        ";
        cmd.Parameters.AddWithValue("@name", tagName);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    #endregion

    #region Collections

    public async Task<IReadOnlyList<string>> GetAllCollectionsAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM Collections ORDER BY name COLLATE NOCASE ASC;";

        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<string>();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }
        return list;
    }

    public async Task<IReadOnlyList<string>> GetCollectionsForComicAsync(long comicId)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        return await GetCollectionsForComicInternalAsync(conn, comicId);
    }

    private static async Task<List<string>> GetCollectionsForComicInternalAsync(SqliteConnection conn, long comicId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT col.name FROM Collections col
            JOIN ComicCollections cc ON col.id = cc.collection_id
            WHERE cc.comic_id = @comic_id
            ORDER BY col.name COLLATE NOCASE ASC;
        ";
        cmd.Parameters.AddWithValue("@comic_id", comicId);

        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<string>();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }
        return list;
    }

    public async Task AddToCollectionAsync(long comicId, string collectionName)
    {
        string trimmed = collectionName.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var transaction = conn.BeginTransaction();

        // 1. Get or create collection
        using var cmdCol = conn.CreateCommand();
        cmdCol.Transaction = transaction;
        cmdCol.CommandText = @"
            INSERT INTO Collections (name) VALUES (@name)
            ON CONFLICT(name) DO UPDATE SET name = excluded.name
            RETURNING id;
        ";
        cmdCol.Parameters.AddWithValue("@name", trimmed);
        long colId = Convert.ToInt64(await cmdCol.ExecuteScalarAsync());

        // 2. Link comic and collection
        using var cmdLink = conn.CreateCommand();
        cmdLink.Transaction = transaction;
        cmdLink.CommandText = @"
            INSERT OR IGNORE INTO ComicCollections (comic_id, collection_id)
            VALUES (@comic_id, @col_id);
        ";
        cmdLink.Parameters.AddWithValue("@comic_id", comicId);
        cmdLink.Parameters.AddWithValue("@col_id", colId);
        await cmdLink.ExecuteNonQueryAsync();

        await transaction.CommitAsync();
    }

    public async Task RemoveFromCollectionAsync(long comicId, string collectionName)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM ComicCollections
            WHERE comic_id = @comic_id
              AND collection_id IN (SELECT id FROM Collections WHERE name = @name);
        ";
        cmd.Parameters.AddWithValue("@comic_id", comicId);
        cmd.Parameters.AddWithValue("@name", collectionName.Trim());
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task CreateCollectionAsync(string collectionName)
    {
        string trimmed = collectionName.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO Collections (name) VALUES (@name);";
        cmd.Parameters.AddWithValue("@name", trimmed);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteCollectionAsync(string collectionName)
    {
        string trimmed = collectionName.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Collections WHERE name = @name;";
        cmd.Parameters.AddWithValue("@name", trimmed);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> GetComicCountForCollectionAsync(string collectionName)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*) FROM Comics 
            WHERE parent_folder = @name 
               OR id IN (
                   SELECT cc.comic_id FROM ComicCollections cc
                   JOIN Collections c ON cc.collection_id = c.id
                   WHERE c.name = @name
               );
        ";
        cmd.Parameters.AddWithValue("@name", collectionName);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<IReadOnlyList<string>> GetCollectionFoldersAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT DISTINCT parent_folder FROM Comics WHERE parent_folder IS NOT NULL AND parent_folder != ''
            UNION
            SELECT name FROM Collections WHERE name IS NOT NULL AND name != ''
            ORDER BY 1 ASC;
        ";
        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<string>();
        while (await reader.ReadAsync())
        {
            string name = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(name))
            {
                list.Add(name);
            }
        }
        return list;
    }

    public async Task<int> GetComicCountInFolderAsync(string folderName)
    {
        return await GetComicCountForCollectionAsync(folderName);
    }

    public async Task<IReadOnlyList<ApplicationFolder>> GetApplicationFoldersAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT col.id, col.name,
                   (
                       SELECT COUNT(DISTINCT c.id) FROM Comics c
                       WHERE c.is_missing = 0 AND (
                           c.parent_folder = col.name OR c.id IN (
                               SELECT cc.comic_id FROM ComicCollections cc WHERE cc.collection_id = col.id
                           )
                       )
                   ) AS comic_count,
                   (
                       SELECT c.thumbnail_path FROM Comics c
                       WHERE c.is_missing = 0 AND c.thumbnail_path IS NOT NULL AND (
                           c.parent_folder = col.name OR c.id IN (
                               SELECT cc.comic_id FROM ComicCollections cc WHERE cc.collection_id = col.id
                           )
                       )
                       LIMIT 1
                   ) AS cover_thumb
            FROM Collections col
            ORDER BY col.name ASC;
        ";

        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<ApplicationFolder>();
        while (await reader.ReadAsync())
        {
            list.Add(new ApplicationFolder
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                ComicCount = reader.GetInt32(2),
                CoverThumbnailPath = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }
        return list;
    }

    public async Task CreateFolderAsync(string name)
    {
        await CreateCollectionAsync(name);
        _ = BackupLibraryDataCacheAsync();
    }

    public async Task RenameFolderAsync(string oldName, string newName)
    {
        string oldTrimmed = oldName.Trim();
        string newTrimmed = newName.Trim();
        if (string.IsNullOrEmpty(oldTrimmed) || string.IsNullOrEmpty(newTrimmed)) return;

        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Collections SET name = @newName WHERE name = @oldName;
            UPDATE Comics SET parent_folder = @newName WHERE parent_folder = @oldName;
        ";
        cmd.Parameters.AddWithValue("@oldName", oldTrimmed);
        cmd.Parameters.AddWithValue("@newName", newTrimmed);
        await cmd.ExecuteNonQueryAsync();
        _ = BackupLibraryDataCacheAsync();
    }

    public async Task DeleteFolderAsync(string name)
    {
        string trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM ComicCollections 
            WHERE collection_id IN (SELECT id FROM Collections WHERE name = @name);

            DELETE FROM Collections WHERE name = @name;

            UPDATE Comics SET parent_folder = NULL WHERE parent_folder = @name;
        ";
        cmd.Parameters.AddWithValue("@name", trimmed);
        await cmd.ExecuteNonQueryAsync();
        _ = BackupLibraryDataCacheAsync();
    }

    public async Task AddComicToFolderAsync(long comicId, string folderName)
    {
        string trimmed = folderName.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        await CreateFolderAsync(trimmed);

        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "UPDATE Comics SET parent_folder = @folder WHERE id = @id;";
            cmd.Parameters.AddWithValue("@folder", trimmed);
            cmd.Parameters.AddWithValue("@id", comicId);
            await cmd.ExecuteNonQueryAsync();
        }

        await AddToCollectionAsync(comicId, trimmed);
        _ = BackupLibraryDataCacheAsync();
    }

    public async Task RemoveComicFromFolderAsync(long comicId, string folderName)
    {
        string trimmed = folderName.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                UPDATE Comics SET parent_folder = NULL 
                WHERE id = @id AND parent_folder = @folder;
            ";
            cmd.Parameters.AddWithValue("@folder", trimmed);
            cmd.Parameters.AddWithValue("@id", comicId);
            await cmd.ExecuteNonQueryAsync();
        }

        await RemoveFromCollectionAsync(comicId, trimmed);
        _ = BackupLibraryDataCacheAsync();
    }

    public async Task MoveComicToCollectionFolderAsync(long comicId, string targetFolderName)
    {
        await AddComicToFolderAsync(comicId, targetFolderName);
    }

    public async Task RemoveComicsInWatchedFolderAsync(string folderPath)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        // NON-DESTRUCTIVE: Mark as is_missing = 1 so reading progress, metadata, tags, and folders are never lost
        cmd.CommandText = @"
            UPDATE Comics 
            SET is_missing = 1 
            WHERE file_path LIKE @prefix || '%' 
               OR file_path = @folder;
        ";
        cmd.Parameters.AddWithValue("@prefix", folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);
        cmd.Parameters.AddWithValue("@folder", folderPath);
        await cmd.ExecuteNonQueryAsync();
        _ = BackupLibraryDataCacheAsync();
    }

    #region Persistent Cache & Backup

    private static string GetCacheDataFilePath()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".komik");
        try { Directory.CreateDirectory(dir); } catch { }
        return Path.Combine(dir, "library_data_cache.json");
    }

    public async Task BackupLibraryDataCacheAsync()
    {
        try
        {
            string cachePath = GetCacheDataFilePath();
            var comics = await GetComicsAsync(new LibraryFilter { IncludeMissing = true });
            var list = new List<ComicCacheItem>();
            foreach (var c in comics)
            {
                var meta = await GetMetadataForComicAsync(c.Id);
                var tags = await GetTagsForComicAsync(c.Id);
                var colls = await GetCollectionsForComicAsync(c.Id);
                list.Add(new ComicCacheItem
                {
                    FilePath = c.FilePath,
                    Title = c.Title,
                    Format = c.Format.ToString(),
                    PageCount = c.PageCount,
                    ThumbnailPath = c.ThumbnailPath,
                    ParentFolder = c.ParentFolder,
                    FileSize = c.FileSize,
                    IsFavorite = c.IsFavorite,
                    IsCompleted = c.IsCompleted,
                    LastReadPage = c.LastReadPage,
                    LastReadAt = c.LastReadAt,
                    Tags = new List<string>(tags),
                    Collections = new List<string>(colls),
                    Metadata = meta
                });
            }

            string json = System.Text.Json.JsonSerializer.Serialize(list, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(cachePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LibraryRepository] BackupLibraryDataCache error: {ex.Message}");
        }
    }

    public async Task RestoreLibraryDataCacheAsync()
    {
        try
        {
            string cachePath = GetCacheDataFilePath();
            if (!File.Exists(cachePath)) return;

            string json = await File.ReadAllTextAsync(cachePath);
            var list = System.Text.Json.JsonSerializer.Deserialize<List<ComicCacheItem>>(json);
            if (list == null || list.Count == 0) return;

            foreach (var item in list)
            {
                var existing = await GetComicByPathAsync(item.FilePath);
                long comicId;
                if (existing == null)
                {
                    if (!Enum.TryParse<ComicSourceType>(item.Format, out var format)) format = ComicSourceType.ZipArchive;
                    comicId = await InsertComicAsync(new ComicEntity
                    {
                        FilePath = item.FilePath,
                        Title = item.Title,
                        Format = format,
                        PageCount = item.PageCount,
                        ThumbnailPath = item.ThumbnailPath,
                        ParentFolder = item.ParentFolder,
                        FileSize = item.FileSize,
                        IsFavorite = item.IsFavorite,
                        IsCompleted = item.IsCompleted,
                        LastReadPage = item.LastReadPage,
                        LastReadAt = item.LastReadAt,
                        IsMissing = false
                    });
                }
                else
                {
                    comicId = existing.Id;
                    if (item.IsCompleted != existing.IsCompleted) await SetCompletedStatusAsync(comicId, item.IsCompleted);
                    if (item.LastReadPage > existing.LastReadPage) await UpdateReadingProgressAsync(item.FilePath, item.LastReadPage, item.PageCount);
                    if (item.IsFavorite != existing.IsFavorite) await SetFavoriteAsync(comicId, item.IsFavorite);
                    if (!string.IsNullOrEmpty(item.ParentFolder) && existing.ParentFolder != item.ParentFolder)
                    {
                        await AddComicToFolderAsync(comicId, item.ParentFolder);
                    }
                }

                if (item.Metadata != null)
                {
                    item.Metadata.ComicId = comicId;
                    await SaveComicMetadataAsync(item.Metadata);
                }

                foreach (var tag in item.Tags)
                {
                    await AddTagToComicAsync(comicId, tag);
                }

                foreach (var coll in item.Collections)
                {
                    await AddComicToFolderAsync(comicId, coll);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LibraryRepository] RestoreLibraryDataCache error: {ex.Message}");
        }
    }

    private sealed class ComicCacheItem
    {
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public int PageCount { get; set; }
        public string? ThumbnailPath { get; set; }
        public string? ParentFolder { get; set; }
        public long FileSize { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsCompleted { get; set; }
        public int LastReadPage { get; set; }
        public DateTime? LastReadAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<string> Collections { get; set; } = new();
        public ComicMetadataEntity? Metadata { get; set; }
    }

    #endregion

    #endregion

    #region Bookmarks

    public async Task<IReadOnlyList<BookmarkEntity>> GetBookmarksForComicAsync(long comicId)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, comic_id, page_number, user_note, created_at
            FROM Bookmarks
            WHERE comic_id = @comic_id
            ORDER BY page_number ASC;
        ";
        cmd.Parameters.AddWithValue("@comic_id", comicId);

        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<BookmarkEntity>();
        while (await reader.ReadAsync())
        {
            list.Add(new BookmarkEntity
            {
                Id = reader.GetInt64(0),
                ComicId = reader.GetInt64(1),
                PageIndex = reader.GetInt32(2),
                UserNote = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4))
            });
        }
        return list;
    }

    public async Task<BookmarkEntity> AddBookmarkAsync(long comicId, int pageIndex, string? userNote = null)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        string now = DateTime.UtcNow.ToString("O");
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Bookmarks (comic_id, page_number, user_note, created_at)
            VALUES (@comic_id, @page, @note, @created)
            RETURNING id;
        ";
        cmd.Parameters.AddWithValue("@comic_id", comicId);
        cmd.Parameters.AddWithValue("@page", pageIndex);
        cmd.Parameters.AddWithValue("@note", (object?)userNote ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@created", now);

        long id = Convert.ToInt64(await cmd.ExecuteScalarAsync());
        return new BookmarkEntity
        {
            Id = id,
            ComicId = comicId,
            PageIndex = pageIndex,
            UserNote = userNote,
            CreatedAt = DateTime.Parse(now)
        };
    }

    public async Task RemoveBookmarkAsync(long bookmarkId)
    {
        try
        {
            await InitializeAsync();
            using var conn = CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Bookmarks WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", bookmarkId);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LibraryRepository] Failed to remove bookmark: {ex.Message}");
        }
    }

    public async Task<bool> IsPageBookmarkedAsync(long comicId, int pageIndex)
    {
        try
        {
            await InitializeAsync();
            using var conn = CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Bookmarks WHERE comic_id = @comic_id AND page_number = @page;";
            cmd.Parameters.AddWithValue("@comic_id", comicId);
            cmd.Parameters.AddWithValue("@page", pageIndex);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Counts

    public async Task<int> GetTotalComicCountAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Comics WHERE is_missing = 0;";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> GetFavoritesCountAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Comics WHERE is_favorite = 1 AND is_missing = 0;";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> GetInProgressCountAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Comics WHERE is_completed = 0 AND is_missing = 0 AND (last_read_page > 0 OR last_read_at IS NOT NULL);";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> GetUnreadCountAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Comics WHERE is_completed = 0 AND is_missing = 0 AND (last_read_page = 0 OR last_read_page IS NULL) AND last_read_at IS NULL;";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<int> GetCompletedCountAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Comics WHERE is_completed = 1 AND is_missing = 0;";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    #endregion

    private static ComicEntity MapComicEntity(DbDataReader reader)
    {
        return new ComicEntity
        {
            Id = reader.GetInt64(0),
            FilePath = reader.GetString(1),
            Title = reader.GetString(2),
            Format = Enum.Parse<ComicSourceType>(reader.GetString(3)),
            PageCount = reader.GetInt32(4),
            ThumbnailPath = reader.IsDBNull(5) ? null : reader.GetString(5),
            DateAdded = DateTime.Parse(reader.GetString(6)),
            LastModified = DateTime.Parse(reader.GetString(7)),
            ParentFolder = reader.IsDBNull(8) ? null : reader.GetString(8),
            FileSize = reader.GetInt64(9),
            IsFavorite = reader.GetInt32(10) == 1,
            IsMissing = reader.GetInt32(11) == 1,
            LastReadPage = reader.GetInt32(12),
            LastReadAt = reader.IsDBNull(13) ? null : DateTime.Parse(reader.GetString(13)),
            IsCompleted = reader.GetInt32(14) == 1
        };
    }

    #region Extended Comic Metadata
 
    public async Task<ComicMetadataEntity?> GetMetadataForComicAsync(long comicId)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, comic_id, title, issue_number, series_name, writers, artists, publisher, release_date, summary, last_updated
            FROM ComicMetadata
            WHERE comic_id = @comicId
            LIMIT 1;
        ";
        cmd.Parameters.AddWithValue("@comicId", comicId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ComicMetadataEntity
            {
                Id = reader.GetInt64(0),
                ComicId = reader.GetInt64(1),
                Title = reader.IsDBNull(2) ? null : reader.GetString(2),
                IssueNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                SeriesName = reader.IsDBNull(4) ? null : reader.GetString(4),
                Writers = reader.IsDBNull(5) ? null : reader.GetString(5),
                Artists = reader.IsDBNull(6) ? null : reader.GetString(6),
                Publisher = reader.IsDBNull(7) ? null : reader.GetString(7),
                ReleaseDate = reader.IsDBNull(8) ? null : reader.GetString(8),
                Summary = reader.IsDBNull(9) ? null : reader.GetString(9),
                LastUpdated = DateTime.Parse(reader.GetString(10))
            };
        }
        return null;
    }

    public async Task SaveComicMetadataAsync(ComicMetadataEntity metadata)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO ComicMetadata (comic_id, title, issue_number, series_name, writers, artists, publisher, release_date, summary, last_updated)
            VALUES (@comicId, @title, @issueNumber, @seriesName, @writers, @artists, @publisher, @releaseDate, @summary, @lastUpdated)
            ON CONFLICT(comic_id) DO UPDATE SET
                title = excluded.title,
                issue_number = excluded.issue_number,
                series_name = excluded.series_name,
                writers = excluded.writers,
                artists = excluded.artists,
                publisher = excluded.publisher,
                release_date = excluded.release_date,
                summary = excluded.summary,
                last_updated = excluded.last_updated;
        ";
        cmd.Parameters.AddWithValue("@comicId", metadata.ComicId);
        cmd.Parameters.AddWithValue("@title", (object?)metadata.Title ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@issueNumber", (object?)metadata.IssueNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@seriesName", (object?)metadata.SeriesName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@writers", (object?)metadata.Writers ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@artists", (object?)metadata.Artists ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@publisher", (object?)metadata.Publisher ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@releaseDate", (object?)metadata.ReleaseDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@summary", (object?)metadata.Summary ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@lastUpdated", metadata.LastUpdated.ToString("o"));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteComicMetadataAsync(long comicId)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM ComicMetadata WHERE comic_id = @comicId;";
        cmd.Parameters.AddWithValue("@comicId", comicId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateComicTitleAsync(long comicId, string title)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Comics SET title = @title WHERE id = @id;";
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@id", comicId);
        await cmd.ExecuteNonQueryAsync();
    }

    #endregion

    #region App Settings

    public async Task<string?> GetSettingAsync(string key)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM AppSettings WHERE key = @key LIMIT 1;";
        cmd.Parameters.AddWithValue("@key", key);

        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString();
    }

    public async Task SetSettingAsync(string key, string value)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO AppSettings (key, value) VALUES (@key, @value)
            ON CONFLICT(key) DO UPDATE SET value = excluded.value;
        ";
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<AppSettings> GetAppSettingsAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT key, value FROM AppSettings;";

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            dict[reader.GetString(0)] = reader.GetString(1);
        }

        var settings = new AppSettings();
        if (dict.TryGetValue(nameof(AppSettings.Theme), out var theme)) settings.Theme = theme;
        if (dict.TryGetValue(nameof(AppSettings.DefaultFitMode), out var fit)) settings.DefaultFitMode = fit;
        if (dict.TryGetValue(nameof(AppSettings.DefaultReadingDirection), out var dir)) settings.DefaultReadingDirection = dir;
        if (dict.TryGetValue(nameof(AppSettings.DefaultViewMode), out var view)) settings.DefaultViewMode = view;
        if (dict.TryGetValue(nameof(AppSettings.DefaultSortOption), out var sort) && int.TryParse(sort, out int s)) settings.DefaultSortOption = s;
        if (dict.TryGetValue(nameof(AppSettings.DefaultBrightness), out var bright) && double.TryParse(bright, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double b)) settings.DefaultBrightness = b;
        if (dict.TryGetValue(nameof(AppSettings.DefaultContrast), out var contrast) && double.TryParse(contrast, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double c)) settings.DefaultContrast = c;
        if (dict.TryGetValue(nameof(AppSettings.DefaultWarmth), out var warm) && double.TryParse(warm, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double w)) settings.DefaultWarmth = w;
        if (dict.TryGetValue(nameof(AppSettings.DefaultNightMode), out var nm) && bool.TryParse(nm, out bool n)) settings.DefaultNightMode = n;

        return settings;
    }

    public async Task SaveAppSettingsAsync(AppSettings settings)
    {
        await SetSettingAsync(nameof(AppSettings.Theme), settings.Theme);
        await SetSettingAsync(nameof(AppSettings.DefaultFitMode), settings.DefaultFitMode);
        await SetSettingAsync(nameof(AppSettings.DefaultReadingDirection), settings.DefaultReadingDirection);
        await SetSettingAsync(nameof(AppSettings.DefaultViewMode), settings.DefaultViewMode);
        await SetSettingAsync(nameof(AppSettings.DefaultSortOption), settings.DefaultSortOption.ToString());
        await SetSettingAsync(nameof(AppSettings.DefaultBrightness), settings.DefaultBrightness.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await SetSettingAsync(nameof(AppSettings.DefaultContrast), settings.DefaultContrast.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await SetSettingAsync(nameof(AppSettings.DefaultWarmth), settings.DefaultWarmth.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await SetSettingAsync(nameof(AppSettings.DefaultNightMode), settings.DefaultNightMode.ToString());
    }

    #endregion

    public void Dispose()
    {
        _initLock.Dispose();
    }
}
