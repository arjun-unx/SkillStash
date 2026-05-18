using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Features.Users.GetUserProfile;
using PromptStash.Api.Features.Users.ToggleFollow;

namespace PromptStash.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet("{userName}")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileDto>> GetProfile([FromRoute] string userName, CancellationToken ct)
        => Ok(await sender.Send(new GetUserProfileQuery(userName), ct));

    [HttpPost("{userName}/follow")]
    [Authorize]
    [ProducesResponseType(typeof(ToggleFollowResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ToggleFollowResponse>> ToggleFollow(
        [FromRoute] string userName, CancellationToken ct)
        => Ok(await sender.Send(new ToggleFollowCommand(userName), ct));
}
