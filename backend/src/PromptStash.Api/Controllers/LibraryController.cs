using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Models;
namespace PromptStash.Api.Controllers;

[ApiController]
[Route("api/library")]
[Authorize]
public sealed class LibraryController(ISender sender) : ControllerBase
{
    [HttpGet("collections")]
    [ProducesResponseType(typeof(IReadOnlyList<BookmarkCollectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookmarkCollectionDto>>> ListCollections(CancellationToken ct)
        => Ok(await sender.Send(new ListBookmarkCollectionsRequest(), ct));

    [HttpPost("collections")]
    [ProducesResponseType(typeof(BookmarkCollectionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BookmarkCollectionDto>> CreateCollection(
        [FromBody] CreateBookmarkCollectionRequest request,
        CancellationToken ct)
        => Ok(await sender.Send(request, ct));

    [HttpDelete("collections/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCollection([FromRoute] Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteBookmarkCollectionRequest(id), ct);
        return NoContent();
    }

    [HttpGet("trending-bookmarks")]
    [ProducesResponseType(typeof(PaginatedList<TrendingSkillDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<TrendingSkillDto>>> TrendingBookmarks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 60,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetBookmarkedTrendingSkillsRequest(page, pageSize), ct));

    [HttpGet("bookmarks")]
    [ProducesResponseType(typeof(PaginatedList<SkillDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<SkillDto>>> Bookmarks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] Guid? collectionId = null,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetBookmarkedSkillsRequest(page, pageSize, collectionId), ct));

    [HttpPut("bookmarks/{skillId:guid}/collection")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MoveBookmark(
        [FromRoute] Guid skillId,
        [FromBody] MoveBookmarkToCollectionDto body,
        CancellationToken ct)
    {
        await sender.Send(new MoveSkillBookmarkRequest(skillId, body.CollectionId), ct);
        return NoContent();
    }
}
