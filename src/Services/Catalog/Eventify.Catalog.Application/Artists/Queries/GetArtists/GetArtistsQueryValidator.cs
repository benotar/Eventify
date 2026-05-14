using FluentValidation;

namespace Eventify.Catalog.Application.Artists.Queries.GetArtists;

public sealed class GetArtistsQueryValidator : AbstractValidator<GetArtistsQuery>
{
    public GetArtistsQueryValidator()
    {
        RuleFor(prop => prop.Page)
            .Must(prop => prop >= CatalogConstants.MinPageSize)
            .WithMessage(CatalogConstants.PageMustBePositive);

        RuleFor(prop => prop.PageSize)
            .Must(prop => prop is >= CatalogConstants.MinPageSize and <= CatalogConstants.MaxPageSize)
            .WithMessage(CatalogConstants.PageSizeMustBeInRange);
    }
}
