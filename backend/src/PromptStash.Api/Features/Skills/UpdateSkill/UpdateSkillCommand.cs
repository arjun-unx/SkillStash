using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Events;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Skills.UpdateSkill;

public sealed record UpdateSkillCommand(
    Guid SkillId,
    string Title,
    string Body,
    string? Description,
    string AgentSlug,
    SkillVisibility Visibility,
    IReadOnlyList<string> Tags) : IRequest<SkillDto>;

public sealed class UpdateSkillCommandValidator : AbstractValidator<UpdateSkillCommand>
{
    public UpdateSkillCommandValidator()
    {
        RuleFor(x => x.SkillId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.Description).MaximumLength(280);
        RuleFor(x => x.AgentSlug).NotEmpty().MaximumLength(60);
        RuleForEach(x => x.Tags).MaximumLength(40);
    }
}

public sealed class UpdateSkillCommandHandler(
    ISkillRepository repo,
    IUserRepository users,
    ICurrentUserService currentUser,
    IServiceBusPublisher bus) : IRequestHandler<UpdateSkillCommand, SkillDto>
{
    public async Task<SkillDto> Handle(UpdateSkillCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var skill = await repo.GetByIdAsync(request.SkillId, ct)
                     ?? throw new NotFoundException("Skill", request.SkillId);

        if (skill.AuthorId != userId) throw new ForbiddenAccessException();

        var becamePublic = skill.Visibility != SkillVisibility.Public &&
                           request.Visibility == SkillVisibility.Public;

        skill.Title = request.Title.Trim();
        skill.Body = request.Body;
        skill.Description = request.Description?.Trim();
        skill.AgentSlug = request.AgentSlug.Trim();
        skill.Visibility = request.Visibility;
        skill.Tags = request.Tags.Select(t => t.Trim().ToLowerInvariant()).Distinct().ToList();

        await repo.SaveAsync(ct);

        var author = await users.GetByIdAsync(userId, ct)!;

        if (becamePublic && author is not null)
        {
            await bus.PublishAsync(new SkillPublishedIntegrationEvent
            {
                SkillId = skill.Id,
                Title = skill.Title,
                Description = skill.Description,
                AuthorId = author.Id,
                AuthorEmail = author.Email,
                AuthorDisplayName = author.DisplayName
            }, ct);
        }

        return await repo.Query()
            .AsNoTracking()
            .Where(p => p.Id == skill.Id)
            .Select(p => new SkillDto(
                p.Id, p.Title, p.Body, p.Description, p.AgentSlug, p.Visibility,
                p.Tags, p.CopyCount, p.LikeCount,
                p.Bookmarks.Count,
                p.Comments.Count,
                p.Likes.Any(l => l.UserId == userId),
                p.Bookmarks.Any(b => b.UserId == userId),
                p.AuthorId, p.Author!.DisplayName, p.Author!.UserName, p.Author!.Headline,
                p.CreatedAtUtc, p.UpdatedAtUtc))
            .FirstAsync(ct);
    }
}
