using Eventify.Catalog.Api;
using Eventify.Catalog.Application;
using Eventify.Catalog.Infrastructure;
using Eventify.ServiceDefaults;
using Eventify.ServiceDefaults.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddCommonPresentation()
    .AddPresentation(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), branch => branch.UseIdempotency());

app.MapEndpoints();

app.MapScalar();

app.MapHealthChecks();

await app.RunAsync();
