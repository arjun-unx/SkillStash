using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Models;
namespace PromptStash.Api.Controllers;

[ApiController]
[Route("api/skills")]
public sealed class SkillsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<SkillDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<SkillDto>>> GetPublic(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? search = null,
        [FromQuery] string[]? tags = null,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetPublicFeedRequest(page, pageSize, search, tags), ct));

    [HttpGet("following")]
    [Authorize]
    [ProducesResponseType(typeof(PaginatedList<SkillDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<SkillDto>>> GetFollowing(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetFollowingFeedRequest(page, pageSize), ct));

    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(PaginatedList<SkillDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<SkillDto>>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetMySkillsRequest(page, pageSize), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SkillDto>> GetById([FromRoute] Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetSkillByIdRequest(id), ct));

    [HttpGet("{id:guid}/comments")]
    [ProducesResponseType(typeof(PaginatedList<SkillCommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<SkillCommentDto>>> GetComments(
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetSkillCommentsRequest(id, page, pageSize), ct));

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SkillDto>> Create([FromBody] CreateSkillRequest request, CancellationToken ct)
    {
        var dto = await sender.Send(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SkillDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateSkillRequest request,
        CancellationToken ct)
    {
        if (id != request.SkillId) return BadRequest("Mismatched skill id.");
        return Ok(await sender.Send(request, ct));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteSkillRequest(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/like")]
    [Authorize]
    [ProducesResponseType(typeof(ToggleLikeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ToggleLikeResponse>> ToggleLike([FromRoute] Guid id, CancellationToken ct)
        => Ok(await sender.Send(new ToggleLikeRequest(id), ct));

    [HttpPost("{id:guid}/bookmark")]
    [Authorize]
    [ProducesResponseType(typeof(ToggleBookmarkResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ToggleBookmarkResponse>> ToggleBookmark(
        [FromRoute] Guid id,
        [FromBody] BookmarkBodyDto? body,
        CancellationToken ct)
        => Ok(await sender.Send(new ToggleSkillBookmarkRequest(id, body?.CollectionId, body?.Bookmarked), ct));

    [HttpPost("{id:guid}/copy")]
    [ProducesResponseType(typeof(TrackCopyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrackCopyResponse>> TrackCopy([FromRoute] Guid id, CancellationToken ct)
        => Ok(await sender.Send(new TrackCopyRequest(id), ct));

    [HttpPost("{id:guid}/comments")]
    [Authorize]
    [ProducesResponseType(typeof(SkillCommentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SkillCommentDto>> AddComment(
        [FromRoute] Guid id,
        [FromBody] AddSkillCommentBodyDto body,
        CancellationToken ct)
    {
        var dto = await sender.Send(new AddSkillCommentRequest(id, body.Body), ct);
        return CreatedAtAction(nameof(GetComments), new { id }, dto);
    }
}
