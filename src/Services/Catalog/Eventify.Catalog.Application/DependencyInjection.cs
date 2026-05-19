using System.Reflection;
using Eventify.SharedKernel.Application.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Eventify.Catalog.Application;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication()
        {
            var assembly = Assembly.GetExecutingAssembly();

            services.AddValidatorsFromAssembly(assembly);

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
