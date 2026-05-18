namespace PromptStash.Api.Common.Events;

public abstract class IntegrationEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public abstract string EventName { get; }
}

public sealed class UserRegisteredIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;

    public override string EventName => "user.registered.v1";
}

public sealed class SkillPublishedIntegrationEvent : IntegrationEvent
{
    public Guid SkillId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid AuthorId { get; init; }
    public string AuthorEmail { get; init; } = string.Empty;
    public string AuthorDisplayName { get; init; } = string.Empty;

    public override string EventName => "skill.published.v1";
}
