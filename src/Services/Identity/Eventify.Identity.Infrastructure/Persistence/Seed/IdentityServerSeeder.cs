using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Eventify.Identity.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Eventify.Identity.Infrastructure.Persistence.Seed;

public sealed class IdentityServerSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServicesOptions _serviceOptions;

    public IdentityServerSeeder(IServiceScopeFactory scopeFactory, ServicesOptions serviceOptions)
    {
        _scopeFactory = scopeFactory;
        _serviceOptions = serviceOptions;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

        await SeedIdentityResources(dbContext, ct);
        await SeedApiScopes(dbContext, ct);
        await SeedApiResources(dbContext, ct);
        await SeedClients(dbContext, ct);
    }

    private static async Task SeedIdentityResources(ConfigurationDbContext context, CancellationToken ct)
    {
        if (await context.IdentityResources.AnyAsync(ct))
        {
            return;
        }

        await context.AddRangeAsync(SeedData.GetIdentityResources().Select(resource => resource.ToEntity()), ct);

        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedApiScopes(ConfigurationDbContext context, CancellationToken ct)
    {
        if (await context.ApiScopes.AnyAsync(ct))
        {
            return;
        }

        await context.AddRangeAsync(SeedData.GetApiScopes().Select(scope => scope.ToEntity()), ct);

        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedApiResources(ConfigurationDbContext context, CancellationToken ct)
    {
        if (await context.ApiResources.AnyAsync(ct))
        {
            return;
        }

        await context.AddRangeAsync(SeedData.GetApiResources().Select(resource => resource.ToEntity()), ct);

        await context.SaveChangesAsync(ct);
    }

    private async Task SeedClients(ConfigurationDbContext context, CancellationToken ct)
    {
        if (await context.Clients.AnyAsync(ct))
        {
            return;
        }

        await context.AddRangeAsync(SeedData.GetClients(_serviceOptions).Select(client => client.ToEntity()), ct);

        await context.SaveChangesAsync(ct);
    }

    public Task StopAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
