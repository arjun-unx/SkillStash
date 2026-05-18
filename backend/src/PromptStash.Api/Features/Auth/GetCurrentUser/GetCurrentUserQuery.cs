using MediatR;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Auth.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUserService currentUser,
    IUserRepository users) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        var id = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var user = await users.GetByIdAsync(id, ct) ?? throw new NotFoundException("User", id);

        return new CurrentUserDto(user.Id, user.Email, user.UserName, user.DisplayName,
            user.Bio, user.AvatarUrl, user.EmailNotificationsEnabled);
    }
}
