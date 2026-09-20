using System.Globalization;
using System.Reflection;
using Asp.Versioning;
using Carter;
using Eventify.ServiceDefaults.Middleware;
using Eventify.SharedKernel.Languages;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace Eventify.ServiceDefaults;

public static class Extensions
{
    public static IServiceCollection AddCommonPresentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi();

        services.AddCarter(new DependencyContextAssemblyCatalog([Assembly.GetEntryAssembly()!]));

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddHealthChecks();

        services.AddApiVersioning(options =>
            {
                // https://dotnet.github.io/aspnet-api-versioning/diagnostic/av0011.html
                //options.DefaultApiVersion = ApiVersion.Default;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
                //options.AssumeDefaultVersionWhenUnspecified = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }

    extension(WebApplication app)
    {
        public void MapScalar()
        {
            app.MapOpenApi();

            app.MapScalarApiReference();
        }

        public void MapHealthChecks()
        {
            app.MapHealthChecks("health",
                new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
        }

        public void MapEndpoints()
        {
            var apiVersionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1))
                .ReportApiVersions()
                .Build();

            var versionedGroup = app
                .MapGroup("api/v{version:apiVersion}")
                .WithApiVersionSet(apiVersionSet);

            versionedGroup.MapCarter();
        }
    }

    extension(RequestLocalizationOptions localizationOptions)
    {
        public void ConfigureLocalizationOptions()
        {
            var supportedCultures = CultureManager.CultureNames.Select(item => new CultureInfo(item.Value))
                .ToList();

            localizationOptions.DefaultRequestCulture = new RequestCulture(CultureManager.CultureNames[Languages.En]);
            localizationOptions.SupportedCultures = supportedCultures;
            localizationOptions.SupportedUICultures = supportedCultures;
        }
    }

    extension(HttpContext ctx)
    {
        public IResult ActionSetCulture(string? culture, string redirectUri)
        {
            if (culture != null)
            {
                ctx.Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, culture)));
            }

            return Results.Redirect(redirectUri);
        }
    }
}
