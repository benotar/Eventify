using Eventify.ServiceDefaults.Middleware;
using Eventify.SharedKernel.Extensions;
using Eventify.SharedKernel.Options;

namespace Eventify.Catalog.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOption<IdempotencyOptions>(configuration);

        services.AddScoped<IdempotencyMiddleware>();

        return services;
    }
}
