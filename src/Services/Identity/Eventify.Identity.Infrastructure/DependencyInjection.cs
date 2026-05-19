using Eventify.Identity.Domain.Entities;
using Eventify.Identity.Infrastructure.Persistence;
using Eventify.SharedKernel.Extensions;
using Eventify.SharedKernel.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Eventify.Identity.Infrastructure;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration, IWebHostEnvironment environment)
        {
            services.AddOption<DatabaseOptions>(configuration, out var dbOption);

            var migrationsAssembly = typeof(DependencyInjection).Assembly.GetName().Name;

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(dbOption.ConnectionString);
            });

            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireUppercase = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthorization();

            var identityServerBuilder = services.AddIdentityServer(options =>
                {
                    if (environment.IsDevelopment())
                    {
                        options.KeyManagement.Enabled = false;
                    }

                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.EmitStaticAudienceClaim = true;
                })
                .AddConfigurationStore((options) =>
                {
                    options.ConfigureDbContext = builder =>
                        builder.UseNpgsql(dbOption.ConnectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                        builder.UseNpgsql(dbOption.ConnectionString, sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                .AddAspNetIdentity<ApplicationUser>();

            if (environment.IsDevelopment())
            {
                identityServerBuilder.AddDeveloperSigningCredential();
            }

            return services;
        }
    }
}
