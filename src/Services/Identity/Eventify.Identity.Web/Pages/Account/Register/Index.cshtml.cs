using Eventify.Identity.Application.User.RegisterUser;
using Eventify.SharedKernel.Application.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Eventify.Identity.Web.Pages.Account.Register;

public class Index : PageModel
{
    private readonly ICommandHandler<RegisterUserCommand, Guid> _handler;

    public Index(ICommandHandler<RegisterUserCommand, Guid> handler)
    {
        _handler = handler;
    }

    [BindProperty] public RegisterModel Model { get; set; } = default!;
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(Model.Email, Model.FirstName, Model.LastName, Model.Password);

        var result = await _handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return RedirectToPage("../Login/Index", new { ReturnUrl });
        }

        ModelState.AddModelError($"{nameof(Model)}.{result.Error.Code}", result.Error.Description);

        return Page();
    }

    public IActionResult OnGet(string? returnUrl)
    {
        ReturnUrl = returnUrl;
        return Page();
    }
}
