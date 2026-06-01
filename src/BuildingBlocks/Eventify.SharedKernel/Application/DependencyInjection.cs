using System.Reflection;
using Eventify.SharedKernel.Application.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Eventify.SharedKernel.Application;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMediatrWithBehavior(Assembly assembly)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            return services;
        }
    }
}
