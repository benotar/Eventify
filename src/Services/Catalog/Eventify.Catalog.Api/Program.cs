using Eventify.Catalog.Application;
using Eventify.Catalog.Infrastructure;
using Eventify.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddApplication()
    .AddCommonPresentation()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapEndpoints();

app.MapScalar();

app.MapHealthChecks();

app.UseExceptionHandler();

await app.RunAsync();
