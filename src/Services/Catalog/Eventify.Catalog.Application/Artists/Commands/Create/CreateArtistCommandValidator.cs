using Eventify.SharedKernel.Extensions;
using FluentValidation;

namespace Eventify.Catalog.Application.Artists.Commands.Create;

public sealed class CreateArtistCommandValidator : AbstractValidator<CreateArtistCommand>
{
    public CreateArtistCommandValidator()
    {
        RuleFor(command => command.Name)
            .ArtistName();

        RuleFor(command => command.Bio)
            .ArtistBio()
            .When(command => command.Bio!.IsNotEmpty);

        RuleFor(command => command.ImageUrl)
            .ArtistImageUrl()
            .When(command => command.ImageUrl!.IsNotBlank);
    }
}
