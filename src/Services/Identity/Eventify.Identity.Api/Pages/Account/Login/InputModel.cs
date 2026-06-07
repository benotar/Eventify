using System.ComponentModel.DataAnnotations;
using Eventify.SharedKernel;

namespace Eventify.Identity.Api.Pages.Account.Login;

public class InputModel
{
    [Required]
    [EmailAddress]
    [MaxLength(SharedConstants.MaxEmailLength)]
    public string Email { get; set; } = default!;

    [Required]
    [MinLength(SharedConstants.MinPasswordLength)]
    [MaxLength(SharedConstants.MaxPasswordLength)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = default!;
}
