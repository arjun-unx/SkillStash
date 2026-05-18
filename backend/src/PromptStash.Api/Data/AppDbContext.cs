using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillLike> SkillLikes => Set<SkillLike>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<SkillComment> SkillComments => Set<SkillComment>();
    public DbSet<SkillBookmark> SkillBookmarks => Set<SkillBookmark>();
    public DbSet<BookmarkCollection> BookmarkCollections => Set<BookmarkCollection>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<TrendingSkill> TrendingSkills => Set<TrendingSkill>();
    public DbSet<TrendingSkillBookmark> TrendingSkillBookmarks => Set<TrendingSkillBookmark>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}
