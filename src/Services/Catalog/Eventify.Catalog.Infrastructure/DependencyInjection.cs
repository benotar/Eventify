using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Infrastructure.Persistence;
using Eventify.Catalog.Infrastructure.Repositories;
using Eventify.SharedKernel.Extensions;
using Eventify.SharedKernel.Infrastructure.Interceptor;
using Eventify.SharedKernel.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Eventify.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Options
        services.AddOption<DatabaseOptions>(configuration);

        // DbContext
        services.AddScoped<ISaveChangesInterceptor, UpdateAuditableInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, PublishDomainEventsInterceptor>();
        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            var dbConfiguration = sp
                .GetRequiredService<DatabaseOptions>();

            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(dbConfiguration.ConnectionString);
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());

        // Repositories
        services.AddScoped<IArtistRepository, ArtistRepository>();

        return services;
    }
}
