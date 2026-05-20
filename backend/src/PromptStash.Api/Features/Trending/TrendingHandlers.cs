using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Features.Trending;

public sealed record GetTrendingProvidersRequest : IRequest<IReadOnlyList<TrendingProviderDto>>;

public sealed class GetTrendingProvidersHandler(ITrendingSkillRepository repo)
    : IRequestHandler<GetTrendingProvidersRequest, IReadOnlyList<TrendingProviderDto>>
{
    public async Task<IReadOnlyList<TrendingProviderDto>> Handle(GetTrendingProvidersRequest request, CancellationToken ct)
    {
        var list = await repo.GetProvidersAsync(ct);
        return list.Select(p =>
        {
            var meta = TrendingProviderCatalog.Find(p.Slug);
            return meta is null
                ? p
                : p with { Name = meta.Name, ShortLabel = meta.ShortLabel, Description = meta.Description, Icon = meta.Icon };
        }).ToList();
    }
}

public sealed record GetTrendingSkillByIdRequest(Guid Id) : IRequest<TrendingSkillDto>;

public sealed class GetTrendingSkillByIdHandler(
    ITrendingSkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<GetTrendingSkillByIdRequest, TrendingSkillDto>
{
    public async Task<TrendingSkillDto> Handle(GetTrendingSkillByIdRequest request, CancellationToken ct)
    {
        var dto = await repo.GetByIdAsync(request.Id, currentUser.UserId, ct)
                  ?? throw new NotFoundException("TrendingSkill", request.Id);
        var meta = TrendingProviderCatalog.Find(dto.ProviderSlug);
        return meta is null ? dto : dto with { ProviderName = meta.Name };
    }
}

public sealed record SearchTrendingSkillsRequest(
    string? Provider,
    string? Role,
    string? Category,
    string? Search,
    string Sort,
    int Page,
    int PageSize) : IRequest<PaginatedList<TrendingSkillDto>>;

public sealed class SearchTrendingSkillsHandler(
    ITrendingSkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<SearchTrendingSkillsRequest, PaginatedList<TrendingSkillDto>>
{
    public async Task<PaginatedList<TrendingSkillDto>> Handle(SearchTrendingSkillsRequest request, CancellationToken ct)
    {
        var page = await repo.SearchAsync(
            request.Provider,
            request.Role,
            request.Category,
            request.Search,
            request.Sort,
            request.Page,
            request.PageSize,
            currentUser.UserId,
            ct);

        var items = page.Items.Select(Enrich).ToList();
        return new PaginatedList<TrendingSkillDto>(items, page.PageNumber, page.PageSize, page.TotalCount);
    }

    private static TrendingSkillDto Enrich(TrendingSkillDto p)
    {
        var meta = TrendingProviderCatalog.Find(p.ProviderSlug);
        return meta is null ? p : p with { ProviderName = meta.Name };
    }
}

public sealed record SyncTrendingSkillsRequest : IRequest<int>;

public sealed class SyncTrendingSkillsHandler(ITrendingSkillSyncService sync)
    : IRequestHandler<SyncTrendingSkillsRequest, int>
{
    public Task<int> Handle(SyncTrendingSkillsRequest request, CancellationToken ct) =>
        sync.SyncAsync(failIfGitHubNotConfigured: true, ct: ct);
}

public sealed record ToggleTrendingBookmarkRequest(Guid TrendingSkillId, bool? Bookmarked = null)
    : IRequest<ToggleTrendingBookmarkResponse>;

public sealed class ToggleTrendingBookmarkHandler(
    AppDbContext db,
    ITrendingSkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<ToggleTrendingBookmarkRequest, ToggleTrendingBookmarkResponse>
{
    public async Task<ToggleTrendingBookmarkResponse> Handle(ToggleTrendingBookmarkRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var skill = await repo.GetEntityAsync(request.TrendingSkillId, ct)
                     ?? throw new NotFoundException("TrendingSkill", request.TrendingSkillId);

        var existing = await db.TrendingSkillBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.TrendingSkillId == skill.Id, ct);

        var desired = request.Bookmarked ?? (existing is null);

        if (desired)
        {
            if (existing is not null)
                return new ToggleTrendingBookmarkResponse(true, skill.SaveCount);

            await db.TrendingSkillBookmarks.AddAsync(new TrendingSkillBookmark
            {
                UserId = userId,
                TrendingSkillId = skill.Id,
                CreatedAtUtc = DateTime.UtcNow
            }, ct);
            skill.SaveCount++;
            await db.SaveChangesAsync(ct);
            return new ToggleTrendingBookmarkResponse(true, skill.SaveCount);
        }

        if (existing is null)
            return new ToggleTrendingBookmarkResponse(false, skill.SaveCount);

        db.TrendingSkillBookmarks.Remove(existing);
        skill.SaveCount = Math.Max(0, skill.SaveCount - 1);
        await db.SaveChangesAsync(ct);
        return new ToggleTrendingBookmarkResponse(false, skill.SaveCount);
    }
}

public sealed record TrackTrendingUseRequest(Guid TrendingSkillId) : IRequest<TrackTrendingUseResponse>;

public sealed class TrackTrendingUseHandler(
    AppDbContext db,
    ITrendingSkillRepository repo) : IRequestHandler<TrackTrendingUseRequest, TrackTrendingUseResponse>
{
    public async Task<TrackTrendingUseResponse> Handle(TrackTrendingUseRequest request, CancellationToken ct)
    {
        var skill = await repo.GetEntityAsync(request.TrendingSkillId, ct)
                     ?? throw new NotFoundException("TrendingSkill", request.TrendingSkillId);
        skill.UseCount++;
        await db.SaveChangesAsync(ct);
        return new TrackTrendingUseResponse(skill.UseCount);
    }
}