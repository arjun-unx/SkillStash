using MediatR;
using PromptStash.Api.Services.Trending;

namespace PromptStash.Api.Features.Trending.SyncTrendingSkills;

public sealed record SyncTrendingSkillsCommand : IRequest<int>;

public sealed class SyncTrendingSkillsCommandHandler(ITrendingSkillSyncService sync)
    : IRequestHandler<SyncTrendingSkillsCommand, int>
{
    public Task<int> Handle(SyncTrendingSkillsCommand request, CancellationToken ct) =>
        sync.SyncAsync(failIfGitHubNotConfigured: true, ct: ct);
}
