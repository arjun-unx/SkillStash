using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Library.DeleteBookmarkCollection;

public sealed record DeleteBookmarkCollectionCommand(Guid CollectionId) : IRequest<Unit>;

public sealed class DeleteBookmarkCollectionCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<DeleteBookmarkCollectionCommand, Unit>
{
    public async Task<Unit> Handle(DeleteBookmarkCollectionCommand request, CancellationToken ct)
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
