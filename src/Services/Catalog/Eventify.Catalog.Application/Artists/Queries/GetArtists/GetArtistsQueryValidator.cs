using Eventify.SharedKernel;
using FluentValidation;

namespace Eventify.Catalog.Application.Artists.Queries.GetArtists;

public sealed class GetArtistsQueryValidator : AbstractValidator<GetArtistsQuery>
{
    public GetArtistsQueryValidator()
    {
        RuleFor(prop => prop.Page)
            .Must(prop => prop >= SharedConstants.MinPageSize)
            .WithMessage(SharedConstants.PageMustBePositive);

        RuleFor(prop => prop.PageSize)
            .Must(prop => prop is >= SharedConstants.MinPageSize and <= SharedConstants.MaxPageSize)
            .WithMessage(SharedConstants.PageSizeMustBeInRange);
    }
}
