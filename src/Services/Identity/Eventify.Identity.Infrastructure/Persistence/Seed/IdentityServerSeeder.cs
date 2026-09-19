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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

        await SeedIdentityResources(dbContext,cancellationToken);
        await SeedApiScopes(dbContext,cancellationToken);
        await SeedApiResources(dbContext,cancellationToken);
        await SeedClients(dbContext, cancellationToken);
    }

    private static async Task SeedIdentityResources(ConfigurationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.IdentityResources.AnyAsync(cancellationToken))
        {
            return;
        }

        await context.AddRangeAsync(SeedData.GetIdentityResources().Select(resource => resource.ToEntity()), cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedApiScopes(ConfigurationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.ApiScopes.AnyAsync(cancellationToken))
        {
            return;
        }

        await context.AddRangeAsync(SeedData.GetApiScopes().Select(scope => scope.ToEntity()), cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedApiResources(ConfigurationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.ApiResources.AnyAsync(cancellationToken))
        {
            return;
        }

        await context.AddRangeAsync(SeedData.GetApiResources().Select(resource => resource.ToEntity()), cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedClients(ConfigurationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Clients.AnyAsync(cancellationToken))
        {
            return;
        }

        await context.AddRangeAsync(SeedData.GetClients(_serviceOptions).Select(client => client.ToEntity()), cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
