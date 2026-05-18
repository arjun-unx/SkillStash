namespace PromptStash.Api.Services.Trending;

public sealed record TrendingFetchedSkill(
    string ExternalKey,
    string ProviderSlug,
    string SourceName,
    string SourceUrl,
    string Title,
    string Body,
    string RoleCategory,
    string? Category,
    IReadOnlyList<string> Tags,
    int TrendingScore,
    DateTime? SourceUpdatedAtUtc);
