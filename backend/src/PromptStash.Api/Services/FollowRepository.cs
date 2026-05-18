using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Services;

public interface IFollowRepository
{
    Task<Follow?> FindAsync(Guid followerId, Guid followeeId, CancellationToken ct);
    Task<int> CountFollowersAsync(Guid userId, CancellationToken ct);
    Task<int> CountFollowingAsync(Guid userId, CancellationToken ct);
    Task AddAsync(Follow follow, CancellationToken ct);
    void Remove(Follow follow);
    Task<bool> IsFollowingAsync(Guid followerId, Guid followeeId, CancellationToken ct);
    Task<HashSet<Guid>> GetFolloweeIdsAsync(Guid followerId, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public sealed class FollowRepository(AppDbContext db) : IFollowRepository
{
    public Task<Follow?> FindAsync(Guid followerId, Guid followeeId, CancellationToken ct)
        => db.Follows.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, ct);

    public Task<int> CountFollowersAsync(Guid userId, CancellationToken ct)
        => db.Follows.CountAsync(f => f.FolloweeId == userId, ct);

    public Task<int> CountFollowingAsync(Guid userId, CancellationToken ct)
        => db.Follows.CountAsync(f => f.FollowerId == userId, ct);

    public async Task AddAsync(Follow follow, CancellationToken ct) => await db.Follows.AddAsync(follow, ct);

    public void Remove(Follow follow) => db.Follows.Remove(follow);

    public Task<bool> IsFollowingAsync(Guid followerId, Guid followeeId, CancellationToken ct)
        => db.Follows.AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, ct);

    public async Task<HashSet<Guid>> GetFolloweeIdsAsync(Guid followerId, CancellationToken ct)
    {
        var ids = await db.Follows.AsNoTracking()
            .Where(f => f.FollowerId == followerId)
            .Select(f => f.FolloweeId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
