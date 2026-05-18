using FluentAssertions;
using PromptStash.Api.Common.Events;
using PromptStash.Api.Common.Exceptions;
using PromptStash.Api.Features.Auth.Register;
using Xunit;

namespace PromptStash.Tests.Auth;

public sealed class RegisterCommandHandlerTests : TestFixture
{
    private RegisterCommandHandler CreateSut() =>
        new(Users, Hasher, Tokens, Bus);

    [Fact]
    public async Task Register_creates_user_and_returns_token()
    {
        var sut = CreateSut();
        var cmd = new RegisterCommand("alice@promptstash.io", "alice", "Alice", "Password!1");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.UserName.Should().Be("alice");
        Db.Users.Should().HaveCount(1);
        await Bus.Received(1).PublishAsync(Arg.Any<UserRegisteredIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_rejects_duplicate_username()
    {
        var sut = CreateSut();
        await sut.Handle(new RegisterCommand("a@b.io", "alice", "Alice", "Password!1"), CancellationToken.None);

        var dup = sut.Handle(new RegisterCommand("c@d.io", "alice", "Alice2", "Password!1"), CancellationToken.None);

        await dup.Invoking(t => t).Should().ThrowAsync<ConflictException>();
    }
}
