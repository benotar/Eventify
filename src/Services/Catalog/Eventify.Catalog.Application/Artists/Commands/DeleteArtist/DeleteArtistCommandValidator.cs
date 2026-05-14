using FluentValidation;

namespace Eventify.Catalog.Application.Artists.Commands.DeleteArtist;

public sealed class DeleteArtistCommandValidator : AbstractValidator<DeleteArtistCommand>
{
    public DeleteArtistCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty()
            .WithMessage(CatalogConstants.ArtistIdIsRequired);
    }
}
