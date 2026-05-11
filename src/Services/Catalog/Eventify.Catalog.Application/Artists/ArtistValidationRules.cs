using Eventify.SharedKernel.Extensions;
using FluentValidation;

namespace Eventify.Catalog.Application.Artists;

static internal class ArtistValidationRules
{
    private const int MinNameLength = 2;
    private const int MaxNameLength = 200;
    private const int MaxBioLength = 2000;
    private const int MaxImageUrlLength = 500;
    private const string NameRegex = @"^[\p{L}\p{N}\s\-'.]+$";

    private static readonly string[] AllowedImageExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"];

    extension<T>(IRuleBuilder<T, string> ruleBuilder)
    {
        public IRuleBuilderOptions<T, string> ArtistName()
        {
            return ruleBuilder
                .NotEmpty()
                .WithMessage("Name is required.")
                .MinimumLength(MinNameLength)
                .WithMessage($"Name must be at least {MinNameLength} characters.")
                .MaximumLength(MaxNameLength)
                .WithMessage($"Name must not exceed {MaxNameLength} characters.")
                .Matches(NameRegex)
                .WithMessage("Name can only contain letters, numbers, spaces, hyphens, and apostrophes")
                .Must(name => name.IsNotBlank)
                .WithMessage("Name cannot be only whitespace");
        }
    }

    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        public IRuleBuilderOptions<T, string?> ArtistBio()
        {
            return ruleBuilder
                .MaximumLength(MaxBioLength)
                .WithMessage($"Bio must not exceed {MaxBioLength} characters")
                .Must(bio => bio.IsEmpty || bio.IsNotBlank)
                .WithMessage("Bio cannot be only whitespace");
        }

        public IRuleBuilderOptions<T, string?> ArtistImageUrl()
        {
            return ruleBuilder
                .MaximumLength(MaxImageUrlLength)
                .WithMessage($"Image URL must not exceed {MaxImageUrlLength} characters")
                .Must(BeAValidHttpUrl)
                .WithMessage("Image URL must be a valid HTTP or HTTPS URL")
                .Must(BeAnImageUrl)
                .WithMessage($"Image URL must point to an image ({string.Join(", ", AllowedImageExtensions)})");
        }
    }

    private static bool BeAValidHttpUrl(string? url)
    {
        return url!.IsBlank
               || Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private static bool BeAnImageUrl(string? url)
    {
        if (url!.IsBlank)
        {
            return true;
        }

        var path = Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.AbsolutePath
            : url;

        return AllowedImageExtensions.Any(ext =>
            path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}
