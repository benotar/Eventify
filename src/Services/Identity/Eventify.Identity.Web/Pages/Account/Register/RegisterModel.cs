using System.ComponentModel.DataAnnotations;
using Eventify.Localization;

namespace Eventify.Identity.Web.Pages.Account.Register;

public class RegisterModel
{
    [Display(Name = nameof(Captions.Email), ResourceType = typeof(Captions))]
    public string Email { get; set; } = string.Empty;

    [Display(Name = nameof(Captions.FirstName), ResourceType = typeof(Captions))]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = nameof(Captions.LastName), ResourceType = typeof(Captions))]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = nameof(Captions.Password), ResourceType = typeof(Captions))]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
