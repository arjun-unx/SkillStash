using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Skills.GetPublicFeed;

public sealed record GetPublicFeedQuery(int Page, int PageSize, string? Search, IReadOnlyList<string>? Tags)
    : IRequest<PaginatedList<SkillDto>>;

public sealed class GetPublicFeedQueryHandler(
    ISkillRepository repo,
    ICurrentUserService currentUser) : IRequestHandler<GetPublicFeedQuery, PaginatedList<SkillDto>>
{
    public async Task<PaginatedList<SkillDto>> Handle(GetPublicFeedQuery request, CancellationToken ct)
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
