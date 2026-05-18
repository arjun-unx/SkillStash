using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Library.ListBookmarkCollections;

public sealed record ListBookmarkCollectionsQuery : IRequest<IReadOnlyList<BookmarkCollectionDto>>;

public sealed class ListBookmarkCollectionsQueryHandler(
    AppDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<ListBookmarkCollectionsQuery, IReadOnlyList<BookmarkCollectionDto>>
{
    public async Task<IReadOnlyList<BookmarkCollectionDto>> Handle(ListBookmarkCollectionsQuery request, CancellationToken ct)
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
