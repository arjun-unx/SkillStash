using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PromptStash.Api.Common.Settings;
using PromptStash.Api.Data;
using PromptStash.Api.Services;

namespace PromptStash.Tests;

public abstract class TestFixture : IDisposable
{
    protected AppDbContext Db { get; }
    protected IUserRepository Users { get; }
    protected ISkillRepository Skills { get; }
    protected IFollowRepository Follows { get; }
    protected ICurrentUserService CurrentUser { get; }
    protected IPasswordHasher Hasher { get; }
    protected IJwtTokenService Tokens { get; }
    protected IServiceBusPublisher Bus { get; }
    protected IDateTimeProvider Clock { get; }

    protected TestFixture()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"SkillStashTests-{Guid.NewGuid()}")
            .Options;
        Db = new AppDbContext(options);

        Users = new UserRepository(Db);
        Skills = new SkillRepository(Db);
        Follows = new FollowRepository(Db);

        CurrentUser = Substitute.For<ICurrentUserService>();
        Hasher = new BcryptPasswordHasher();
        Tokens = new JwtTokenService(Options.Create(new JwtOptions
        {
            Secret = "this-is-a-test-secret-with-32+chars-aaaaaaaaaaaaaaaaaaa",
            Issuer = "SkillStashTests",
            Audience = "SkillStashTests.Clients",
            ExpiryMinutes = 30
        }));
        Bus = Substitute.For<IServiceBusPublisher>();
        Clock = Substitute.For<IDateTimeProvider>();
        Clock.UtcNow.Returns(_ => DateTime.UtcNow);
    }

    protected ILogger<T> NullLoggerFor<T>() => NullLogger<T>.Instance;

    public void Dispose() => Db.Dispose();
}
