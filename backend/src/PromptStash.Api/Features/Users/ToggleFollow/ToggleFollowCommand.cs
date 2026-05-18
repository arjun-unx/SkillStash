using MediatR;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Users.ToggleFollow;

public sealed record ToggleFollowCommand(string UserName) : IRequest<ToggleFollowResponse>;

public sealed class ToggleFollowCommandHandler(
    IUserRepository users,
    IFollowRepository follows,
    ICurrentUserService currentUser) : IRequestHandler<ToggleFollowCommand, ToggleFollowResponse>
{
    public async Task<ToggleFollowResponse> Handle(ToggleFollowCommand request, CancellationToken ct)
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
