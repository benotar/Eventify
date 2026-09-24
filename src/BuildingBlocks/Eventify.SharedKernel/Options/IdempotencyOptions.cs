using System.ComponentModel.DataAnnotations;

namespace Eventify.SharedKernel.Options;

public sealed class IdempotencyOptions : IOption
{
    public static string SectionName { get; } = IOption.GetSectionName<IdempotencyOptions>();

    [Required(AllowEmptyStrings = false)] public string HeaderName { get; init; } = string.Empty;

    public TimeSpan Ttl { get; init; }

    public string[] Methods { get; init; } = [];
}