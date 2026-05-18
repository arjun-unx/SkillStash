namespace PromptStash.Api.Common.DTOs;

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string UserName,
    string DisplayName,
    string AccessToken,
    DateTime ExpiresAtUtc);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string UserName,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    bool EmailNotificationsEnabled);
