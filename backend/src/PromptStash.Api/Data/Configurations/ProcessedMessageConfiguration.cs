using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Data.Configurations;

public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> b)
    {
        b.ToTable("processed_messages");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.MessageId).IsUnique();
        b.Property(x => x.EventName).HasMaxLength(120).IsRequired();
    }
}
