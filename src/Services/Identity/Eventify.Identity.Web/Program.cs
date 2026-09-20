using Eventify.Identity.Application;
using Eventify.Identity.Infrastructure;
using Eventify.ServiceDefaults;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// DI
builder.Services.AddCommonPresentation();

builder.Services.Configure<RequestLocalizationOptions>(options => options.ConfigureLocalizationOptions());

// Browser requests that throw are re-executed onto the HTML /Error page (see GlobalExceptionHandler).
builder.Services.Configure<ExceptionHandlerOptions>(options => options.ExceptionHandlingPath = "/Error");

builder.Services.AddRazorPages()
    .AddMvcOptions(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseWhen(ctx => ctx.Request.Headers.Accept.ToString().Contains("text/html"),
    htmlBranch => htmlBranch.UseExceptionHandler("/Error"));

app.UseExceptionHandler();

app.MapEndpoints();

app.UseStaticFiles();

app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");

app.UseRouting();

app.UseRequestLocalization(app.Services.GetService<IOptions<RequestLocalizationOptions>>()!.Value);

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/culture/set", (HttpContext ctx, string culture, string redirectUri) =>
    ctx.ActionSetCulture(culture, redirectUri));

app.Run();
