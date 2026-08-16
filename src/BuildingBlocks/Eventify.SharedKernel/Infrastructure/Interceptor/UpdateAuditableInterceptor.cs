using Eventify.SharedKernel.Domain;
using Eventify.SharedKernel.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Eventify.SharedKernel.Infrastructure.Interceptor;

public sealed class UpdateAuditableInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is not null)
        {
            UpdateEntities(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static void UpdateEntities(DbContext context)
    {
        var now = DateTimeOffset.UtcNow;

        var entries = context.ChangeTracker
            .Entries<IAuditable>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified || e.HasChangedOwnedEntities())
            .ToList();

        if (entries.IsEmpty)
        {
            return;
        }

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreatedAt(now);
                entry.Entity.SetUpdatedAt(now);
            }

            if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Entity.SetUpdatedAt(now);
            }
        }
    }
}