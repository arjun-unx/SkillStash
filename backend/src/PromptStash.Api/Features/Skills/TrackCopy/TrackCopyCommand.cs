using MediatR;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Skills.TrackCopy;

public sealed record TrackCopyCommand(Guid SkillId) : IRequest<TrackCopyResponse>;

public sealed class TrackCopyCommandHandler(
    ISkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<TrackCopyCommand, TrackCopyResponse>
{
    public async Task<TrackCopyResponse> Handle(TrackCopyCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var skill = await repo.GetByIdAsync(request.SkillId, ct)
                     ?? throw new NotFoundException("Skill", request.SkillId);

        if (skill.Visibility == SkillVisibility.Private && skill.AuthorId != userId)
            throw new ForbiddenAccessException();

        skill.CopyCount++;
        await repo.SaveAsync(ct);
        return new TrackCopyResponse(skill.CopyCount);
    }
}
