namespace PromptStash.Api.Data.Entities;

public sealed class BookmarkCollection : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ICollection<SkillBookmark> Bookmarks { get; set; } = new List<SkillBookmark>();
}
