using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Features.Library.CreateBookmarkCollection;
using PromptStash.Api.Features.Library.DeleteBookmarkCollection;
using PromptStash.Api.Features.Library.GetBookmarkedSkills;
using PromptStash.Api.Features.Library.ListBookmarkCollections;
using PromptStash.Api.Features.Library.MoveSkillBookmark;

namespace PromptStash.Api.Controllers;

[ApiController]
[Route("api/library")]
[Authorize]
public sealed class LibraryController(ISender sender) : ControllerBase
{
    [HttpGet("collections")]
    [ProducesResponseType(typeof(IReadOnlyList<BookmarkCollectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookmarkCollectionDto>>> ListCollections(CancellationToken ct)
        => Ok(await sender.Send(new ListBookmarkCollectionsQuery(), ct));

    [HttpPost("collections")]
    [ProducesResponseType(typeof(BookmarkCollectionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookmarkCollectionDto>> CreateCollection(
        [FromBody] CreateBookmarkCollectionCommand command,
        CancellationToken ct)
        => Ok(await sender.Send(command, ct));

    [HttpDelete("collections/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCollection([FromRoute] Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteBookmarkCollectionCommand(id), ct);
        return NoContent();
    }

    [HttpGet("bookmarks")]
    [ProducesResponseType(typeof(PaginatedList<SkillDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<SkillDto>>> Bookmarks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] Guid? collectionId = null,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetBookmarkedSkillsQuery(page, pageSize, collectionId), ct));

    [HttpPut("bookmarks/{skillId:guid}/collection")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MoveBookmark(
        [FromRoute] Guid skillId,
        [FromBody] MoveBookmarkToCollectionDto body,
        CancellationToken ct)
    {
        await sender.Send(new MoveSkillBookmarkCommand(skillId, body.CollectionId), ct);
        return NoContent();
    }
}
