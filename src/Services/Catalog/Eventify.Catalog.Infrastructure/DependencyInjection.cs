using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Infrastructure.Persistence;
using Eventify.Catalog.Infrastructure.Time;
using Eventify.SharedKernel.Application;
using Eventify.SharedKernel.Extensions;
using Eventify.SharedKernel.Infrastructure;
using Eventify.SharedKernel.Infrastructure.DomainEvents;
using Eventify.SharedKernel.Infrastructure.Interceptor;
using Eventify.SharedKernel.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Eventify.Catalog.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            services.AddSingleton<IDateTimeOffsetProvider, DateTimeOffsetProvider>();
            services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

            services.AddOption<DatabaseOptions>(configuration, out var dbOption);

            services.AddScoped<ISaveChangesInterceptor, UpdateAuditableInterceptor>();
            services.AddScoped<ISaveChangesInterceptor, PublishDomainEventsInterceptor>();

            services.AddDbContext<CatalogDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseNpgsql(dbOption.ConnectionString);
            });

            services.AddScoped<IArtistDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());
            services.AddScoped<IVenueDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());

            return services;
        }
    }
}
