using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Skills.GetSkillComments;

public sealed record GetSkillCommentsQuery(Guid SkillId, int Page, int PageSize)
    : IRequest<PaginatedList<SkillCommentDto>>;

public sealed class GetSkillCommentsQueryHandler(
    ISkillRepository skills,
    ICurrentUserService currentUser) : IRequestHandler<GetSkillCommentsQuery, PaginatedList<SkillCommentDto>>
{
    public async Task<PaginatedList<SkillCommentDto>> Handle(GetSkillCommentsQuery request, CancellationToken ct)
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
