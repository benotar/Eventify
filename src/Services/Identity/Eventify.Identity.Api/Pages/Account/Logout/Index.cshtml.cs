using Duende.IdentityServer.Services;
using Eventify.Identity.Domain.Entities;
using Eventify.SharedKernel.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Eventify.Identity.Api.Pages.Account.Logout;

public class Index : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityServerInteractionService _interaction;

    [BindProperty(SupportsGet = true)] public string? LogoutId { get; set; }

    public Index(SignInManager<ApplicationUser> signInManager, IIdentityServerInteractionService interaction)
    {
        _signInManager = signInManager;
        _interaction = interaction;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();

        var context = await _interaction.GetLogoutContextAsync(LogoutId);

        var postLogoutUri = context.PostLogoutRedirectUri;

        return Redirect(postLogoutUri!.IsNotEmpty ? postLogoutUri : "/");
    }

    public IActionResult OnGet(string? logoutId)
    {
        LogoutId = logoutId;
        return Page();
    }
}
