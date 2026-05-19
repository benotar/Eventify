using Eventify.SharedKernel.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Eventify.SharedKernel.Extensions;

public static class OptionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOption<TOption>(IConfiguration configuration, bool isAddSingleton = false)
            where TOption : class, IOption
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(TOption.SectionName);

            services.AddOptions<TOption>()
                .Bind(configuration.GetSection(TOption.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            if (isAddSingleton)
            {
                services.AddSingleton(sp => sp.GetRequiredService<IOptions<TOption>>().Value);
            }

            return services;
        }

        public TOption AddOption<TOption>(IConfiguration configuration, out TOption option)
            where TOption : class, IOption
        {
            services.AddOption<TOption>(configuration);

            option = configuration.GetSection(TOption.SectionName).Get<TOption>()!;

            return option;
        }
    }
}
