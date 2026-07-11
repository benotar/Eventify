using System.ComponentModel.DataAnnotations;
using Eventify.ServiceDefaults.Resources;
using Eventify.SharedKernel;

namespace Eventify.Identity.Api.Pages.Account.Login;

public class LoginModel
{
    [Display(Name = nameof(Captions.Email), ResourceType = typeof(Captions))]
    [Required(ErrorMessageResourceType = typeof(Captions), ErrorMessageResourceName = nameof(Captions.RequiredValidation))]
    [EmailAddress(ErrorMessageResourceType = typeof(Captions),
        ErrorMessageResourceName = nameof(Captions.EmailAddressValidation))]
    [MaxLength(SharedConstants.MaxEmailLength,
        ErrorMessageResourceType = typeof(Captions), ErrorMessageResourceName = nameof(Captions.MaxLengthValidation))]
    public string Email { get; set; } = string.Empty;

    [Display(Name = nameof(Captions.Password), ResourceType = typeof(Captions))]
    [Required(ErrorMessageResourceType = typeof(Captions), ErrorMessageResourceName = nameof(Captions.RequiredValidation))]
    [MinLength(SharedConstants.MinPasswordLength, ErrorMessageResourceType = typeof(Captions),
        ErrorMessageResourceName = nameof(Captions.MinLengthValidaion))]
    [MaxLength(SharedConstants.MaxPasswordLength, ErrorMessageResourceType = typeof(Captions),
        ErrorMessageResourceName = nameof(Captions.MaxLengthValidation))]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
