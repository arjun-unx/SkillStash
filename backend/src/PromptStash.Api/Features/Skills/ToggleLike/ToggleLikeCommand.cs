using MediatR;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Skills.ToggleLike;

public sealed record ToggleLikeCommand(Guid SkillId) : IRequest<ToggleLikeResponse>;

public sealed class ToggleLikeCommandHandler(
    ISkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<ToggleLikeCommand, ToggleLikeResponse>
{
    public async Task<ToggleLikeResponse> Handle(ToggleLikeCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var skill = await repo.GetByIdAsync(request.SkillId, ct)
                     ?? throw new NotFoundException("Skill", request.SkillId);

        if (skill.Visibility == SkillVisibility.Private && skill.AuthorId != userId)
            throw new ForbiddenAccessException();

        var existing = await repo.GetLikeAsync(skill.Id, userId, ct);
        bool liked;
        if (existing is null)
        {
            await repo.AddLikeAsync(new SkillLike { SkillId = skill.Id, UserId = userId }, ct);
            skill.LikeCount++;
            liked = true;
        }
        else
        {
            repo.RemoveLike(existing);
            if (skill.LikeCount > 0) skill.LikeCount--;
            liked = false;
        }

        await repo.SaveAsync(ct);
        return new ToggleLikeResponse(liked, skill.LikeCount);
    }
}
