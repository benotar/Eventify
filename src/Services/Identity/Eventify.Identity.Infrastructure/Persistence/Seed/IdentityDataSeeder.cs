using Eventify.Identity.Domain.Entities;
using Eventify.Identity.Infrastructure.Options;
using Eventify.SharedKernel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Eventify.Identity.Infrastructure.Persistence.Seed;

public sealed class IdentityDataSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AdminCredentialsOptions _adminCredentials;

    public IdentityDataSeeder(IServiceScopeFactory scopeFactory, AdminCredentialsOptions adminCredentials)
    {
        _scopeFactory = scopeFactory;
        _adminCredentials = adminCredentials;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        string[] roles = [Constants.Admin, Constants.Customer];

        foreach (var role in roles)
        {
            if (await roleManager.FindByNameAsync(role) is not null)
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create {role} role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

    private async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        if (await userManager.FindByEmailAsync(_adminCredentials.Email) is not null)
        {
            return;
        }

        var admin = new ApplicationUser(_adminCredentials.Email, _adminCredentials.FirstName, _adminCredentials.LastName);

        var createUserResult = await userManager.CreateAsync(admin, _adminCredentials.Password);

        if (!createUserResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create admin user: {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
        }

        var addToRoleResult = await userManager.AddToRoleAsync(admin, Constants.Admin);

        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to add admin role to user: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
        }
    }

    public Task StopAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
