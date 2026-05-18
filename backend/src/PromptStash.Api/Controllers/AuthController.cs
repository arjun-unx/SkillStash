using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Features.Auth.GetCurrentUser;
using PromptStash.Api.Features.Auth.Login;
using PromptStash.Api.Features.Auth.Register;

namespace PromptStash.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterCommand command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginCommand command, CancellationToken ct)
        => Ok(await sender.Send(command, ct));

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken ct)
        => Ok(await sender.Send(new GetCurrentUserQuery(), ct));
}
