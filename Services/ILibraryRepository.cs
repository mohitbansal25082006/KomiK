using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Komik.Models;

namespace Komik.Services;

/// <summary>
/// Repository interface for all comic library database CRUD operations.
/// </summary>
public interface ILibraryRepository : IDisposable
{
    Task InitializeAsync();

    // Watched root folders
    Task<IReadOnlyList<WatchedFolder>> GetWatchedFoldersAsync();
    Task<WatchedFolder> AddWatchedFolderAsync(string path);
    Task RemoveWatchedFolderAsync(long id);
    Task UpdateWatchedFolderLastScannedAsync(long id, DateTime lastScanned);

    // Comics
    Task<IReadOnlyList<ComicEntity>> GetComicsAsync(LibraryFilter? filter = null, LibrarySortOption sort = LibrarySortOption.TitleAscending);
    Task<ComicEntity?> GetComicByIdAsync(long id);
    Task<ComicEntity?> GetComicByPathAsync(string path);
    Task<long> InsertComicAsync(ComicEntity comic);
    Task UpdateComicAsync(ComicEntity comic);
    Task SetFavoriteAsync(long comicId, bool isFavorite);
    Task SetMissingStatusAsync(long comicId, bool isMissing);
    Task RemoveComicAsync(long comicId);
    Task UpdateThumbnailPathAsync(long comicId, string? thumbnailPath);
    Task ClearAllComicThumbnailsAsync();

    // Reading Progress & Status
    Task UpdateReadingProgressAsync(string filePath, int pageIndex, int totalPages);
    Task SetCompletedStatusAsync(long comicId, bool isCompleted);

    // Bookmarks
    Task<IReadOnlyList<BookmarkEntity>> GetBookmarksForComicAsync(long comicId);
    Task<BookmarkEntity> AddBookmarkAsync(long comicId, int pageIndex, string? userNote = null);
    Task RemoveBookmarkAsync(long bookmarkId);
    Task<bool> IsPageBookmarkedAsync(long comicId, int pageIndex);

    // Tags
    Task<IReadOnlyList<string>> GetAllTagsAsync();
    Task<IReadOnlyList<string>> GetTagsForComicAsync(long comicId);
    Task AddTagToComicAsync(long comicId, string tagName);
    Task RemoveTagFromComicAsync(long comicId, string tagName);
    Task CreateTagAsync(string tagName);
    Task DeleteTagAsync(string tagName);
    Task<int> GetComicCountForTagAsync(string tagName);
    Task<IReadOnlyDictionary<string, int>> GetTagUsageAsync();
    Task RenameTagAsync(string oldName, string newName);

    // Folder Collections
    Task<IReadOnlyList<string>> GetCollectionFoldersAsync();
    Task<int> GetComicCountInFolderAsync(string folderName);
    Task MoveComicToCollectionFolderAsync(long comicId, string targetFolderName);
    Task RemoveComicsInWatchedFolderAsync(string folderPath);

    // Collections (compatibility)
    Task<IReadOnlyList<string>> GetAllCollectionsAsync();
    Task<IReadOnlyList<string>> GetCollectionsForComicAsync(long comicId);
    Task AddToCollectionAsync(long comicId, string collectionName);
    Task RemoveFromCollectionAsync(long comicId, string collectionName);
    Task CreateCollectionAsync(string collectionName);
    Task DeleteCollectionAsync(string collectionName);
    Task<int> GetComicCountForCollectionAsync(string collectionName);

    // Counts
    Task<int> GetTotalComicCountAsync();
    Task<int> GetFavoritesCountAsync();
    Task<int> GetInProgressCountAsync();
    Task<int> GetUnreadCountAsync();
    Task<int> GetCompletedCountAsync();

    // Virtual In-App Folders
    Task<IReadOnlyList<ApplicationFolder>> GetApplicationFoldersAsync();
    Task CreateFolderAsync(string name);
    Task RenameFolderAsync(string oldName, string newName);
    Task DeleteFolderAsync(string name);
    Task AddComicToFolderAsync(long comicId, string folderName);
    Task RemoveComicFromFolderAsync(long comicId, string folderName);
    Task BackupLibraryDataCacheAsync();
    Task RestoreLibraryDataCacheAsync();

    // Extended Comic Metadata
    Task<ComicMetadataEntity?> GetMetadataForComicAsync(long comicId);
    Task<IReadOnlyDictionary<long, ComicMetadataEntity>> GetAllComicMetadataAsync();
    Task SaveComicMetadataAsync(ComicMetadataEntity metadata);
    Task DeleteComicMetadataAsync(long comicId);
    Task UpdateComicTitleAsync(long comicId, string title);

    // App Settings
    Task<string?> GetSettingAsync(string key);
    Task SetSettingAsync(string key, string value);
    Task<AppSettings> GetAppSettingsAsync();
    Task SaveAppSettingsAsync(AppSettings settings);

    // Reading Sessions & Statistics
    Task RecordReadingSessionAsync(long comicId, DateTime startTime, DateTime endTime, int durationSeconds, int pagesRead);
    Task<long> SaveReadingSessionAsync(long? sessionId, long comicId, DateTime startTimeUtc, DateTime endTimeUtc, int durationSeconds, int pagesRead);
    Task<ReadingStatsSummary> GetReadingStatsSummaryAsync();

    // Duplicate Handling
    Task IgnoreDuplicatePairAsync(long comicId1, long comicId2);
    Task<HashSet<(long, long)>> GetIgnoredDuplicatePairsAsync();
    Task RemoveIgnoredDuplicatePairAsync(long comicId1, long comicId2);
    Task DeleteComicAsync(long comicId, bool deleteFileFromDisk);

    // Comics the user removed from the library (rescans skip them until they are added back)
    Task<HashSet<string>> GetRemovedComicPathsAsync();
    Task ForgetRemovedComicAsync(string filePath);

    // Full Library Export & Import
    Task<string> ExportLibraryBackupJsonAsync();
    Task<(int comicsRestored, int bookmarksRestored, int tagsRestored)> ImportLibraryBackupJsonAsync(string jsonContent, bool overwriteExisting);

    // Manual Series
    Task<List<ComicSeriesGroup>> GetManualSeriesAsync();
    Task<long> CreateManualSeriesAsync(string name, IEnumerable<long> comicIds);
    Task<long> CreateManualSeriesAsync(string name, IEnumerable<long> comicIds, SeriesSection section, bool autoUpdate, string? sourceKey);
    Task UpdateManualSeriesOptionsAsync(long seriesId, bool autoUpdate, SeriesSection section);
    Task SetManualSeriesOrderAsync(long seriesId, IReadOnlyList<long> orderedComicIds);
    Task AddComicsToManualSeriesAsync(long seriesId, IEnumerable<long> comicIds);
    Task RemoveComicFromManualSeriesAsync(long seriesId, long comicId);
    Task<string> RenameManualSeriesAsync(long seriesId, string name);
    Task DeleteManualSeriesAsync(long seriesId);
}


