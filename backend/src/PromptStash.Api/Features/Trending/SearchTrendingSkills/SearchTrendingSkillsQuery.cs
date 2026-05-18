using MediatR;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Services;
using PromptStash.Api.Services.Trending;

namespace PromptStash.Api.Features.Trending.SearchTrendingSkills;

public sealed record SearchTrendingSkillsQuery(
    string? Provider,
    string? Role,
    string? Category,
    string? Search,
    string Sort,
    int Page,
    int PageSize) : IRequest<PaginatedList<TrendingSkillDto>>;

public sealed class SearchTrendingSkillsQueryHandler(
    ITrendingSkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<SearchTrendingSkillsQuery, PaginatedList<TrendingSkillDto>>
{
    public async Task<PaginatedList<TrendingSkillDto>> Handle(SearchTrendingSkillsQuery request, CancellationToken ct)
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
