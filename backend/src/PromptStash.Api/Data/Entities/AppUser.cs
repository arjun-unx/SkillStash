namespace PromptStash.Api.Data.Entities;

public sealed class AppUser : BaseEntity, IAuditableEntity
{
    public required string Email { get; set; }
    public required string UserName { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public string? Bio { get; set; }
    /// <summary>Optional title/role shown on prompt cards (e.g. “Staff Engineer”).</summary>
    public string? Headline { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool EmailNotificationsEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
