using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Events;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Features.Skills;

public sealed record AddSkillCommentRequest(Guid SkillId, string Body) : IRequest<SkillCommentDto>;

public sealed class AddSkillCommentRequestValidator : AbstractValidator<AddSkillCommentRequest>
{
    public AddSkillCommentRequestValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}

public sealed class AddSkillCommentHandler(
    AppDbContext db,
    ISkillRepository skills,
    IUserRepository users,
    ICurrentUserService currentUser) : IRequestHandler<AddSkillCommentRequest, SkillCommentDto>
{
    public async Task<SkillCommentDto> Handle(AddSkillCommentRequest request, CancellationToken ct)
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

public sealed record CreateSkillRequest(
    string Title,
    string Body,
    string? Description,
    string AgentSlug,
    SkillVisibility Visibility,
    IReadOnlyList<string> Tags) : IRequest<SkillDto>;

public sealed class CreateSkillRequestValidator : AbstractValidator<CreateSkillRequest>
{
    public CreateSkillRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(100_000);
        RuleFor(x => x.Description).MaximumLength(280);
        RuleFor(x => x.AgentSlug).NotEmpty().MaximumLength(60);
        RuleForEach(x => x.Tags).MaximumLength(40);
        RuleFor(x => x.Tags).Must(t => t.Count <= 8).WithMessage("Maximum of 8 tags allowed.");
    }
}

public sealed class CreateSkillHandler(
    ISkillRepository repo,
    IUserRepository users,
    ICurrentUserService currentUser,
    IServiceBusPublisher bus) : IRequestHandler<CreateSkillRequest, SkillDto>
{
    public async Task<SkillDto> Handle(CreateSkillRequest request, CancellationToken ct)
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

public sealed record DeleteSkillRequest(Guid SkillId) : IRequest<Unit>;

public sealed class DeleteSkillHandler(
    ISkillRepository repo,
    ICurrentUserService currentUser,
    IDateTimeProvider clock) : IRequestHandler<DeleteSkillRequest, Unit>
{
    public async Task<Unit> Handle(DeleteSkillRequest request, CancellationToken ct)
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

public sealed record GetFollowingFeedRequest(int Page, int PageSize) : IRequest<PaginatedList<SkillDto>>;

public sealed class GetFollowingFeedHandler(
    ISkillRepository repo,
    IFollowRepository follows,
    ICurrentUserService currentUser) : IRequestHandler<GetFollowingFeedRequest, PaginatedList<SkillDto>>
{
    public async Task<PaginatedList<SkillDto>> Handle(GetFollowingFeedRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var followees = await follows.GetFolloweeIdsAsync(userId, ct);
        if (followees.Count == 0)
        {
            var empty = Array.Empty<SkillDto>().AsQueryable();
            return await PaginatedList<SkillDto>.CreateAsync(empty, request.Page, request.PageSize, ct);
        }

        var query = repo.Query()
            .Include(p => p.Author)
            .Include(p => p.Likes)
            .Where(p => p.Visibility == SkillVisibility.Public && followees.Contains(p.AuthorId))
            .OrderByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc);

        var projected = query.Select(p => new SkillDto(
            p.Id, p.Title, p.Body, p.Description, p.AgentSlug, p.Visibility,
            p.Tags, p.CopyCount, p.LikeCount,
            p.Bookmarks.Count,
            p.Comments.Count,
            p.Likes.Any(l => l.UserId == userId),
            p.Bookmarks.Any(b => b.UserId == userId),
            p.AuthorId, p.Author!.DisplayName, p.Author!.UserName, p.Author!.Headline,
            p.CreatedAtUtc, p.UpdatedAtUtc));

        return await PaginatedList<SkillDto>.CreateAsync(projected, request.Page, request.PageSize, ct);
    }
}

public sealed record GetMySkillsRequest(int Page, int PageSize) : IRequest<PaginatedList<SkillDto>>;

public sealed class GetMySkillsHandler(
    ISkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<GetMySkillsRequest, PaginatedList<SkillDto>>
{
    public async Task<PaginatedList<SkillDto>> Handle(GetMySkillsRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var query = repo.Query()
            .Include(p => p.Author)
            .Include(p => p.Likes)
            .Where(p => p.AuthorId == userId)
            .OrderByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc);

        var projected = query.Select(p => new SkillDto(
            p.Id, p.Title, p.Body, p.Description, p.AgentSlug, p.Visibility,
            p.Tags, p.CopyCount, p.LikeCount,
            p.Bookmarks.Count,
            p.Comments.Count,
            p.Likes.Any(l => l.UserId == userId),
            p.Bookmarks.Any(b => b.UserId == userId),
            p.AuthorId, p.Author!.DisplayName, p.Author!.UserName, p.Author!.Headline,
            p.CreatedAtUtc, p.UpdatedAtUtc));

        return await PaginatedList<SkillDto>.CreateAsync(projected, request.Page, request.PageSize, ct);
    }
}

public sealed record GetPublicFeedRequest(int Page, int PageSize, string? Search, IReadOnlyList<string>? Tags)
    : IRequest<PaginatedList<SkillDto>>;

public sealed class GetPublicFeedHandler(
    ISkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<GetPublicFeedRequest, PaginatedList<SkillDto>>
{
    public async Task<PaginatedList<SkillDto>> Handle(GetPublicFeedRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var query = repo.Query()
            .Include(p => p.Author)
            .Include(p => p.Likes)
            .Where(p => p.Visibility == SkillVisibility.Public);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(p =>
                EF.Functions.ILike(p.Title, $"%{s}%") ||
                (p.Description != null && EF.Functions.ILike(p.Description, $"%{s}%")));
        }

        if (request.Tags is { Count: > 0 })
        {
            var normalized = request.Tags
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => t.Length > 0)
                .Distinct()
                .ToList();
            if (normalized.Count > 0)
                query = query.Where(p => normalized.Any(t => p.Tags.Contains(t)));
        }

        query = query.OrderByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc);

        var projected = query.Select(p => new SkillDto(
            p.Id, p.Title, p.Body, p.Description, p.AgentSlug, p.Visibility,
            p.Tags, p.CopyCount, p.LikeCount,
            p.Bookmarks.Count,
            p.Comments.Count,
            userId != null && p.Likes.Any(l => l.UserId == userId.Value),
            userId != null && p.Bookmarks.Any(b => b.UserId == userId.Value),
            p.AuthorId, p.Author!.DisplayName, p.Author!.UserName, p.Author!.Headline,
            p.CreatedAtUtc, p.UpdatedAtUtc));

        return await PaginatedList<SkillDto>.CreateAsync(projected, request.Page, request.PageSize, ct);
    }
}

public sealed record GetSkillByIdRequest(Guid SkillId) : IRequest<SkillDto>;

public sealed class GetSkillByIdHandler(
    ISkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<GetSkillByIdRequest, SkillDto>
{
    public async Task<SkillDto> Handle(GetSkillByIdRequest request, CancellationToken ct)
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

public sealed record GetSkillCommentsRequest(Guid SkillId, int Page, int PageSize)
    : IRequest<PaginatedList<SkillCommentDto>>;

public sealed class GetSkillCommentsHandler(
    ISkillRepository skills,
    ICurrentUserService currentUser) : IRequestHandler<GetSkillCommentsRequest, PaginatedList<SkillCommentDto>>
{
    public async Task<PaginatedList<SkillCommentDto>> Handle(GetSkillCommentsRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var meta = await skills.Query()
            .AsNoTracking()
            .Where(p => p.Id == request.SkillId)
            .Select(p => new { p.Visibility, p.AuthorId })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Skill", request.SkillId);

        if (meta.Visibility == SkillVisibility.Private && meta.AuthorId != userId)
            throw new ForbiddenAccessException();

        var projected = skills.Query()
            .Where(p => p.Id == request.SkillId)
            .SelectMany(p => p.Comments)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new SkillCommentDto(
                c.Id, c.Body, c.User!.DisplayName, c.User!.UserName, c.CreatedAtUtc));

        return await PaginatedList<SkillCommentDto>.CreateAsync(projected, request.Page, request.PageSize, ct);
    }
}

public sealed record ToggleSkillBookmarkRequest(Guid SkillId, Guid? CollectionId, bool? Bookmarked = null)
    : IRequest<ToggleBookmarkResponse>;

public sealed class ToggleSkillBookmarkHandler(
    ISkillRepository skills,
    ISkillBookmarkRepository bookmarks,
    AppDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<ToggleSkillBookmarkRequest, ToggleBookmarkResponse>
{
    public async Task<ToggleBookmarkResponse> Handle(ToggleSkillBookmarkRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var skill = await skills.GetByIdAsync(request.SkillId, ct)
                     ?? throw new NotFoundException("Skill", request.SkillId);

        if (skill.Visibility == SkillVisibility.Private && skill.AuthorId != userId)
            throw new ForbiddenAccessException();

        if (request.CollectionId is { } cid &&
            !await db.BookmarkCollections.AnyAsync(c => c.Id == cid && c.UserId == userId, ct))
            throw new NotFoundException("Collection", cid);

        var existing = await bookmarks.FindAsync(userId, request.SkillId, ct);
        var desired = request.Bookmarked ?? (existing is null);

        if (desired)
        {
            if (existing is not null)
            {
                var count = await bookmarks.CountForSkillAsync(request.SkillId, ct);
                return new ToggleBookmarkResponse(true, count);
            }

            await bookmarks.AddAsync(new SkillBookmark
            {
                UserId = userId,
                SkillId = request.SkillId,
                CollectionId = request.CollectionId,
                CreatedAtUtc = DateTime.UtcNow
            }, ct);
            await bookmarks.SaveAsync(ct);
            var addedCount = await bookmarks.CountForSkillAsync(request.SkillId, ct);
            return new ToggleBookmarkResponse(true, addedCount);
        }

        if (existing is null)
        {
            var count = await bookmarks.CountForSkillAsync(request.SkillId, ct);
            return new ToggleBookmarkResponse(false, count);
        }

        bookmarks.Remove(existing);
        await bookmarks.SaveAsync(ct);
        var removedCount = await bookmarks.CountForSkillAsync(request.SkillId, ct);
        return new ToggleBookmarkResponse(false, removedCount);
    }
}

public sealed record ToggleLikeRequest(Guid SkillId) : IRequest<ToggleLikeResponse>;

public sealed class ToggleLikeHandler(
    ISkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<ToggleLikeRequest, ToggleLikeResponse>
{
    public async Task<ToggleLikeResponse> Handle(ToggleLikeRequest request, CancellationToken ct)
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

public sealed record TrackCopyRequest(Guid SkillId) : IRequest<TrackCopyResponse>;

public sealed class TrackCopyHandler(
    ISkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<TrackCopyRequest, TrackCopyResponse>
{
    public async Task<TrackCopyResponse> Handle(TrackCopyRequest request, CancellationToken ct)
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

public sealed record UpdateSkillRequest(
    Guid SkillId,
    string Title,
    string Body,
    string? Description,
    string AgentSlug,
    SkillVisibility Visibility,
    IReadOnlyList<string> Tags) : IRequest<SkillDto>;

public sealed class UpdateSkillRequestValidator : AbstractValidator<UpdateSkillRequest>
{
    public UpdateSkillRequestValidator()
    {
        RuleFor(x => x.SkillId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.Description).MaximumLength(280);
        RuleFor(x => x.AgentSlug).NotEmpty().MaximumLength(60);
        RuleForEach(x => x.Tags).MaximumLength(40);
    }
}

public sealed class UpdateSkillHandler(
    ISkillRepository repo,
    IUserRepository users,
    ICurrentUserService currentUser,
    IServiceBusPublisher bus) : IRequestHandler<UpdateSkillRequest, SkillDto>
{
    public async Task<SkillDto> Handle(UpdateSkillRequest request, CancellationToken ct)
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