namespace PromptStash.Api.Services.Trending;

public enum TrendingFetchStatus
{
    Success,
    Empty,
    RateLimited,
    Failed
}

public sealed record TrendingFetchResult(
    IReadOnlyList<TrendingFetchedSkill> Skills,
    TrendingFetchStatus Status);
