using Eventify.Identity.Application.User.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Eventify.Identity.Web.Pages.Account.Register;

public class Index : PageModel
{
    private readonly ISender _sender;

    public Index(ISender sender)
    {
        _sender = sender;
    }

    [BindProperty] public RegisterModel Model { get; set; } = default!;
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var result = await _sender.Send(new RegisterUserCommand(Model.Email, Model.FirstName, Model.LastName, Model.Password),
            ct);

        if (!result.IsError)
        {
            return RedirectToPage("../Login/Index", new { ReturnUrl });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError($"{nameof(Model)}.{error.Code}", error.Description);
        }

        return Page();
    }

    public IActionResult OnGet(string? returnUrl)
    {
        ReturnUrl = returnUrl;
        return Page();
    }
}
