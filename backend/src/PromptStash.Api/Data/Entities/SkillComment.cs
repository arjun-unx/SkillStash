namespace PromptStash.Api.Data.Entities;

public sealed class SkillComment : BaseEntity, IAuditableEntity
{
    public Guid SkillId { get; set; }
    public Skill? Skill { get; set; }
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
    public required string Body { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
