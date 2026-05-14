using System.ComponentModel.DataAnnotations;

namespace Eventify.SharedKernel.Options;

public sealed class DatabaseOptions : IOption
{
    public static string SectionName => "Database";
    [Required(AllowEmptyStrings = false)] public string ConnectionString { get; init; } = string.Empty;
}
