using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Features.Library;

public sealed record CreateBookmarkCollectionRequest(string Name) : IRequest<BookmarkCollectionDto>;

public sealed class CreateBookmarkCollectionRequestValidator : AbstractValidator<CreateBookmarkCollectionRequest>
{
    public CreateBookmarkCollectionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
    }
}

public sealed class CreateBookmarkCollectionHandler(
    AppDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<CreateBookmarkCollectionRequest, BookmarkCollectionDto>
{
    public async Task<BookmarkCollectionDto> Handle(CreateBookmarkCollectionRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var col = new BookmarkCollection
        {
            UserId = userId,
            Name = request.Name.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        await db.BookmarkCollections.AddAsync(col, ct);
        await db.SaveChangesAsync(ct);
        return new BookmarkCollectionDto(col.Id, col.Name, 0);
    }
}

public sealed record DeleteBookmarkCollectionRequest(Guid CollectionId) : IRequest<Unit>;

public sealed class DeleteBookmarkCollectionHandler(
    AppDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<DeleteBookmarkCollectionRequest, Unit>
{
    public async Task<Unit> Handle(DeleteBookmarkCollectionRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var col = await db.BookmarkCollections
            .Include(c => c.Bookmarks)
            .FirstOrDefaultAsync(c => c.Id == request.CollectionId && c.UserId == userId, ct)
                     ?? throw new NotFoundException("Collection", request.CollectionId);

        foreach (var b in col.Bookmarks)
            b.CollectionId = null;

        db.BookmarkCollections.Remove(col);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public sealed record GetBookmarkedSkillsRequest(int Page, int PageSize, Guid? CollectionId)
    : IRequest<PaginatedList<SkillDto>>;

public sealed class GetBookmarkedSkillsHandler(
    AppDbContext db,
    ISkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<GetBookmarkedSkillsRequest, PaginatedList<SkillDto>>
{
    public async Task<PaginatedList<SkillDto>> Handle(GetBookmarkedSkillsRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var bookmarkQuery = db.SkillBookmarks.AsNoTracking()
            .Where(b => b.UserId == userId &&
                        (!request.CollectionId.HasValue || b.CollectionId == request.CollectionId))
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => b.SkillId);

        var total = await bookmarkQuery.CountAsync(ct);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;
        var ids = await bookmarkQuery.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        if (ids.Count == 0)
            return new PaginatedList<SkillDto>(Array.Empty<SkillDto>(), page, pageSize, total);

        var idSet = ids.ToHashSet();
        var dtoList = await repo.Query()
            .AsNoTracking()
            .Where(p => idSet.Contains(p.Id))
            .Select(p => new SkillDto(
                p.Id, p.Title, p.Body, p.Description, p.AgentSlug, p.Visibility,
                p.Tags, p.CopyCount, p.LikeCount,
                p.Bookmarks.Count,
                p.Comments.Count,
                p.Likes.Any(l => l.UserId == userId),
                p.Bookmarks.Any(b => b.UserId == userId),
                p.AuthorId, p.Author!.DisplayName, p.Author!.UserName, p.Author!.Headline,
                p.CreatedAtUtc, p.UpdatedAtUtc))
            .ToListAsync(ct);

        var byId = dtoList.ToDictionary(d => d.Id);
        var ordered = ids
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .ToList();

        return new PaginatedList<SkillDto>(ordered, page, pageSize, total);
    }
}

public sealed record ListBookmarkCollectionsRequest : IRequest<IReadOnlyList<BookmarkCollectionDto>>;

public sealed class ListBookmarkCollectionsHandler(
    AppDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<ListBookmarkCollectionsRequest, IReadOnlyList<BookmarkCollectionDto>>
{
    public async Task<IReadOnlyList<BookmarkCollectionDto>> Handle(ListBookmarkCollectionsRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        return await db.BookmarkCollections
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new BookmarkCollectionDto(c.Id, c.Name, c.Bookmarks.Count))
            .ToListAsync(ct);
    }
}

public sealed record MoveSkillBookmarkRequest(Guid SkillId, Guid? CollectionId) : IRequest<Unit>;

public sealed class MoveSkillBookmarkHandler(
    AppDbContext db,
    ISkillBookmarkRepository bookmarks,
    ICurrentUserService currentUser) : IRequestHandler<MoveSkillBookmarkRequest, Unit>
{
    public async Task<Unit> Handle(MoveSkillBookmarkRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var b = await bookmarks.FindAsync(userId, request.SkillId, ct)
                ?? throw new NotFoundException("Bookmark", request.SkillId);

        if (request.CollectionId is { } cid &&
            !await db.BookmarkCollections.AnyAsync(c => c.Id == cid && c.UserId == userId, ct))
            throw new NotFoundException("Collection", cid);

        b.CollectionId = request.CollectionId;
        b.UpdatedAtUtc = DateTime.UtcNow;
        await bookmarks.SaveAsync(ct);
        return Unit.Value;
    }
}

public sealed record GetBookmarkedTrendingSkillsRequest(int Page, int PageSize)
    : IRequest<PaginatedList<TrendingSkillDto>>;

public sealed class GetBookmarkedTrendingSkillsHandler(
    AppDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<GetBookmarkedTrendingSkillsRequest, PaginatedList<TrendingSkillDto>>
{
    public async Task<PaginatedList<TrendingSkillDto>> Handle(GetBookmarkedTrendingSkillsRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var projected = db.TrendingSkillBookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new TrendingSkillDto(
                b.TrendingSkill!.Id,
                b.TrendingSkill.ProviderSlug,
                b.TrendingSkill.ProviderSlug,
                b.TrendingSkill.SourceName,
                b.TrendingSkill.SourceUrl,
                b.TrendingSkill.Title,
                b.TrendingSkill.Body,
                b.TrendingSkill.Snippet ?? "",
                b.TrendingSkill.RoleCategory,
                b.TrendingSkill.Category,
                b.TrendingSkill.Tags,
                b.TrendingSkill.TrendingScore,
                b.TrendingSkill.UseCount,
                b.TrendingSkill.SaveCount,
                b.TrendingSkill.Rating,
                b.TrendingSkill.SourceUpdatedAtUtc,
                b.TrendingSkill.SyncedAtUtc,
                true));

        return await PaginatedList<TrendingSkillDto>.CreateAsync(projected, request.Page, request.PageSize, ct);
    }
}