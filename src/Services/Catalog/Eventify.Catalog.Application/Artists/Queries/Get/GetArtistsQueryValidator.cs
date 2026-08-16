using Eventify.SharedKernel;
using FluentValidation;

namespace Eventify.Catalog.Application.Artists.Queries.Get;

public sealed class GetArtistsQueryValidator : AbstractValidator<GetArtistsQuery>
{
    public GetArtistsQueryValidator()
    {
        RuleFor(prop => prop.Page)
            .Must(prop => prop >= SharedConstants.MinPageSize);

        RuleFor(prop => prop.PageSize)
            .Must(prop => prop is >= SharedConstants.MinPageSize and <= SharedConstants.MaxPageSize);
    }
}
