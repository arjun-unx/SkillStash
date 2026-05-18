using MediatR;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Services;
using PromptStash.Api.Services.Trending;

namespace PromptStash.Api.Features.Trending.GetTrendingSkillById;

public sealed record GetTrendingSkillByIdQuery(Guid Id) : IRequest<TrendingSkillDto>;

public sealed class GetTrendingSkillByIdQueryHandler(
    ITrendingSkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<GetTrendingSkillByIdQuery, TrendingSkillDto>
{
    public async Task<TrendingSkillDto> Handle(GetTrendingSkillByIdQuery request, CancellationToken ct)
    {
        var dto = await repo.GetByIdAsync(request.Id, currentUser.UserId, ct)
                  ?? throw new NotFoundException("TrendingSkill", request.Id);
        var meta = TrendingProviderCatalog.Find(dto.ProviderSlug);
        return meta is null ? dto : dto with { ProviderName = meta.Name };
    }
}
