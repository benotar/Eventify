using Duende.IdentityServer.Services;
using Eventify.Identity.Domain.Entities;
using Eventify.Identity.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Eventify.Identity.Web.Pages.Account.Logout;

public class Index : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly ServicesOptions _servicesOptions;

    [BindProperty(SupportsGet = true)] public string? LogoutId { get; set; }
    public string? CancelUrl { get; private set; }

    public Index(SignInManager<ApplicationUser> signInManager, IIdentityServerInteractionService interaction,
        ServicesOptions servicesOptions)
    {
        _signInManager = signInManager;
        _interaction = interaction;
        _servicesOptions = servicesOptions;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();

        var context = await _interaction.GetLogoutContextAsync(LogoutId);

        var postLogoutUri = context.PostLogoutRedirectUri;

        return Redirect(postLogoutUri ?? _servicesOptions.Spa);
    }

    public async Task<IActionResult> OnGetAsync(string? logoutId)
    {
        LogoutId = logoutId;

        var context = await _interaction.GetLogoutContextAsync(logoutId);

        CancelUrl = context.PostLogoutRedirectUri ?? _servicesOptions.Spa;

        return Page();
    }
}
