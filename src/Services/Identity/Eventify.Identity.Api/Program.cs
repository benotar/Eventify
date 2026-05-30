using Duende.IdentityServer.EntityFramework.DbContexts;
using Eventify.Identity.Application;
using Eventify.Identity.Infrastructure;
using Eventify.Identity.Infrastructure.Persistence;
using Eventify.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// DI
builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

// Middleware + routing
if (app.Environment.IsDevelopment())
{
    await app.MigrateDatabaseAsync<ApplicationDbContext>();
    await app.MigrateDatabaseAsync<ConfigurationDbContext>();
    await app.MigrateDatabaseAsync<PersistedGrantDbContext>();
}

app.MapDefaultEndpoints();

app.UseIdentityServer();

app.UseAuthentication();

app.UseAuthorization();

app.Run();
