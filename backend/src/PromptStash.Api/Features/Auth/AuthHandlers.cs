using FluentValidation;
using MediatR;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Events;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Features.Auth;

public sealed record GetCurrentUserRequest : IRequest<CurrentUserDto>;

public sealed class GetCurrentUserHandler(
    ICurrentUserService currentUser,
    IUserRepository users) : IRequestHandler<GetCurrentUserRequest, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserRequest request, CancellationToken ct)
    {
        var id = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var user = await users.GetByIdAsync(id, ct) ?? throw new NotFoundException("User", id);

        return new CurrentUserDto(user.Id, user.Email, user.UserName, user.DisplayName,
            user.Bio, user.AvatarUrl, user.EmailNotificationsEnabled);
    }
}

public sealed record LoginRequest(string Email, string Password) : IRequest<AuthResponse>;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class LoginHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService tokens) : IRequestHandler<LoginRequest, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginRequest request, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct)
                   ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var token = tokens.GenerateToken(user);
        return new AuthResponse(user.Id, user.Email, user.UserName, user.DisplayName,
            token.AccessToken, token.ExpiresAtUtc);
    }
}

public sealed record RegisterRequest(
    string Email,
    string UserName,
    string DisplayName,
    string Password) : IRequest<AuthResponse>;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.UserName).NotEmpty().Matches("^[a-zA-Z0-9_\\.-]+$").Length(3, 40);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public sealed class RegisterHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService tokens,
    IServiceBusPublisher bus) : IRequestHandler<RegisterRequest, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterRequest request, CancellationToken ct)
    {
        if (await users.ExistsByEmailOrUserNameAsync(request.Email, request.UserName, ct))
            throw new ConflictException("Email or username is already taken.");

        var user = new AppUser
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            UserName = request.UserName.Trim().ToLowerInvariant(),
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = hasher.Hash(request.Password)
        };
        await users.AddAsync(user, ct);
        await users.SaveAsync(ct);

        await bus.PublishAsync(new UserRegisteredIntegrationEvent
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName
        }, ct);

        var token = tokens.GenerateToken(user);
        return new AuthResponse(user.Id, user.Email, user.UserName, user.DisplayName,
            token.AccessToken, token.ExpiresAtUtc);
    }
}