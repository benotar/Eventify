using EFCore.ComplexIndexes.SqlServer;
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
            return services.AddServices(configuration)
                .AddDatabase(configuration);
        }

        private IServiceCollection AddServices(IConfiguration configuration)
        {
            return services.AddProviders(configuration)
                .AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>()
                .AddInterceptors(configuration);
        }

        private IServiceCollection AddProviders(IConfiguration configuration)
        {
            return services.AddSingleton<IDateTimeOffsetProvider, DateTimeOffsetProvider>();
        }

        private IServiceCollection AddInterceptors(IConfiguration configuration)
        {
            return services.AddScoped<ISaveChangesInterceptor, UpdateAuditableInterceptor>()
                .AddScoped<ISaveChangesInterceptor, PublishDomainEventsInterceptor>();
        }

        private IServiceCollection AddDatabase(IConfiguration configuration)
        {
            services.AddOption<DatabaseOptions>(configuration, out var dbOption);
            services.AddDbContext<CatalogDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseSqlServer(dbOption.ConnectionString).UseSqlServerComplexIndexes();
            });

            services.AddScoped<IArtistDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());
            services.AddScoped<IVenueDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());

            return services;
        }
    }
}
