using Eventify.SharedKernel.Options;

namespace Eventify.Identity.Infrastructure.Options;

public class ServicesOptions : IOption
{
    public static string SectionName => IOption.GetSectionName<ServicesOptions>();

    public required string Spa { get; init; }
    public required string Catalog { get; init; }
}
