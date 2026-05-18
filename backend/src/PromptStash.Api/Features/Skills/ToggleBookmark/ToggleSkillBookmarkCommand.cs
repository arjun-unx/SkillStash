using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Skills.ToggleBookmark;

public sealed record ToggleSkillBookmarkCommand(Guid SkillId, Guid? CollectionId) : IRequest<ToggleBookmarkResponse>;

public sealed class ToggleSkillBookmarkCommandHandler(
    ISkillRepository skills,
    ISkillBookmarkRepository bookmarks,
    AppDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<ToggleSkillBookmarkCommand, ToggleBookmarkResponse>
{
    public async Task<ToggleBookmarkResponse> Handle(ToggleSkillBookmarkCommand request, CancellationToken ct)
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
        if (existing is not null)
        {
            bookmarks.Remove(existing);
            await bookmarks.SaveAsync(ct);
            var removedCount = await bookmarks.CountForSkillAsync(request.SkillId, ct);
            return new ToggleBookmarkResponse(false, removedCount);
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
}
