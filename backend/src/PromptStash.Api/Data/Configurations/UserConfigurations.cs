using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Data.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.UserName).HasMaxLength(40).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(80).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
        b.Property(x => x.Bio).HasMaxLength(280);
        b.Property(x => x.Headline).HasMaxLength(120);
        b.Property(x => x.AvatarUrl).HasMaxLength(512);
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.UserName).IsUnique();
    }
}

public sealed class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> b)
    {
        b.ToTable("follows");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.FollowerId, x.FolloweeId }).IsUnique();

        b.HasOne(x => x.Follower)
            .WithMany()
            .HasForeignKey(x => x.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Followee)
            .WithMany()
            .HasForeignKey(x => x.FolloweeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
