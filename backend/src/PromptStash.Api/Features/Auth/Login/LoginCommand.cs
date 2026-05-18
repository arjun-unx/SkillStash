using FluentValidation;
using MediatR;
using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Services;

namespace PromptStash.Api.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService tokens) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
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
