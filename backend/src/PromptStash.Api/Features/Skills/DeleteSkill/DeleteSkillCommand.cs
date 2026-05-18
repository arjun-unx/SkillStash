using MediatR;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Skills.DeleteSkill;

public sealed record DeleteSkillCommand(Guid SkillId) : IRequest<Unit>;

public sealed class DeleteSkillCommandHandler(
    ISkillRepository repo,
    ICurrentUserService currentUser,
    IDateTimeProvider clock) : IRequestHandler<DeleteSkillCommand, Unit>
{
    public async Task<Unit> Handle(DeleteSkillCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var skill = await repo.GetByIdAsync(request.SkillId, ct)
                     ?? throw new NotFoundException("Skill", request.SkillId);

        if (skill.AuthorId != userId) throw new ForbiddenAccessException();

        skill.IsDeleted = true;
        skill.DeletedAtUtc = clock.UtcNow;

        await repo.SaveAsync(ct);
        return Unit.Value;
    }
}
