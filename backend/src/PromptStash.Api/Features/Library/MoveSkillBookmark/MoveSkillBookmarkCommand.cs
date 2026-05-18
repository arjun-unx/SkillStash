using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Library.MoveSkillBookmark;

public sealed record MoveSkillBookmarkCommand(Guid SkillId, Guid? CollectionId) : IRequest<Unit>;

public sealed class MoveSkillBookmarkCommandHandler(
    AppDbContext db,
    ISkillBookmarkRepository bookmarks,
    ICurrentUserService currentUser) : IRequestHandler<MoveSkillBookmarkCommand, Unit>
{
    public async Task<Unit> Handle(MoveSkillBookmarkCommand request, CancellationToken ct)
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
