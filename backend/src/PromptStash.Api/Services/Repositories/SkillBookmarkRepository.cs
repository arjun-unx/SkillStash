using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Services.Repositories;

public interface ISkillBookmarkRepository
{
    Task<SkillBookmark?> FindAsync(Guid userId, Guid skillId, CancellationToken ct);
    Task<bool> AnyAsync(Guid userId, Guid skillId, CancellationToken ct);
    Task<int> CountForSkillAsync(Guid skillId, CancellationToken ct);
    Task AddAsync(SkillBookmark bookmark, CancellationToken ct);
    void Remove(SkillBookmark bookmark);
    Task SaveAsync(CancellationToken ct);
}

public sealed class SkillBookmarkRepository(AppDbContext db) : ISkillBookmarkRepository
{
    public Task<SkillBookmark?> FindAsync(Guid userId, Guid skillId, CancellationToken ct)
        => db.SkillBookmarks.FirstOrDefaultAsync(b => b.UserId == userId && b.SkillId == skillId, ct);

    public Task<bool> AnyAsync(Guid userId, Guid skillId, CancellationToken ct)
        => db.SkillBookmarks.AnyAsync(b => b.UserId == userId && b.SkillId == skillId, ct);

    public Task<int> CountForSkillAsync(Guid skillId, CancellationToken ct)
        => db.SkillBookmarks.CountAsync(b => b.SkillId == skillId, ct);

    public async Task AddAsync(SkillBookmark bookmark, CancellationToken ct)
        => await db.SkillBookmarks.AddAsync(bookmark, ct);

    public void Remove(SkillBookmark bookmark) => db.SkillBookmarks.Remove(bookmark);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
