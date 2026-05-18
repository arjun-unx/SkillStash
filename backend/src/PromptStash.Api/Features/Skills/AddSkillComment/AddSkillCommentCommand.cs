using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Skills.AddSkillComment;

public sealed record AddSkillCommentCommand(Guid SkillId, string Body) : IRequest<SkillCommentDto>;

public sealed class AddSkillCommentCommandValidator : AbstractValidator<AddSkillCommentCommand>
{
    public AddSkillCommentCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}

public sealed class AddSkillCommentCommandHandler(
    AppDbContext db,
    ISkillRepository skills,
    IUserRepository users,
    ICurrentUserService currentUser) : IRequestHandler<AddSkillCommentCommand, SkillCommentDto>
{
    public async Task<SkillCommentDto> Handle(AddSkillCommentCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var skill = await skills.GetByIdAsync(request.SkillId, ct)
                     ?? throw new NotFoundException("Skill", request.SkillId);

        if (skill.Visibility == SkillVisibility.Private && skill.AuthorId != userId)
            throw new ForbiddenAccessException();

        var author = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User", userId);

        var comment = new SkillComment
        {
            SkillId = skill.Id,
            UserId = userId,
            Body = request.Body.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await db.SkillComments.AddAsync(comment, ct);
        await db.SaveChangesAsync(ct);

        return new SkillCommentDto(
            comment.Id, comment.Body, author.DisplayName, author.UserName, comment.CreatedAtUtc);
    }
}
