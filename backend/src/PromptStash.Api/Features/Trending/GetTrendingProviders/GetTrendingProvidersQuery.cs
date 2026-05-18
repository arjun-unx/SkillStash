using MediatR;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Services.Trending;

namespace PromptStash.Api.Features.Trending.GetTrendingProviders;

public sealed record GetTrendingProvidersQuery : IRequest<IReadOnlyList<TrendingProviderDto>>;

public sealed class GetTrendingProvidersQueryHandler(ITrendingSkillRepository repo)
    : IRequestHandler<GetTrendingProvidersQuery, IReadOnlyList<TrendingProviderDto>>
{
    public async Task<IReadOnlyList<TrendingProviderDto>> Handle(GetTrendingProvidersQuery request, CancellationToken ct)
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
