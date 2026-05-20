namespace PromptStash.Api.Common.DTOs;

public sealed record UserProfileDto(
    Guid Id,
    string UserName,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    int FollowersCount,
    int FollowingCount,
    int PublicSkillsCount,
    bool IsFollowedByCurrentUser);

public sealed record ToggleFollowResponse(bool IsFollowing, int FollowersCount);
