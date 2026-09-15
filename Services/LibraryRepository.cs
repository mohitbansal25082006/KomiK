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

    /// <summary>
    /// Optional override for portable installs and testing: when KOMIK_DATA_DIR is set, the library database
    /// and thumbnails live there instead of %USERPROFILE%\.komik.
    /// </summary>
    public static string? DataDirectoryOverride
    {
        get
        {
            string? dir = Environment.GetEnvironmentVariable("KOMIK_DATA_DIR");
            return string.IsNullOrWhiteSpace(dir) ? null : dir.Trim();
        }
    }

    public static string ResolveDatabasePath()
    {
        if (DataDirectoryOverride is { } overrideDir)
        {
            Directory.CreateDirectory(overrideDir);
            return Path.Combine(overrideDir, "komik_library.db");
        }

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

                CREATE TABLE IF NOT EXISTS ReadingSessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    comic_id INTEGER NOT NULL,
                    start_time TEXT NOT NULL,
                    end_time TEXT NOT NULL,
                    duration_seconds INTEGER NOT NULL,
                    pages_read INTEGER NOT NULL,
                    session_date TEXT NOT NULL,
                    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS IgnoredDuplicates (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    comic_id_1 INTEGER NOT NULL,
                    comic_id_2 INTEGER NOT NULL,
                    date_ignored TEXT NOT NULL,
                    UNIQUE(comic_id_1, comic_id_2)
                );

                CREATE TABLE IF NOT EXISTS ManualSeries (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT UNIQUE NOT NULL COLLATE NOCASE,
                    date_created TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS ManualSeriesComics (
                    series_id INTEGER NOT NULL,
                    comic_id INTEGER NOT NULL,
                    sort_order INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY(series_id, comic_id),
                    FOREIGN KEY(series_id) REFERENCES ManualSeries(id) ON DELETE CASCADE,
                    FOREIGN KEY(comic_id) REFERENCES Comics(id) ON DELETE CASCADE
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
                CREATE INDEX IF NOT EXISTS idx_reading_sessions_comic ON ReadingSessions(comic_id);
                CREATE INDEX IF NOT EXISTS idx_reading_sessions_date ON ReadingSessions(session_date);
                CREATE INDEX IF NOT EXISTS idx_manual_series_comic ON ManualSeriesComics(comic_id);

                CREATE TABLE IF NOT EXISTS ManualSeriesExclusions (
                    series_id INTEGER NOT NULL,
                    comic_id INTEGER NOT NULL,
                    PRIMARY KEY(series_id, comic_id),
                    FOREIGN KEY(series_id) REFERENCES ManualSeries(id) ON DELETE CASCADE
                );
            ";


            await cmd.ExecuteNonQueryAsync();

            // v1.1.0: manual series remember their section (story / creator), auto-update and the group they came from.
            var manualColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var info = conn.CreateCommand())
            {
                info.CommandText = "PRAGMA table_info(ManualSeries);";
                using var reader = await info.ExecuteReaderAsync();
                while (await reader.ReadAsync()) manualColumns.Add(reader.GetString(1));
            }
            foreach (var (column, definition) in new[]
            {
                ("kind", "TEXT NOT NULL DEFAULT 'story'"),
                ("auto_update", "INTEGER NOT NULL DEFAULT 0"),
                ("source_key", "TEXT")
            })
            {
                if (manualColumns.Contains(column)) continue;
                using var alter = conn.CreateCommand();
                alter.CommandText = $"ALTER TABLE ManualSeries ADD COLUMN {column} {definition};";
                await alter.ExecuteNonQueryAsync();
            }

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
                thumbnail_path = COALESCE(excluded.thumbnail_path, thumbnail_path),
                last_modified = excluded.last_modified,
                parent_folder = COALESCE(excluded.parent_folder, parent_folder),
                file_size = excluded.file_size,
                is_completed = CASE WHEN excluded.is_completed = 1 THEN 1 ELSE is_completed END,
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

    private string GetCacheDataFilePath()
    {
        string dir = Path.GetDirectoryName(_databaseFilePath) ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".komik");
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
 
    public async Task<IReadOnlyDictionary<long, ComicMetadataEntity>> GetAllComicMetadataAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var result = new Dictionary<long, ComicMetadataEntity>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, comic_id, title, issue_number, series_name, writers, artists, publisher, release_date, summary, last_updated
            FROM ComicMetadata;
        ";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var meta = new ComicMetadataEntity
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
                LastUpdated = DateTime.TryParse(reader.GetString(10), out var updated) ? updated : DateTime.UtcNow
            };
            result[meta.ComicId] = meta;
        }

        return result;
    }

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
        if (dict.TryGetValue(nameof(AppSettings.DefaultReadingPreset), out var preset)) settings.DefaultReadingPreset = preset;
        if (dict.TryGetValue(nameof(AppSettings.WindowWidth), out var ww) && int.TryParse(ww, out int wVal)) settings.WindowWidth = wVal;
        if (dict.TryGetValue(nameof(AppSettings.WindowHeight), out var wh) && int.TryParse(wh, out int hVal)) settings.WindowHeight = hVal;
        if (dict.TryGetValue(nameof(AppSettings.WindowX), out var wx) && int.TryParse(wx, out int xVal)) settings.WindowX = xVal;
        if (dict.TryGetValue(nameof(AppSettings.WindowY), out var wy) && int.TryParse(wy, out int yVal)) settings.WindowY = yVal;
        if (dict.TryGetValue(nameof(AppSettings.IsMaximized), out var max) && bool.TryParse(max, out bool mVal)) settings.IsMaximized = mVal;
        if (dict.TryGetValue(nameof(AppSettings.IsSeriesViewDefault), out var sview) && bool.TryParse(sview, out bool svVal)) settings.IsSeriesViewDefault = svVal;

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
        await SetSettingAsync(nameof(AppSettings.DefaultReadingPreset), settings.DefaultReadingPreset);
        await SetSettingAsync(nameof(AppSettings.WindowWidth), settings.WindowWidth.ToString());
        await SetSettingAsync(nameof(AppSettings.WindowHeight), settings.WindowHeight.ToString());
        await SetSettingAsync(nameof(AppSettings.WindowX), settings.WindowX.ToString());
        await SetSettingAsync(nameof(AppSettings.WindowY), settings.WindowY.ToString());
        await SetSettingAsync(nameof(AppSettings.IsMaximized), settings.IsMaximized.ToString());
        await SetSettingAsync(nameof(AppSettings.IsSeriesViewDefault), settings.IsSeriesViewDefault.ToString());
    }

    #endregion

    #region Reading Sessions & Statistics


    public async Task RecordReadingSessionAsync(long comicId, DateTime startTime, DateTime endTime, int durationSeconds, int pagesRead)
    {
        if (durationSeconds <= 2 && pagesRead <= 0) return;
        await SaveReadingSessionAsync(null, comicId, startTime, endTime, durationSeconds, pagesRead);
    }

    public async Task<long> SaveReadingSessionAsync(long? sessionId, long comicId, DateTime startTimeUtc, DateTime endTimeUtc, int durationSeconds, int pagesRead)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        DateTime startUtc = startTimeUtc.Kind == DateTimeKind.Local ? startTimeUtc.ToUniversalTime() : DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc);
        DateTime endUtc = endTimeUtc.Kind == DateTimeKind.Local ? endTimeUtc.ToUniversalTime() : DateTime.SpecifyKind(endTimeUtc, DateTimeKind.Utc);

        using var cmd = conn.CreateCommand();
        cmd.Parameters.AddWithValue("@comicId", comicId);
        cmd.Parameters.AddWithValue("@startTime", startUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@endTime", endUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@duration", Math.Max(0, durationSeconds));
        cmd.Parameters.AddWithValue("@pages", Math.Max(0, pagesRead));
        // Local calendar day, so streaks and "today" match the reader's clock.
        cmd.Parameters.AddWithValue("@sessionDate", startUtc.ToLocalTime().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));

        if (sessionId.HasValue)
        {
            cmd.CommandText = @"
                UPDATE ReadingSessions
                SET end_time = @endTime, duration_seconds = @duration, pages_read = @pages
                WHERE id = @id;
            ";
            cmd.Parameters.AddWithValue("@id", sessionId.Value);
            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows > 0) return sessionId.Value;
        }

        cmd.CommandText = @"
            INSERT INTO ReadingSessions (comic_id, start_time, end_time, duration_seconds, pages_read, session_date)
            VALUES (@comicId, @startTime, @endTime, @duration, @pages, @sessionDate);
            SELECT last_insert_rowid();
        ";
        var id = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(id ?? 0);
    }

    public async Task<ReadingStatsSummary> GetReadingStatsSummaryAsync()
    {
        await InitializeAsync();

        var sessions = new List<ReadingSessionRecord>();
        using (var conn = CreateConnection())
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT comic_id, start_time, duration_seconds, pages_read FROM ReadingSessions;";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!DateTime.TryParse(reader.GetString(1), System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var start))
                {
                    continue;
                }

                if (start.Kind == DateTimeKind.Unspecified) start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
                if (start.Kind == DateTimeKind.Local) start = start.ToUniversalTime();
                sessions.Add(new ReadingSessionRecord(reader.GetInt64(0), start, reader.GetInt32(2), reader.GetInt32(3)));
            }
        }

        var comics = await GetComicsAsync();
        var metadata = await GetAllComicMetadataAsync();
        return ReadingStatsCalculator.Calculate(sessions, comics, metadata, DateTime.UtcNow);
    }

    #endregion

    #region Duplicate Handling

    public async Task IgnoreDuplicatePairAsync(long comicId1, long comicId2)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        long minId = Math.Min(comicId1, comicId2);
        long maxId = Math.Max(comicId1, comicId2);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO IgnoredDuplicates (comic_id_1, comic_id_2, date_ignored)
            VALUES (@c1, @c2, @dt)
            ON CONFLICT(comic_id_1, comic_id_2) DO NOTHING;
        ";
        cmd.Parameters.AddWithValue("@c1", minId);
        cmd.Parameters.AddWithValue("@c2", maxId);
        cmd.Parameters.AddWithValue("@dt", DateTime.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<HashSet<(long, long)>> GetIgnoredDuplicatePairsAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var set = new HashSet<(long, long)>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT comic_id_1, comic_id_2 FROM IgnoredDuplicates;";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            set.Add((reader.GetInt64(0), reader.GetInt64(1)));
        }
        return set;
    }

    public async Task RemoveIgnoredDuplicatePairAsync(long comicId1, long comicId2)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        long minId = Math.Min(comicId1, comicId2);
        long maxId = Math.Max(comicId1, comicId2);

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM IgnoredDuplicates WHERE comic_id_1 = @c1 AND comic_id_2 = @c2;";
        cmd.Parameters.AddWithValue("@c1", minId);
        cmd.Parameters.AddWithValue("@c2", maxId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteComicAsync(long comicId, bool deleteFileFromDisk)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        string? filePath = null;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT file_path FROM Comics WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", comicId);
            var res = await cmd.ExecuteScalarAsync();
            filePath = res?.ToString();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM Comics WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", comicId);
            await cmd.ExecuteNonQueryAsync();
        }

        if (deleteFileFromDisk && !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            try
            {
                // Recycle Bin rather than a permanent delete, so a mistaken duplicate cleanup can be undone.
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    filePath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibraryRepository] Failed to delete file {filePath}: {ex.Message}");
            }
        }
    }

    #endregion

    #region Full Library Export & Import

    public async Task<string> ExportLibraryBackupJsonAsync()
    {
        await InitializeAsync();
        var backup = new LibraryBackupData();

        var watched = await GetWatchedFoldersAsync();
        backup.WatchedFolders.AddRange(watched.Select(w => w.Path));

        var allComics = await GetComicsAsync();
        foreach (var c in allComics)

        {
            var rec = new BackupComicRecord
            {
                FilePath = c.FilePath,
                Title = c.Title,
                LastReadPage = c.LastReadPage,
                IsCompleted = c.IsCompleted,
                IsFavorite = c.IsFavorite,
                LastReadAt = c.LastReadAt?.ToString("o")
            };

            var tags = await GetTagsForComicAsync(c.Id);
            rec.Tags.AddRange(tags);

            var meta = await GetMetadataForComicAsync(c.Id);
            rec.Metadata = meta;

            backup.Comics.Add(rec);

            var bms = await GetBookmarksForComicAsync(c.Id);
            foreach (var b in bms)
            {
                backup.Bookmarks.Add(new BackupBookmarkRecord
                {
                    ComicFilePath = c.FilePath,
                    PageNumber = b.PageNumber,
                    UserNote = b.UserNote,
                    CreatedAt = b.CreatedAt.ToString("o")
                });
            }
        }

        var settings = await GetAppSettingsAsync();
        backup.Settings[nameof(AppSettings.Theme)] = settings.Theme;
        backup.Settings[nameof(AppSettings.DefaultFitMode)] = settings.DefaultFitMode;
        backup.Settings[nameof(AppSettings.DefaultReadingDirection)] = settings.DefaultReadingDirection;
        backup.Settings[nameof(AppSettings.DefaultViewMode)] = settings.DefaultViewMode;
        backup.Settings[nameof(AppSettings.DefaultReadingPreset)] = settings.DefaultReadingPreset;
        backup.Settings[nameof(AppSettings.DefaultNightMode)] = settings.DefaultNightMode.ToString();

        return System.Text.Json.JsonSerializer.Serialize(backup, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<(int comicsRestored, int bookmarksRestored, int tagsRestored)> ImportLibraryBackupJsonAsync(string jsonContent, bool overwriteExisting)
    {
        if (string.IsNullOrWhiteSpace(jsonContent)) return (0, 0, 0);

        var backup = System.Text.Json.JsonSerializer.Deserialize<LibraryBackupData>(jsonContent);
        if (backup == null) return (0, 0, 0);

        await InitializeAsync();
        int comicsRestored = 0;
        int bookmarksRestored = 0;
        int tagsRestored = 0;

        foreach (var cRec in backup.Comics)
        {
            var existing = await GetComicByPathAsync(cRec.FilePath);
            if (existing == null)
            {
                string fileName = Path.GetFileName(cRec.FilePath);
                string title = Path.GetFileNameWithoutExtension(fileName);
                using var conn = CreateConnection();
                await conn.OpenAsync();
                using var fCmd = conn.CreateCommand();
                fCmd.CommandText = "SELECT id, file_path, title, page_count, last_read_page, is_favorite FROM Comics WHERE file_path LIKE @tail OR title = @title LIMIT 1;";
                fCmd.Parameters.AddWithValue("@tail", "%" + fileName);
                fCmd.Parameters.AddWithValue("@title", title);
                using var fReader = await fCmd.ExecuteReaderAsync();
                if (await fReader.ReadAsync())
                {
                    existing = new ComicEntity
                    {
                        Id = fReader.GetInt64(0),
                        FilePath = fReader.GetString(1),
                        Title = fReader.GetString(2),
                        PageCount = fReader.GetInt32(3),
                        LastReadPage = fReader.GetInt32(4),
                        IsFavorite = fReader.GetInt32(5) == 1
                    };
                }
            }

            if (existing != null)
            {
                if (overwriteExisting || existing.LastReadPage == 0 || cRec.LastReadPage > existing.LastReadPage || cRec.IsFavorite)
                {
                    await UpdateReadingProgressAsync(existing.FilePath, cRec.LastReadPage, existing.PageCount);
                    await SetFavoriteAsync(existing.Id, cRec.IsFavorite);
                    comicsRestored++;
                }

                if (cRec.Metadata != null)
                {
                    cRec.Metadata.ComicId = existing.Id;
                    await SaveComicMetadataAsync(cRec.Metadata);
                }

                foreach (var tag in cRec.Tags)
                {
                    await AddTagToComicAsync(existing.Id, tag);
                    tagsRestored++;
                }
            }
        }

        foreach (var bRec in backup.Bookmarks)
        {
            var existing = await GetComicByPathAsync(bRec.ComicFilePath);
            if (existing == null)
            {
                string fileName = Path.GetFileName(bRec.ComicFilePath);
                using var conn = CreateConnection();
                await conn.OpenAsync();
                using var fCmd = conn.CreateCommand();
                fCmd.CommandText = "SELECT id, file_path FROM Comics WHERE file_path LIKE @tail LIMIT 1;";
                fCmd.Parameters.AddWithValue("@tail", "%" + fileName);
                using var fReader = await fCmd.ExecuteReaderAsync();
                if (await fReader.ReadAsync())
                {
                    existing = new ComicEntity { Id = fReader.GetInt64(0), FilePath = fReader.GetString(1) };
                }
            }

            if (existing != null)
            {
                await AddBookmarkAsync(existing.Id, bRec.PageNumber, bRec.UserNote);
                bookmarksRestored++;
            }
        }

        if (backup.Settings != null && backup.Settings.Count > 0)
        {
            var currentSettings = await GetAppSettingsAsync();
            if (backup.Settings.TryGetValue(nameof(AppSettings.Theme), out var t)) currentSettings.Theme = t;
            if (backup.Settings.TryGetValue(nameof(AppSettings.DefaultFitMode), out var fm)) currentSettings.DefaultFitMode = fm;
            if (backup.Settings.TryGetValue(nameof(AppSettings.DefaultReadingDirection), out var rd)) currentSettings.DefaultReadingDirection = rd;
            if (backup.Settings.TryGetValue(nameof(AppSettings.DefaultReadingPreset), out var rp)) currentSettings.DefaultReadingPreset = rp;
            await SaveAppSettingsAsync(currentSettings);
        }

        return (comicsRestored, bookmarksRestored, tagsRestored);
    }

    #endregion

    #region Manual Series

    public async Task<List<ComicSeriesGroup>> GetManualSeriesAsync()
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        var seriesList = new List<ComicSeriesGroup>();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, name, kind, auto_update, source_key FROM ManualSeries ORDER BY name ASC;";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                seriesList.Add(new ComicSeriesGroup
                {
                    ManualSeriesId = reader.GetInt64(0),
                    SeriesName = reader.GetString(1),
                    Section = string.Equals(reader.IsDBNull(2) ? null : reader.GetString(2), "creator", StringComparison.OrdinalIgnoreCase) ? SeriesSection.Creator : SeriesSection.Story,
                    IsAutoUpdate = !reader.IsDBNull(3) && reader.GetInt64(3) != 0,
                    SourceKey = reader.IsDBNull(4) ? null : reader.GetString(4),
                    IsManual = true
                });
            }
        }

        foreach (var s in seriesList)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT c.id, c.file_path, c.title, c.format, c.page_count, c.thumbnail_path,
                       c.date_added, c.last_modified, c.parent_folder, c.file_size,
                       c.is_favorite, c.is_missing, c.last_read_page, c.last_read_at, c.is_completed
                FROM ManualSeriesComics m
                JOIN Comics c ON m.comic_id = c.id
                WHERE m.series_id = @sid AND c.is_missing = 0
                ORDER BY m.sort_order ASC, c.title ASC;
            ";
            cmd.Parameters.AddWithValue("@sid", s.ManualSeriesId);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                s.Issues.Add(MapComicEntity(reader));
            }

            reader.Close();
            using (var ex = conn.CreateCommand())
            {
                ex.CommandText = "SELECT comic_id FROM ManualSeriesExclusions WHERE series_id = @sid;";
                ex.Parameters.AddWithValue("@sid", s.ManualSeriesId);
                using var exReader = await ex.ExecuteReaderAsync();
                while (await exReader.ReadAsync()) s.ExcludedComicIds.Add(exReader.GetInt64(0));
            }

            if (s.Issues.Count > 0)
            {
                s.CoverThumbnailPath = s.Issues.Select(i => i.ThumbnailPath).FirstOrDefault(t => !string.IsNullOrEmpty(t));
            }
            s.RefreshProperties();
        }

        return seriesList;
    }

    public Task<long> CreateManualSeriesAsync(string name, IEnumerable<long> comicIds) =>
        CreateManualSeriesAsync(name, comicIds, SeriesSection.Story, autoUpdate: false, sourceKey: null);

    public async Task<long> CreateManualSeriesAsync(string name, IEnumerable<long> comicIds, SeriesSection section, bool autoUpdate, string? sourceKey)
    {
        string trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed)) throw new ArgumentException("Series name cannot be empty", nameof(name));

        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        // Names are unique (case-insensitive): pick "Name (2)", "Name (3)", ... instead of failing.
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var check = conn.CreateCommand())
        {
            check.CommandText = "SELECT name FROM ManualSeries;";
            using var reader = await check.ExecuteReaderAsync();
            while (await reader.ReadAsync()) taken.Add(reader.GetString(0));
        }

        string baseName = trimmed;
        for (int n = 2; taken.Contains(trimmed); n++)
        {
            trimmed = $"{baseName} ({n})";
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO ManualSeries (name, date_created, kind, auto_update, source_key)
            VALUES (@name, @created, @kind, @auto, @source);
            SELECT last_insert_rowid();
        ";
        cmd.Parameters.AddWithValue("@name", trimmed);
        cmd.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@kind", section == SeriesSection.Creator ? "creator" : "story");
        cmd.Parameters.AddWithValue("@auto", autoUpdate ? 1 : 0);
        cmd.Parameters.AddWithValue("@source", (object?)sourceKey ?? DBNull.Value);

        long seriesId = Convert.ToInt64(await cmd.ExecuteScalarAsync());

        await AddComicsToManualSeriesAsync(seriesId, comicIds);
        return seriesId;
    }

    public async Task AddComicsToManualSeriesAsync(long seriesId, IEnumerable<long> comicIds)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        int order = 0;
        using (var max = conn.CreateCommand())
        {
            max.CommandText = "SELECT COALESCE(MAX(sort_order), -1) + 1 FROM ManualSeriesComics WHERE series_id = @sid;";
            max.Parameters.AddWithValue("@sid", seriesId);
            order = Convert.ToInt32(await max.ExecuteScalarAsync());
        }

        using var trans = conn.BeginTransaction();
        try
        {
            foreach (var cid in comicIds)
            {
                using var cmd = conn.CreateCommand();
                cmd.Transaction = trans;
                cmd.CommandText = @"
                    DELETE FROM ManualSeriesExclusions WHERE series_id = @sid AND comic_id = @cid;
                    INSERT OR IGNORE INTO ManualSeriesComics (series_id, comic_id, sort_order)
                    VALUES (@sid, @cid, @order);
                ";
                cmd.Parameters.AddWithValue("@sid", seriesId);
                cmd.Parameters.AddWithValue("@cid", cid);
                cmd.Parameters.AddWithValue("@order", order++);
                await cmd.ExecuteNonQueryAsync();
            }
            await trans.CommitAsync();
        }
        catch
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task RemoveComicFromManualSeriesAsync(long seriesId, long comicId)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        // Remember the removal so auto-update never adds the comic back.
        cmd.CommandText = @"
            DELETE FROM ManualSeriesComics WHERE series_id = @sid AND comic_id = @cid;
            INSERT OR IGNORE INTO ManualSeriesExclusions (series_id, comic_id) VALUES (@sid, @cid);
        ";
        cmd.Parameters.AddWithValue("@sid", seriesId);
        cmd.Parameters.AddWithValue("@cid", comicId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateManualSeriesOptionsAsync(long seriesId, bool autoUpdate, SeriesSection section)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE ManualSeries SET auto_update = @auto, kind = @kind WHERE id = @sid;";
        cmd.Parameters.AddWithValue("@sid", seriesId);
        cmd.Parameters.AddWithValue("@auto", autoUpdate ? 1 : 0);
        cmd.Parameters.AddWithValue("@kind", section == SeriesSection.Creator ? "creator" : "story");
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SetManualSeriesOrderAsync(long seriesId, IReadOnlyList<long> orderedComicIds)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var trans = conn.BeginTransaction();
        for (int i = 0; i < orderedComicIds.Count; i++)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = "UPDATE ManualSeriesComics SET sort_order = @order WHERE series_id = @sid AND comic_id = @cid;";
            cmd.Parameters.AddWithValue("@order", i);
            cmd.Parameters.AddWithValue("@sid", seriesId);
            cmd.Parameters.AddWithValue("@cid", orderedComicIds[i]);
            await cmd.ExecuteNonQueryAsync();
        }
        await trans.CommitAsync();
    }

    public async Task DeleteManualSeriesAsync(long seriesId)
    {
        await InitializeAsync();
        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM ManualSeries WHERE id = @sid;";
        cmd.Parameters.AddWithValue("@sid", seriesId);
        await cmd.ExecuteNonQueryAsync();
    }

    #endregion

    public void Dispose()
    {
        _initLock.Dispose();
    }
}
