using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Services.Trending;

public sealed class TrendingSkillRepository(AppDbContext db, IMemoryCache cache) : ITrendingSkillRepository
{
    public async Task<IReadOnlyList<TrendingProviderDto>> GetProvidersAsync(CancellationToken ct)
    {
        if (cache.TryGetValue("trending:providers", out IReadOnlyList<TrendingProviderDto>? cached) && cached is not null)
            return cached;

        var stats = await db.TrendingSkills
            .AsNoTracking()
            .GroupBy(p => p.ProviderSlug)
            .Select(g => new
            {
                Slug = g.Key,
                Count = g.Count(),
                MaxScore = g.Max(p => p.TrendingScore),
                LastSync = g.Max(p => p.SyncedAtUtc)
            })
            .ToListAsync(ct);

        var bySlug = stats.ToDictionary(s => s.Slug, StringComparer.OrdinalIgnoreCase);
        var list = TrendingProviderCatalog.All.Select(meta =>
        {
            bySlug.TryGetValue(meta.Slug, out var s);
            return new TrendingProviderDto(
                meta.Slug,
                meta.Name,
                meta.ShortLabel,
                meta.Description,
                meta.Icon,
                s?.Count ?? 0,
                s?.MaxScore ?? 0,
                s?.LastSync);
        }).ToList();

        cache.Set("trending:providers", list, TimeSpan.FromMinutes(5));
        return list;
    }

    public async Task<PaginatedList<TrendingSkillDto>> SearchAsync(
        string? provider,
        string? role,
        string? category,
        string? search,
        string sort,
        int page,
        int pageSize,
        Guid? userId,
        CancellationToken ct)
    {
        var q = db.TrendingSkills.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(provider))
            q = q.Where(p => p.ProviderSlug == provider.ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim().ToLower();
            q = q.Where(p => EF.Functions.ILike(p.RoleCategory, $"%{r}%"));
        }

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p =>
                EF.Functions.ILike(p.Title, $"%{s}%") ||
                EF.Functions.ILike(p.Body, $"%{s}%") ||
                EF.Functions.ILike(p.RoleCategory, $"%{s}%"));
        }

        q = sort.ToLowerInvariant() switch
        {
            "bookmarked" or "saved" => q.OrderByDescending(p => p.SaveCount).ThenByDescending(p => p.TrendingScore),
            "used" => q.OrderByDescending(p => p.UseCount).ThenByDescending(p => p.TrendingScore),
            "latest" => q.OrderByDescending(p => p.SyncedAtUtc),
            "rated" => q.OrderByDescending(p => p.Rating).ThenByDescending(p => p.TrendingScore),
            _ => q.OrderByDescending(p => p.TrendingScore).ThenByDescending(p => p.SaveCount)
        };

        var projected = q.Select(p => new TrendingSkillDto(
            p.Id,
            p.ProviderSlug,
            p.ProviderSlug,
            p.SourceName,
            p.SourceUrl,
            p.Title,
            p.Body,
            p.Snippet ?? "",
            p.RoleCategory,
            p.Category,
            p.Tags,
            p.TrendingScore,
            p.UseCount,
            p.SaveCount,
            p.Rating,
            p.SourceUpdatedAtUtc,
            p.SyncedAtUtc,
            userId != null && p.Bookmarks.Any(b => b.UserId == userId)));

        return await PaginatedList<TrendingSkillDto>.CreateAsync(projected, page, pageSize, ct);
    }

    public async Task<TrendingSkillDto?> GetByIdAsync(Guid id, Guid? userId, CancellationToken ct)
    {
        return await db.TrendingSkills.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new TrendingSkillDto(
                p.Id,
                p.ProviderSlug,
                p.ProviderSlug,
                p.SourceName,
                p.SourceUrl,
                p.Title,
                p.Body,
                p.Snippet ?? "",
                p.RoleCategory,
                p.Category,
                p.Tags,
                p.TrendingScore,
                p.UseCount,
                p.SaveCount,
                p.Rating,
                p.SourceUpdatedAtUtc,
                p.SyncedAtUtc,
                userId != null && p.Bookmarks.Any(b => b.UserId == userId)))
            .FirstOrDefaultAsync(ct);
    }

    public Task<TrendingSkill?> GetEntityAsync(Guid id, CancellationToken ct) =>
        db.TrendingSkills.FirstOrDefaultAsync(p => p.Id == id, ct);
}
