namespace PromptStash.Api.Common.DTOs;

public sealed record ToggleBookmarkResponse(bool Bookmarked, int BookmarkCount);

public sealed record BookmarkCollectionDto(Guid Id, string Name, int ItemCount);

public sealed record BookmarkBodyDto(Guid? CollectionId, bool? Bookmarked);

public sealed record TrendingBookmarkBodyDto(bool? Bookmarked);

public sealed record MoveBookmarkToCollectionDto(Guid? CollectionId);
