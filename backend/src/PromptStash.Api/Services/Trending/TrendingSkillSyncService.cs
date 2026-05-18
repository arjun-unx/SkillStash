using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Common.Settings;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Services.Trending;

public interface ITrendingSkillSyncService
{
    Task<int> SyncAsync(bool failIfGitHubNotConfigured = false, CancellationToken ct = default);
    DateTime? LastSyncUtc { get; }
}

public sealed class TrendingSkillSyncService(
    AppDbContext db,
    IEnumerable<ITrendingSkillFetcher> fetchers,
    IOptions<TrendingOptions> options,
    IMemoryCache cache,
    ILogger<TrendingSkillSyncService> logger) : ITrendingSkillSyncService
{
    private const string LastSyncCacheKey = "trending:last-sync";

    public DateTime? LastSyncUtc => cache.Get<DateTime?>(LastSyncCacheKey);

    public async Task<int> SyncAsync(bool failIfGitHubNotConfigured = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.GitHubToken))
        {
            const string message =
                "GitHub trending sync requires Trending:GitHubToken. " +
                "Copy appsettings.Secrets.json.example to appsettings.Secrets.json and set your PAT, " +
                "or run: dotnet user-secrets set \"Trending:GitHubToken\" \"ghp_...\" --project src/PromptStash.Api";

            if (failIfGitHubNotConfigured)
                throw new TrendingSyncException(message);

            logger.LogWarning("{Message} Skipping trending sync.", message);
            return 0;
        }

        var sources = options.Value.Sources;
        if (sources.Count == 0)
            sources = TrendingSourceDefaults.Get();

        var fetcherMap = fetchers.ToDictionary(f => f.Format, StringComparer.OrdinalIgnoreCase);
        var aggregated = new Dictionary<string, TrendingFetchedSkill>(StringComparer.Ordinal);
        var rateLimited = false;

        foreach (var source in sources)
        {
            if (rateLimited)
                break;

            if (!fetcherMap.TryGetValue(source.Format, out var fetcher))
            {
                logger.LogWarning("No fetcher for format {Format} ({Name})", source.Format, source.Name);
                continue;
            }

            try
            {
                var result = await fetcher.FetchAsync(source, ct);
                foreach (var s in result.Skills)
                    aggregated[s.ExternalKey] = s;
                logger.LogInformation("Fetched {Count} skills from {Source}", result.Skills.Count, source.Name);

                if (result.Status == TrendingFetchStatus.RateLimited)
                    rateLimited = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch trending skills from {Source}", source.Name);
            }
        }

        if (aggregated.Count == 0)
        {
            if (rateLimited)
            {
                logger.LogWarning(
                    "Trending sync produced zero skills because GitHub rate limit was exceeded. " +
                    "Wait for the limit to reset or verify Trending:GitHubToken is valid.");
            }
            else
                logger.LogWarning("Trending sync produced zero skills");

            return 0;
        }

        var now = DateTime.UtcNow;
        var externalKeys = aggregated.Keys.ToList();
        var existing = await db.TrendingSkills
            .Where(p => externalKeys.Contains(p.ExternalKey))
            .ToDictionaryAsync(p => p.ExternalKey, ct);

        var upserted = 0;
        foreach (var fp in aggregated.Values)
        {
            if (existing.TryGetValue(fp.ExternalKey, out var row))
            {
                row.Title = fp.Title;
                row.Body = fp.Body;
                row.Snippet = TrendingRoleClassifier.Snippet(fp.Body);
                row.RoleCategory = fp.RoleCategory;
                row.Category = fp.Category;
                row.Tags = fp.Tags.ToList();
                row.TrendingScore = fp.TrendingScore;
                row.SourceUrl = fp.SourceUrl;
                row.SourceName = fp.SourceName;
                row.ProviderSlug = fp.ProviderSlug;
                row.SourceUpdatedAtUtc = fp.SourceUpdatedAtUtc;
                row.SyncedAtUtc = now;
            }
            else
            {
                db.TrendingSkills.Add(new TrendingSkill
                {
                    ExternalKey = fp.ExternalKey,
                    ProviderSlug = fp.ProviderSlug,
                    SourceName = fp.SourceName,
                    SourceUrl = fp.SourceUrl,
                    Title = fp.Title,
                    Body = fp.Body,
                    Snippet = TrendingRoleClassifier.Snippet(fp.Body),
                    RoleCategory = fp.RoleCategory,
                    Category = fp.Category,
                    Tags = fp.Tags.ToList(),
                    TrendingScore = fp.TrendingScore,
                    UseCount = 0,
                    SaveCount = 0,
                    Rating = 4.2,
                    SourceUpdatedAtUtc = fp.SourceUpdatedAtUtc,
                    SyncedAtUtc = now
                });
            }

            upserted++;
        }

        await db.SaveChangesAsync(ct);
        cache.Set(LastSyncCacheKey, now, TimeSpan.FromHours(24));
        cache.Remove("trending:providers");
        logger.LogInformation("Trending sync completed: {Count} skills", upserted);
        return upserted;
    }
}
