using FluentAssertions;
using PromptStash.Api.Common.Events;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Features.Skills.CreateSkill;
using Xunit;

namespace PromptStash.Tests.Skills;

public sealed class CreateSkillCommandHandlerTests : TestFixture
{
    [Fact]
    public async Task Public_skill_is_created_and_event_published()
    {
        var user = new AppUser
        {
            Email = "bob@skillstash.io",
            UserName = "bob",
            DisplayName = "Bob",
            PasswordHash = "x"
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();

        CurrentUser.UserId.Returns(user.Id);

        var sut = new CreateSkillCommandHandler(Skills, Users, CurrentUser, Bus);
        var cmd = new CreateSkillCommand(
            "Code review skill", "# Code review\n\nReview for correctness.", "Desc", "claude",
            SkillVisibility.Public,
            new[] { "code-review", "TAG2" });

        var dto = await sut.Handle(cmd, CancellationToken.None);

        dto.Tags.Should().BeEquivalentTo(new[] { "code-review", "tag2" });
        Db.Skills.Should().HaveCount(1);
        await Bus.Received(1).PublishAsync(Arg.Any<SkillPublishedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Private_skill_does_not_publish_event()
    {
        var user = new AppUser
        {
            Email = "carol@skillstash.io",
            UserName = "carol",
            DisplayName = "Carol",
            PasswordHash = "x"
        };
        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        CurrentUser.UserId.Returns(user.Id);

        var sut = new CreateSkillCommandHandler(Skills, Users, CurrentUser, Bus);
        var cmd = new CreateSkillCommand("T", "# B", null, "any", SkillVisibility.Private, Array.Empty<string>());

        await sut.Handle(cmd, CancellationToken.None);

        await Bus.DidNotReceive().PublishAsync(Arg.Any<SkillPublishedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }
}
