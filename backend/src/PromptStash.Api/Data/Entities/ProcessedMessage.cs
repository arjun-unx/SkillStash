namespace PromptStash.Api.Data.Entities;

public sealed class ProcessedMessage : BaseEntity
{
    public Guid MessageId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
}
