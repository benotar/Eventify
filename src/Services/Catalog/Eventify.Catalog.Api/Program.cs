using Eventify.Catalog.Application;
using Eventify.Catalog.Infrastructure;
using Eventify.Catalog.Infrastructure.Persistence;
using Eventify.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// DI
builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Middleware + routing
if (app.Environment.IsDevelopment())
{
    await app.MigrateDatabaseAsync<CatalogDbContext>();
}

app.MapDefaultEndpoints();

app.Run();
