using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Library.GetBookmarkedSkills;

public sealed record GetBookmarkedSkillsQuery(int Page, int PageSize, Guid? CollectionId)
    : IRequest<PaginatedList<SkillDto>>;

public sealed class GetBookmarkedSkillsQueryHandler(
    AppDbContext db,
    ISkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<GetBookmarkedSkillsQuery, PaginatedList<SkillDto>>
{
    public async Task<PaginatedList<SkillDto>> Handle(GetBookmarkedSkillsQuery request, CancellationToken ct)
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

        var ordered = ids
            .Select(id => dtoList.First(d => d.Id == id))
            .ToList();

        return new PaginatedList<SkillDto>(ordered, page, pageSize, total);
    }
}
