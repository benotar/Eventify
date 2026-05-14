using Eventify.SharedKernel.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Eventify.SharedKernel.Extensions;

public static class OptionExtensions
{
    public static IServiceCollection AddOption<TOption>(this IServiceCollection services, IConfiguration configuration)
        where TOption : class, IOption
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(TOption.SectionName);

        services.AddOptions<TOption>()
            .Bind(configuration.GetSection(TOption.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TOption>>().Value);

        return services;
    }
}
