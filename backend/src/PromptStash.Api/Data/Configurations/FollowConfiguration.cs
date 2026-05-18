using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Data.Configurations;

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
