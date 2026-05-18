using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;
using PromptStash.Api.Services.Trending;

namespace PromptStash.Api.Features.Trending.ToggleTrendingBookmark;

public sealed record ToggleTrendingBookmarkCommand(Guid TrendingSkillId) : IRequest<ToggleTrendingBookmarkResponse>;

public sealed class ToggleTrendingBookmarkCommandHandler(
    AppDbContext db,
    ITrendingSkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<ToggleTrendingBookmarkCommand, ToggleTrendingBookmarkResponse>
{
    public async Task<ToggleTrendingBookmarkResponse> Handle(ToggleTrendingBookmarkCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var skill = await repo.GetEntityAsync(request.TrendingSkillId, ct)
                     ?? throw new NotFoundException("TrendingSkill", request.TrendingSkillId);

        var existing = await db.TrendingSkillBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.TrendingSkillId == skill.Id, ct);

        if (existing is not null)
        {
            db.TrendingSkillBookmarks.Remove(existing);
            skill.SaveCount = Math.Max(0, skill.SaveCount - 1);
            await db.SaveChangesAsync(ct);
            return new ToggleTrendingBookmarkResponse(false, skill.SaveCount);
        }

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
}
