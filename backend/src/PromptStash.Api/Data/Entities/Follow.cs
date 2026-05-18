namespace PromptStash.Api.Data.Entities;

public sealed class Follow : BaseEntity, IAuditableEntity
{
    public Guid FollowerId { get; set; }
    public AppUser? Follower { get; set; }

    public Guid FolloweeId { get; set; }
    public AppUser? Followee { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
