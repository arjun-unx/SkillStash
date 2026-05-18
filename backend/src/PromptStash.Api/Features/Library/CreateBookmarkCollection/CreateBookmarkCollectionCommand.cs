using FluentValidation;
using MediatR;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Library.CreateBookmarkCollection;

public sealed record CreateBookmarkCollectionCommand(string Name) : IRequest<BookmarkCollectionDto>;

public sealed class CreateBookmarkCollectionCommandValidator : AbstractValidator<CreateBookmarkCollectionCommand>
{
    public CreateBookmarkCollectionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
    }
}

public sealed class CreateBookmarkCollectionCommandHandler(
    AppDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<CreateBookmarkCollectionCommand, BookmarkCollectionDto>
{
    public async Task<BookmarkCollectionDto> Handle(CreateBookmarkCollectionCommand request, CancellationToken ct)
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
