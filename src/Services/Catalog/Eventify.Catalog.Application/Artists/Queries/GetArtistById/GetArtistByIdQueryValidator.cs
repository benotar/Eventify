using FluentValidation;

namespace Eventify.Catalog.Application.Artists.Queries.GetArtistById;

public sealed class GetArtistByIdQueryValidator : AbstractValidator<GetArtistByIdQuery>
{
    public GetArtistByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(CatalogConstants.ArtistIdIsRequired);
    }
}
