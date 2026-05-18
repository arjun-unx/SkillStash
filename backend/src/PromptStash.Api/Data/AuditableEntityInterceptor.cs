using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Data;

public sealed class AuditableEntityInterceptor(
    ICurrentUserService currentUser,
    IDateTimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context is null) return;
        var now = clock.UtcNow;
        var userId = currentUser.UserId;

        foreach (EntityEntry<IAuditableEntity> entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAtUtc == default) entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedByUserId ??= userId;
            }
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedByUserId = userId;
            }
        }
    }
}
