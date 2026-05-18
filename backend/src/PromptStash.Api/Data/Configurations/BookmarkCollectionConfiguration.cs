using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Data.Configurations;

public sealed class BookmarkCollectionConfiguration : IEntityTypeConfiguration<BookmarkCollection>
{
    public void Configure(EntityTypeBuilder<BookmarkCollection> b)
    {
        b.ToTable("bookmark_collections");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.HasIndex(x => new { x.UserId, x.Name });
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
