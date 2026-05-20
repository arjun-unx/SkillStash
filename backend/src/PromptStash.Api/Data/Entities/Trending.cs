namespace PromptStash.Api.Data.Entities;

/// <summary>Agent skill ingested from a public GitHub repository (SKILL.md).</summary>
public sealed class TrendingSkill : BaseEntity
{
    public required string ExternalKey { get; set; }
    public required string ProviderSlug { get; set; }
    public required string SourceName { get; set; }
    public required string SourceUrl { get; set; }

    public required string Title { get; set; }
    public required string Body { get; set; }
    public string? Snippet { get; set; }

    public required string RoleCategory { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();

    public int TrendingScore { get; set; }
    public int UseCount { get; set; }
    public int SaveCount { get; set; }
    public double Rating { get; set; }

    public DateTime? SourceUpdatedAtUtc { get; set; }
    public DateTime SyncedAtUtc { get; set; }

    public ICollection<TrendingSkillBookmark> Bookmarks { get; set; } = new List<TrendingSkillBookmark>();
}

public sealed class TrendingSkillBookmark : BaseEntity, IAuditableEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
    public Guid TrendingSkillId { get; set; }
    public TrendingSkill? TrendingSkill { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
