using System.ComponentModel.DataAnnotations;
using Eventify.SharedKernel.Options;

namespace Eventify.Identity.Infrastructure.Options;

public class ServicesOptions : IOption
{
    public static string SectionName => IOption.GetSectionName<ServicesOptions>();

    [Required(AllowEmptyStrings = false)] public string Spa { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)] public string Catalog { get; init; } = string.Empty;
}
