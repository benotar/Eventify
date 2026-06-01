using Eventify.SharedKernel.Options;

namespace Eventify.Identity.Infrastructure.Options;

public class AdminCredentialsOptions : IOption
{
    public static string SectionName => IOption.GetSectionName<AdminCredentialsOptions>();

    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
}
