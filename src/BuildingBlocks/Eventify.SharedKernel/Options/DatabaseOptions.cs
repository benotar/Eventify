namespace Eventify.SharedKernel.Options;

public sealed class DatabaseOptions : IOption
{
    public static string SectionName => IOption.GetSectionName<DatabaseOptions>();
    public required string ConnectionString { get; init; } = string.Empty;
}
