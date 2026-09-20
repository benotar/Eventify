using Eventify.SharedKernel;
using Eventify.SharedKernel.Application.Common;
using FluentValidation;

namespace Eventify.Catalog.Application.Venues.Commands.Create;

public sealed class CreateVenueCommandValidator : AbstractValidator<CreateVenueCommand>
{
    public CreateVenueCommandValidator()
    {
        RuleFor(command => command.Name)
            .EntityName();

        RuleFor(command => command.Country)
            .NotEmpty()
            .MaximumLength(SharedConstants.CountryMaxLength);

        RuleFor(command => command.City)
            .NotEmpty()
            .MaximumLength(SharedConstants.CityMaxLength);

        RuleFor(command => command.State)
            .MaximumLength(SharedConstants.StateMaxLength)
            .When(command => command.State is not null);

        RuleFor(command => command.Street)
            .NotEmpty()
            .MaximumLength(SharedConstants.StreetMaxLength);

        RuleFor(command => command.ZipCode)
            .NotEmpty()
            .MaximumLength(SharedConstants.ZipCodeMaxLength);
    }
}
