using Eventify.Identity.Domain.Entities;
using Eventify.Identity.Infrastructure.Options;
using Eventify.Identity.Infrastructure.Persistence;
using Eventify.Identity.Infrastructure.Persistence.Seed;
using Eventify.Identity.Infrastructure.Services;
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
            services.AddOptions(configuration);

            services.AddDatabase(configuration, environment);

            services.AddHostedService<IdentityServerSeeder>();
            services.AddHostedService<IdentityDataSeeder>();

            return services;
        }

        private void AddOptions(IConfiguration configuration)
        {
            services.AddOption<ServicesOptions>(configuration);
            services.AddOption<AdminCredentialsOptions>(configuration);
        }

        private void AddDatabase(IConfiguration configuration, IWebHostEnvironment environment)
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

            // Explicit UI contract the application cookie
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

            services.AddAuthorization();

            services.AddIdentityServer(options =>
                {
                    if (environment.IsDevelopment())
                    {
                        options.KeyManagement.Enabled = false;
                    }

                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.EmitStaticAudienceClaim = true;

                    // Protocol errors (bad client_id / redirect_uri) render our HTML error page
                    options.UserInteraction.ErrorUrl = "/Error";
                    options.UserInteraction.ErrorIdParameter = "errorId";
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
                .AddDeveloperSigningCredential()
                .AddAspNetIdentity<ApplicationUser>()
                .AddProfileService<IdentityProfileService>();
        }
    }
}
