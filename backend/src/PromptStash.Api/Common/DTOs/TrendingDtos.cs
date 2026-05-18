namespace PromptStash.Api.Common.DTOs;

public sealed record TrendingProviderDto(
    string Slug,
    string Name,
    string ShortLabel,
    string Description,
    string Icon,
    int SkillCount,
    int TrendingScore,
    DateTime? LastSyncedAtUtc);

public sealed record TrendingSkillDto(
    Guid Id,
    string ProviderSlug,
    string ProviderName,
    string SourceName,
    string SourceUrl,
    string Title,
    string Body,
    string Snippet,
    string RoleCategory,
    string? Category,
    IReadOnlyList<string> Tags,
    int TrendingScore,
    int UseCount,
    int SaveCount,
    double Rating,
    DateTime? SourceUpdatedAtUtc,
    DateTime SyncedAtUtc,
    bool BookmarkedByCurrentUser);

public sealed record ToggleTrendingBookmarkResponse(bool Bookmarked, int SaveCount);

public sealed record TrackTrendingUseResponse(int UseCount);
