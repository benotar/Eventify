using Duende.IdentityServer.EntityFramework.DbContexts;
using Eventify.Identity.Infrastructure;
using Eventify.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;

    await sp.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<ConfigurationDbContext>().Database.MigrateAsync();
    await sp.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();
}

app.UseIdentityServer();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");

app.Run();
