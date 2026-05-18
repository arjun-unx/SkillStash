using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Skills.GetSkillById;

public sealed record GetSkillByIdQuery(Guid SkillId) : IRequest<SkillDto>;

public sealed class GetSkillByIdQueryHandler(
    ISkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<GetSkillByIdQuery, SkillDto>
{
    public async Task<SkillDto> Handle(GetSkillByIdQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var meta = await repo.Query()
            .AsNoTracking()
            .Where(p => p.Id == request.SkillId)
            .Select(p => new { p.Visibility, p.AuthorId })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Skill", request.SkillId);

        if (meta.Visibility == SkillVisibility.Private && meta.AuthorId != userId)
            throw new ForbiddenAccessException();

        return await repo.Query()
            .AsNoTracking()
            .Where(p => p.Id == request.SkillId)
            .Select(p => new SkillDto(
                p.Id, p.Title, p.Body, p.Description, p.AgentSlug, p.Visibility,
                p.Tags, p.CopyCount, p.LikeCount,
                p.Bookmarks.Count,
                p.Comments.Count,
                userId != null && p.Likes.Any(l => l.UserId == userId.Value),
                userId != null && p.Bookmarks.Any(b => b.UserId == userId.Value),
                p.AuthorId, p.Author!.DisplayName, p.Author!.UserName, p.Author!.Headline,
                p.CreatedAtUtc, p.UpdatedAtUtc))
            .FirstAsync(ct);
    }
}
