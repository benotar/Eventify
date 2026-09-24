using System.ComponentModel.DataAnnotations;

namespace Eventify.SharedKernel.Options;

public sealed class RedisOptions : IOption
{
    public static string SectionName { get; } = IOption.GetSectionName<RedisOptions>();

    [Required(AllowEmptyStrings = false)] public string ConnectionString { get; init; } = string.Empty;
}
