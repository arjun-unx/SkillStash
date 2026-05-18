namespace PromptStash.Api.Data.Entities;

/// <summary>User-authored agent skill (SKILL.md-style instructions for Claude, GPT, Gemini, Cursor, etc.).</summary>
public sealed class Skill : BaseEntity, IAuditableEntity, ISoftDeletable
{
    public required string Title { get; set; }
    public required string Body { get; set; }
    public string? Description { get; set; }
    /// <summary>Target agent: claude, openai, gemini, cursor, grok, any.</summary>
    public string AgentSlug { get; set; } = "any";
    public SkillVisibility Visibility { get; set; } = SkillVisibility.Private;
    public int CopyCount { get; set; }
    public int LikeCount { get; set; }
    public List<string> Tags { get; set; } = new();

    public Guid AuthorId { get; set; }
    public AppUser? Author { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<SkillLike> Likes { get; set; } = new List<SkillLike>();
    public ICollection<SkillBookmark> Bookmarks { get; set; } = new List<SkillBookmark>();
    public ICollection<SkillComment> Comments { get; set; } = new List<SkillComment>();
}

public sealed class SkillLike : BaseEntity, IAuditableEntity
{
    public Guid SkillId { get; set; }
    public Skill? Skill { get; set; }
    public Guid UserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
