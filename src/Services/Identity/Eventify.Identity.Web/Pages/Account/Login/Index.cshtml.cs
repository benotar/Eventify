using Duende.IdentityServer.Services;
using Eventify.Identity.Domain.Entities;
using Eventify.ServiceDefaults.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Eventify.Identity.Web.Pages.Account.Login;

public class Index : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityServerInteractionService _interaction;

    [BindProperty] public LoginModel Model { get; set; } = default!;

    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public Index(SignInManager<ApplicationUser> signInManager, IIdentityServerInteractionService interaction)
    {
        _signInManager = signInManager;
        _interaction = interaction;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(Model.Email, Model.Password, isPersistent: false,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, Captions.AccountLockedOut);
            return Page();
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, Captions.InvalidLogin);
            return Page();
        }

        var context = await _interaction.GetAuthorizationContextAsync(ReturnUrl);

        if (context != null || Url.IsLocalUrl(ReturnUrl))
            return Redirect(ReturnUrl!);

        return Redirect("/");
    }

    public IActionResult OnGet(string? returnUrl)
    {
        ReturnUrl = returnUrl;
        return Page();
    }
}
