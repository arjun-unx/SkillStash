using MediatR;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data;
using PromptStash.Api.Services.Trending;

namespace PromptStash.Api.Features.Trending.TrackTrendingUse;

public sealed record TrackTrendingUseCommand(Guid TrendingSkillId) : IRequest<TrackTrendingUseResponse>;

public sealed class TrackTrendingUseCommandHandler(
    AppDbContext db,
    ITrendingSkillRepository repo) : IRequestHandler<TrackTrendingUseCommand, TrackTrendingUseResponse>
{
    public async Task<TrackTrendingUseResponse> Handle(TrackTrendingUseCommand request, CancellationToken ct)
    {
        var skill = await repo.GetEntityAsync(request.TrendingSkillId, ct)
                     ?? throw new NotFoundException("TrendingSkill", request.TrendingSkillId);
        skill.UseCount++;
        await db.SaveChangesAsync(ct);
        return new TrackTrendingUseResponse(skill.UseCount);
    }
}
