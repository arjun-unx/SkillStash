using FluentValidation;
using MediatR;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Events;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string UserName,
    string DisplayName,
    string Password) : IRequest<AuthResponse>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.UserName).NotEmpty().Matches("^[a-zA-Z0-9_\\.-]+$").Length(3, 40);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public sealed class RegisterCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService tokens,
    IServiceBusPublisher bus) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
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
