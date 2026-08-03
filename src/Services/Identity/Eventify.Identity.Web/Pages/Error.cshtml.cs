using Duende.IdentityServer.Services;
using Eventify.Identity.Infrastructure.Options;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Eventify.Identity.Web.Pages;

public class Error : PageModel
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly ServicesOptions _servicesOptions;

    public string? GoHomeUrl { get; set; }

    public Error(IIdentityServerInteractionService interaction, ServicesOptions servicesOptions)
    {
        _interaction = interaction;
        _servicesOptions = servicesOptions;
    }

    public string? ErrorMessage { get; private set; }

    public async Task OnGet(string? errorId)
    {
        GoHomeUrl = _servicesOptions.Spa;

        if (errorId is not null)
        {
            var context = await _interaction.GetErrorContextAsync(errorId);
            ErrorMessage = context?.ErrorDescription ?? context?.Error;
        }
    }
}
