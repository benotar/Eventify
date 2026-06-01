using System.Security.Claims;
using Duende.IdentityModel;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Eventify.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Eventify.Identity.Infrastructure.Services;

public sealed class IdentityProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityProfileService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var userId = context.Subject.GetSubjectId();

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return;
        }

        List<Claim> claims =
        [
            new Claim(JwtClaimTypes.GivenName, user.FirstName),
            new Claim(JwtClaimTypes.FamilyName, user.LastName),
            ..(await _userManager.GetRolesAsync(user))
            .Select(role => new Claim(JwtClaimTypes.Role, role))
        ];

        context.AddRequestedClaims(claims);
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var userId = context.Subject.GetSubjectId();

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null || await _userManager.IsLockedOutAsync(user))
        {
            context.IsActive = false;
            return;
        }

        context.IsActive = true;
    }
}
