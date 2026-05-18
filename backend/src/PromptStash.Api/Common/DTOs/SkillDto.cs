using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Common.DTOs;

public sealed record SkillDto(
    Guid Id,
    string Title,
    string Body,
    string? Description,
    string AgentSlug,
    SkillVisibility Visibility,
    IReadOnlyList<string> Tags,
    int CopyCount,
    int LikeCount,
    int BookmarkCount,
    int CommentCount,
    bool LikedByCurrentUser,
    bool BookmarkedByCurrentUser,
    Guid AuthorId,
    string AuthorDisplayName,
    string AuthorUserName,
    string? AuthorHeadline,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
