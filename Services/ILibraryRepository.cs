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
    Task SaveComicMetadataAsync(ComicMetadataEntity metadata);
    Task DeleteComicMetadataAsync(long comicId);
    Task UpdateComicTitleAsync(long comicId, string title);

    // App Settings
    Task<string?> GetSettingAsync(string key);
    Task SetSettingAsync(string key, string value);
    Task<AppSettings> GetAppSettingsAsync();
    Task SaveAppSettingsAsync(AppSettings settings);
}
