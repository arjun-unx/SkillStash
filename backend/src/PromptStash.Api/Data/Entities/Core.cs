namespace PromptStash.Api.Data.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
    Guid? CreatedByUserId { get; set; }
    Guid? UpdatedByUserId { get; set; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}

public enum SkillVisibility
{
    Private = 0,
    Public = 1,
    Unlisted = 2
}
