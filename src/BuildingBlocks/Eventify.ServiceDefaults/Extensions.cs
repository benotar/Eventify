using System.Globalization;
using System.Reflection;
using Asp.Versioning;
using Carter;
using Eventify.ServiceDefaults.Middleware;
using Eventify.SharedKernel.Languages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Eventify.ServiceDefaults;

public static class Extensions
{
    public static void AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddOpenApi();

        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        builder.Services.AddCarter(new DependencyContextAssemblyCatalog([Assembly.GetEntryAssembly()!]));
    }

    extension(WebApplication app)
    {
        public void MapDefaultEndpoints()
        {
            app.UseExceptionHandler();

            app.MapOpenApi();

            app.MapScalarApiReference();

            app.MapCarter();
        }

        public async Task MigrateDatabaseAsync<TContext>() where TContext : DbContext
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();
            await context.Database.MigrateAsync();
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
