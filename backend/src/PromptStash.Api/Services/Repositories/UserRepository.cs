using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Services.Repositories;

public interface IUserRepository
{
    Task<bool> ExistsByEmailOrUserNameAsync(string email, string userName, CancellationToken ct);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct);
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<AppUser?> GetByUserNameAsync(string userName, CancellationToken ct);
    Task AddAsync(AppUser user, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<bool> ExistsByEmailOrUserNameAsync(string email, string userName, CancellationToken ct)
        => db.Users.AnyAsync(u => u.Email == email || u.UserName == userName, ct);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct)
        => db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<AppUser?> GetByUserNameAsync(string userName, CancellationToken ct)
        => db.Users.FirstOrDefaultAsync(u => u.UserName == userName, ct);

    public async Task AddAsync(AppUser user, CancellationToken ct)
    {
        await db.Users.AddAsync(user, ct);
    }

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
