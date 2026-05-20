using MediatR;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Features.Users;

public sealed record GetUserProfileRequest(string UserName) : IRequest<UserProfileDto>;

public sealed class GetUserProfileHandler(
    IUserRepository users,
    IFollowRepository follows,
    ISkillRepository skills,
    ICurrentUserService currentUser) : IRequestHandler<GetUserProfileRequest, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetUserProfileRequest request, CancellationToken ct)
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

public sealed record ToggleFollowRequest(string UserName) : IRequest<ToggleFollowResponse>;

public sealed class ToggleFollowHandler(
    IUserRepository users,
    IFollowRepository follows,
    ICurrentUserService currentUser) : IRequestHandler<ToggleFollowRequest, ToggleFollowResponse>
{
    public async Task<ToggleFollowResponse> Handle(ToggleFollowRequest request, CancellationToken ct)
    {
        var followerId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var target = await users.GetByUserNameAsync(request.UserName.ToLowerInvariant(), ct)
                     ?? throw new NotFoundException("User", request.UserName);

        if (target.Id == followerId)
            throw new ConflictException("You cannot follow yourself.");

        var existing = await follows.FindAsync(followerId, target.Id, ct);
        if (existing is null)
        {
            await follows.AddAsync(new Follow { FollowerId = followerId, FolloweeId = target.Id }, ct);
        }
        else
        {
            follows.Remove(existing);
        }

        await follows.SaveAsync(ct);

        var followers = await follows.CountFollowersAsync(target.Id, ct);
        return new ToggleFollowResponse(existing is null, followers);
    }
}