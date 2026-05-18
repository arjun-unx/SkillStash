using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PromptStash.Api.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var sub = accessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email =>
        accessor.HttpContext?.User?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
