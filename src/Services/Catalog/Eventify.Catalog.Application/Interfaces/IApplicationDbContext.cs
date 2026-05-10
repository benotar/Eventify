namespace Eventify.Catalog.Application.Interfaces;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
