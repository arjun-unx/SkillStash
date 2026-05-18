using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Skills.GetFollowingFeed;

public sealed record GetFollowingFeedQuery(int Page, int PageSize) : IRequest<PaginatedList<SkillDto>>;

public sealed class GetFollowingFeedQueryHandler(
    ISkillRepository repo,
    IFollowRepository follows,
    ICurrentUserService currentUser) : IRequestHandler<GetFollowingFeedQuery, PaginatedList<SkillDto>>
{
    public async Task<PaginatedList<SkillDto>> Handle(GetFollowingFeedQuery request, CancellationToken ct)
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
