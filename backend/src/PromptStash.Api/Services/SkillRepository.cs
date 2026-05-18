using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Services;

public interface ISkillRepository
{
    IQueryable<Skill> Query();
    Task<Skill?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Skill?> GetByIdWithLikesAsync(Guid id, CancellationToken ct);
    Task AddAsync(Skill skill, CancellationToken ct);
    void Remove(Skill skill);
    Task<bool> IsLikedByAsync(Guid skillId, Guid userId, CancellationToken ct);
    Task<SkillLike?> GetLikeAsync(Guid skillId, Guid userId, CancellationToken ct);
    Task AddLikeAsync(SkillLike like, CancellationToken ct);
    void RemoveLike(SkillLike like);
    Task SaveAsync(CancellationToken ct);
}

public sealed class SkillRepository(AppDbContext db) : ISkillRepository
{
    public IQueryable<Skill> Query() => db.Skills.AsQueryable();

    public Task<Skill?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Skills.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Skill?> GetByIdWithLikesAsync(Guid id, CancellationToken ct)
        => db.Skills.Include(p => p.Likes).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Skill skill, CancellationToken ct) => await db.Skills.AddAsync(skill, ct);

    public void Remove(Skill skill) => db.Skills.Remove(skill);

    public Task<bool> IsLikedByAsync(Guid skillId, Guid userId, CancellationToken ct)
        => db.SkillLikes.AnyAsync(l => l.SkillId == skillId && l.UserId == userId, ct);

    public Task<SkillLike?> GetLikeAsync(Guid skillId, Guid userId, CancellationToken ct)
        => db.SkillLikes.FirstOrDefaultAsync(l => l.SkillId == skillId && l.UserId == userId, ct);

    public async Task AddLikeAsync(SkillLike like, CancellationToken ct) => await db.SkillLikes.AddAsync(like, ct);

    public void RemoveLike(SkillLike like) => db.SkillLikes.Remove(like);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
