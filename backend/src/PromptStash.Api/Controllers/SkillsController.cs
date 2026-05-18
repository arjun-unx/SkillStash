using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Features.Skills.AddSkillComment;
using PromptStash.Api.Features.Skills.CreateSkill;
using PromptStash.Api.Features.Skills.DeleteSkill;
using PromptStash.Api.Features.Skills.GetFollowingFeed;
using PromptStash.Api.Features.Skills.GetMySkills;
using PromptStash.Api.Features.Skills.GetSkillById;
using PromptStash.Api.Features.Skills.GetSkillComments;
using PromptStash.Api.Features.Skills.GetPublicFeed;
using PromptStash.Api.Features.Skills.ToggleBookmark;
using PromptStash.Api.Features.Skills.ToggleLike;
using PromptStash.Api.Features.Skills.TrackCopy;
using PromptStash.Api.Features.Skills.UpdateSkill;

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
        => Ok(await sender.Send(new GetPublicFeedQuery(page, pageSize, search, tags), ct));

    [HttpGet("following")]
    [Authorize]
    [ProducesResponseType(typeof(PaginatedList<SkillDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<SkillDto>>> GetFollowing(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetFollowingFeedQuery(page, pageSize), ct));

    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(PaginatedList<SkillDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<SkillDto>>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetMySkillsQuery(page, pageSize), ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SkillDto>> GetById([FromRoute] Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetSkillByIdQuery(id), ct));

    [HttpGet("{id:guid}/comments")]
    [ProducesResponseType(typeof(PaginatedList<SkillCommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<SkillCommentDto>>> GetComments(
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await sender.Send(new GetSkillCommentsQuery(id, page, pageSize), ct));

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SkillDto>> Create([FromBody] CreateSkillCommand command, CancellationToken ct)
    {
        var dto = await sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SkillDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateSkillCommand command,
        CancellationToken ct)
    {
        if (id != command.SkillId) return BadRequest("Mismatched skill id.");
        return Ok(await sender.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteSkillCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/like")]
    [Authorize]
    [ProducesResponseType(typeof(ToggleLikeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ToggleLikeResponse>> ToggleLike([FromRoute] Guid id, CancellationToken ct)
        => Ok(await sender.Send(new ToggleLikeCommand(id), ct));

    [HttpPost("{id:guid}/bookmark")]
    [Authorize]
    [ProducesResponseType(typeof(ToggleBookmarkResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ToggleBookmarkResponse>> ToggleBookmark(
        [FromRoute] Guid id,
        [FromBody] BookmarkBodyDto? body,
        CancellationToken ct)
        => Ok(await sender.Send(new ToggleSkillBookmarkCommand(id, body?.CollectionId), ct));

    [HttpPost("{id:guid}/copy")]
    [ProducesResponseType(typeof(TrackCopyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrackCopyResponse>> TrackCopy([FromRoute] Guid id, CancellationToken ct)
        => Ok(await sender.Send(new TrackCopyCommand(id), ct));

    [HttpPost("{id:guid}/comments")]
    [Authorize]
    [ProducesResponseType(typeof(SkillCommentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SkillCommentDto>> AddComment(
        [FromRoute] Guid id,
        [FromBody] AddSkillCommentRequest body,
        CancellationToken ct)
    {
        var dto = await sender.Send(new AddSkillCommentCommand(id, body.Body), ct);
        return CreatedAtAction(nameof(GetComments), new { id }, dto);
    }
}
