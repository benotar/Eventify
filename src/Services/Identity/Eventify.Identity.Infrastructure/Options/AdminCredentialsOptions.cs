using System.ComponentModel.DataAnnotations;
using Eventify.SharedKernel.Options;

namespace Eventify.Identity.Infrastructure.Options;

public class AdminCredentialsOptions : IOption
{
    public static string SectionName => IOption.GetSectionName<AdminCredentialsOptions>();

    [Required(AllowEmptyStrings = false)] public string FirstName { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)] public string LastName { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)] public string Email { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)] public string Password { get; init; } = string.Empty;
}
