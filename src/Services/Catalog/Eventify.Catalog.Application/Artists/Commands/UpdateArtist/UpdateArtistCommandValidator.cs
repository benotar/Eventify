using Eventify.SharedKernel.Extensions;
using FluentValidation;

namespace Eventify.Catalog.Application.Artists.Commands.UpdateArtist;

public sealed class UpdateArtistCommandValidator : AbstractValidator<UpdateArtistCommand>
{
    public UpdateArtistCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage(CatalogConstants.ArtistIdIsRequired);

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
