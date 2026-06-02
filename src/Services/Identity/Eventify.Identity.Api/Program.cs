using Duende.IdentityServer.EntityFramework.DbContexts;
using Eventify.Identity.Application;
using Eventify.Identity.Infrastructure;
using Eventify.Identity.Infrastructure.Persistence;
using Eventify.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// DI
builder.AddServiceDefaults();

builder.Services.AddRazorPages();

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

app.UseStaticFiles(); // TODO Remove if there are not any static files
app.UseRouting();

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
