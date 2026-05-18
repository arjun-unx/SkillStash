using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Events;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Skills.CreateSkill;

public sealed record CreateSkillCommand(
    string Title,
    string Body,
    string? Description,
    string AgentSlug,
    SkillVisibility Visibility,
    IReadOnlyList<string> Tags) : IRequest<SkillDto>;

public sealed class CreateSkillCommandValidator : AbstractValidator<CreateSkillCommand>
{
    public CreateSkillCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(100_000);
        RuleFor(x => x.Description).MaximumLength(280);
        RuleFor(x => x.AgentSlug).NotEmpty().MaximumLength(60);
        RuleForEach(x => x.Tags).MaximumLength(40);
        RuleFor(x => x.Tags).Must(t => t.Count <= 8).WithMessage("Maximum of 8 tags allowed.");
    }
}

public sealed class CreateSkillCommandHandler(
    ISkillRepository repo,
    IUserRepository users,
    ICurrentUserService currentUser,
    IServiceBusPublisher bus) : IRequestHandler<CreateSkillCommand, SkillDto>
{
    public async Task<SkillDto> Handle(CreateSkillCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var author = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User", userId);

        var skill = new Skill
        {
            Title = request.Title.Trim(),
            Body = request.Body,
            Description = request.Description?.Trim(),
            AgentSlug = request.AgentSlug.Trim(),
            Visibility = request.Visibility,
            Tags = request.Tags.Select(t => t.Trim().ToLowerInvariant()).Distinct().ToList(),
            AuthorId = author.Id
        };

        await repo.AddAsync(skill, ct);
        await repo.SaveAsync(ct);

        if (skill.Visibility == SkillVisibility.Public)
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
