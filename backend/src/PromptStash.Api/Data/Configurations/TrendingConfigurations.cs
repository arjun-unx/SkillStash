using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Data.Configurations;

public sealed class TrendingSkillConfiguration : IEntityTypeConfiguration<TrendingSkill>
{
    public void Configure(EntityTypeBuilder<TrendingSkill> b)
    {
        b.ToTable("trending_skills");
        b.HasKey(x => x.Id);
        b.Property(x => x.ExternalKey).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.ExternalKey).IsUnique();
        b.Property(x => x.ProviderSlug).HasMaxLength(32).IsRequired();
        b.HasIndex(x => x.ProviderSlug);
        b.Property(x => x.SourceName).HasMaxLength(120).IsRequired();
        b.Property(x => x.SourceUrl).HasMaxLength(512).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Body).HasColumnType("text").IsRequired();
        b.Property(x => x.Snippet).HasMaxLength(400);
        b.Property(x => x.RoleCategory).HasMaxLength(80).IsRequired();
        b.Property(x => x.Category).HasMaxLength(60);
        b.Property(x => x.Tags).HasColumnType("text[]");
        b.HasIndex(x => x.TrendingScore);
        b.HasIndex(x => x.SyncedAtUtc);
        b.HasIndex(x => x.RoleCategory);
    }
}

public sealed class TrendingSkillBookmarkConfiguration : IEntityTypeConfiguration<TrendingSkillBookmark>
{
    public void Configure(EntityTypeBuilder<TrendingSkillBookmark> b)
    {
        b.ToTable("trending_skill_bookmarks");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.TrendingSkillId }).IsUnique();
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.TrendingSkill).WithMany(p => p.Bookmarks).HasForeignKey(x => x.TrendingSkillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
