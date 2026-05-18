namespace PromptStash.Api.Data.Entities;

public sealed class SkillBookmark : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
    public Guid SkillId { get; set; }
    public Skill? Skill { get; set; }
    public Guid? CollectionId { get; set; }
    public BookmarkCollection? Collection { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
