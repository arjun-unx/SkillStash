using FluentAssertions;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Features.Skills.ToggleLike;
using Xunit;

namespace PromptStash.Tests.Skills;

public sealed class ToggleLikeCommandHandlerTests : TestFixture
{
    [Fact]
    public async Task First_call_likes_then_second_unlikes()
    {
        var author = new AppUser
        {
            Email = "author@a.io",
            UserName = "author",
            DisplayName = "Author",
            PasswordHash = "x"
        };
        var actor = new AppUser
        {
            Email = "actor@a.io",
            UserName = "actor",
            DisplayName = "Actor",
            PasswordHash = "x"
        };
        Db.Users.AddRange(author, actor);
        var skill = new Skill
        {
            Title = "T",
            Body = "# B",
            Visibility = SkillVisibility.Public,
            AuthorId = author.Id
        };
        Db.Skills.Add(skill);
        await Db.SaveChangesAsync();

        CurrentUser.UserId.Returns(actor.Id);

        var sut = new ToggleLikeCommandHandler(Skills, CurrentUser);

        var liked = await sut.Handle(new ToggleLikeCommand(skill.Id), CancellationToken.None);
        liked.Liked.Should().BeTrue();
        liked.LikeCount.Should().Be(1);

        var unliked = await sut.Handle(new ToggleLikeCommand(skill.Id), CancellationToken.None);
        unliked.Liked.Should().BeFalse();
        unliked.LikeCount.Should().Be(0);
    }
}
