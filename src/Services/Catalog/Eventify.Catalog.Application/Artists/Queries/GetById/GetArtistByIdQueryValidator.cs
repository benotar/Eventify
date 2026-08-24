using FluentValidation;

namespace Eventify.Catalog.Application.Artists.Queries.GetById;

public sealed class GetArtistByIdQueryValidator : AbstractValidator<GetArtistByIdQuery>
{
    public GetArtistByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
