using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Eventify.Identity.Api.Pages;

public class Error : PageModel
{
    private readonly IIdentityServerInteractionService _interaction;

    public Error(IIdentityServerInteractionService interaction)
    {
        _interaction = interaction;
    }

    public string? ErrorMessage { get; private set; }

    public async Task OnGet(string? errorId)
    {
        if (errorId is not null)
        {
            var context = await _interaction.GetErrorContextAsync(errorId);
            ErrorMessage = context?.ErrorDescription ?? context?.Error;
        }
    }
}
