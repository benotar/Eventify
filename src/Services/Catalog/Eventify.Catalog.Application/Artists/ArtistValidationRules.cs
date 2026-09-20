using Eventify.SharedKernel;
using Eventify.SharedKernel.Extensions;
using FluentValidation;

namespace Eventify.Catalog.Application.Artists;

static internal class ArtistValidationRules
{
    private static readonly string[] AllowedImageExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"];

    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        public IRuleBuilderOptions<T, string?> ArtistBio()
        {
            return ruleBuilder
                .MaximumLength(SharedConstants.MaxBioLength)
                .Must(bio => bio.IsEmpty || bio.IsNotBlank);
        }

        public IRuleBuilderOptions<T, string?> ArtistImageUrl()
        {
            return ruleBuilder
                .MaximumLength(SharedConstants.MaxImageUrlLength)
                .Must(BeAValidHttpUrl)
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

    private static bool BeAnImageUrl(string url)
    {
        if (url.IsBlank)
        {
            return false;
        }

        var path = Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.AbsolutePath
            : url;

        return AllowedImageExtensions.Any(ext =>
            path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}
