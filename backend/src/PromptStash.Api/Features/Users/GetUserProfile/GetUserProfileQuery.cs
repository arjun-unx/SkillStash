using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Users.GetUserProfile;

public sealed record GetUserProfileQuery(string UserName) : IRequest<UserProfileDto>;

public sealed class GetUserProfileQueryHandler(
    IUserRepository users,
    IFollowRepository follows,
    ISkillRepository skills,
    ICurrentUserService currentUser) : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetUserProfileQuery request, CancellationToken ct)
    {
        var user = await users.GetByUserNameAsync(request.UserName.ToLowerInvariant(), ct)
                   ?? throw new NotFoundException("User", request.UserName);

        var followersCount = await follows.CountFollowersAsync(user.Id, ct);
        var followingCount = await follows.CountFollowingAsync(user.Id, ct);
        var publicCount = await skills.Query()
            .Where(p => p.AuthorId == user.Id && p.Visibility == SkillVisibility.Public)
            .CountAsync(ct);

        var isFollowedByMe = currentUser.UserId is not null &&
            await follows.IsFollowingAsync(currentUser.UserId.Value, user.Id, ct);

        return new UserProfileDto(user.Id, user.UserName, user.DisplayName, user.Bio, user.AvatarUrl,
            followersCount, followingCount, publicCount, isFollowedByMe);
    }
}
