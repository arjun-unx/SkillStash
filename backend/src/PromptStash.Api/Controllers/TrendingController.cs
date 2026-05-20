using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Models;
namespace PromptStash.Api.Controllers;

[ApiController]
[Route("api/trending")]
public sealed class TrendingController(ISender sender) : ControllerBase
{
    [HttpGet("providers")]
    [ProducesResponseType(typeof(IReadOnlyList<TrendingProviderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrendingProviderDto>>> Providers(CancellationToken ct)
        => Ok(await sender.Send(new GetTrendingProvidersRequest(), ct));

    [HttpGet("skills")]
    [ProducesResponseType(typeof(PaginatedList<TrendingSkillDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<TrendingSkillDto>>> Search(
        [FromQuery] string? provider = null,
        [FromQuery] string? role = null,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] string sort = "trending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await sender.Send(new SearchTrendingSkillsRequest(provider, role, category, search, sort, page, pageSize), ct));

    [HttpGet("skills/{id:guid}")]
    [ProducesResponseType(typeof(TrendingSkillDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrendingSkillDto>> GetById([FromRoute] Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetTrendingSkillByIdRequest(id), ct));

    [HttpPost("skills/{id:guid}/bookmark")]
    [Authorize]
    [ProducesResponseType(typeof(ToggleTrendingBookmarkResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ToggleTrendingBookmarkResponse>> ToggleBookmark(
        [FromRoute] Guid id,
        [FromBody] TrendingBookmarkBodyDto? body,
        CancellationToken ct)
        => Ok(await sender.Send(new ToggleTrendingBookmarkRequest(id, body?.Bookmarked), ct));

    [HttpPost("skills/{id:guid}/use")]
    [ProducesResponseType(typeof(TrackTrendingUseResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrackTrendingUseResponse>> TrackUse([FromRoute] Guid id, CancellationToken ct)
        => Ok(await sender.Send(new TrackTrendingUseRequest(id), ct));

    [HttpPost("sync")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> Sync(CancellationToken ct)
    {
        var count = await sender.Send(new SyncTrendingSkillsRequest(), ct);
        return Ok(new { synced = count });
    }
}
