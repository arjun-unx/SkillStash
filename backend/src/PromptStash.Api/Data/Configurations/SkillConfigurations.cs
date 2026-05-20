using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Data.Configurations;

public sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> b)
    {
        b.ToTable("skills");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(120).IsRequired();
        b.Property(x => x.Body).HasColumnType("text").IsRequired();
        b.Property(x => x.Description).HasMaxLength(280);
        b.Property(x => x.AgentSlug).HasMaxLength(32).HasDefaultValue("any");
        b.Property(x => x.Visibility).HasConversion<int>();
        b.Property(x => x.Tags).HasColumnType("text[]");

        b.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.AuthorId);
        b.HasIndex(x => x.Visibility);
        b.HasIndex(x => x.CreatedAtUtc);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class SkillLikeConfiguration : IEntityTypeConfiguration<SkillLike>
{
    public void Configure(EntityTypeBuilder<SkillLike> b)
    {
        b.ToTable("skill_likes");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.SkillId, x.UserId }).IsUnique();
        b.HasOne(x => x.Skill).WithMany(p => p.Likes).HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SkillCommentConfiguration : IEntityTypeConfiguration<SkillComment>
{
    public void Configure(EntityTypeBuilder<SkillComment> b)
    {
        b.ToTable("skill_comments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        b.HasIndex(x => x.SkillId);
        b.HasOne(x => x.Skill).WithMany(p => p.Comments).HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SkillBookmarkConfiguration : IEntityTypeConfiguration<SkillBookmark>
{
    public void Configure(EntityTypeBuilder<SkillBookmark> b)
    {
        b.ToTable("skill_bookmarks");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.SkillId }).IsUnique();
        b.HasOne(x => x.Skill).WithMany(p => p.Bookmarks).HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Collection).WithMany(c => c.Bookmarks).HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.SetNull);
    }
}
