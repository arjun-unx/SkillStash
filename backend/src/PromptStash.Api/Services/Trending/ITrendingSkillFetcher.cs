using PromptStash.Api.Common.Settings;

namespace PromptStash.Api.Services.Trending;

public interface ITrendingSkillFetcher
{
    string Format { get; }
    Task<TrendingFetchResult> FetchAsync(TrendingSourceOptions source, CancellationToken ct);
}
