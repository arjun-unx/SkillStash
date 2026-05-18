namespace PromptStash.Api.Common.DTOs;

public sealed record ToggleBookmarkResponse(bool Bookmarked, int BookmarkCount);

public sealed record BookmarkCollectionDto(Guid Id, string Name, int ItemCount);

public sealed record SkillCommentDto(
    Guid Id,
    string Body,
    string AuthorDisplayName,
    string AuthorUserName,
    DateTime CreatedAtUtc);

public sealed record BookmarkBodyDto(Guid? CollectionId);

public sealed record MoveBookmarkToCollectionDto(Guid? CollectionId);

public sealed record AddSkillCommentRequest(string Body);
