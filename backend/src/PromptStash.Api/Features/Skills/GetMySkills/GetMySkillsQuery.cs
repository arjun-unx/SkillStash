using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Skills.GetMySkills;

public sealed record GetMySkillsQuery(int Page, int PageSize) : IRequest<PaginatedList<SkillDto>>;

public sealed class GetMySkillsQueryHandler(
    ISkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<GetMySkillsQuery, PaginatedList<SkillDto>>
{
    public async Task<PaginatedList<SkillDto>> Handle(GetMySkillsQuery request, CancellationToken ct)
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
