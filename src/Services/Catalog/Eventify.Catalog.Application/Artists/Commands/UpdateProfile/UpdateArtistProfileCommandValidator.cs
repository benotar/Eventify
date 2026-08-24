using Eventify.SharedKernel.Extensions;
using FluentValidation;

namespace Eventify.Catalog.Application.Artists.Commands.UpdateProfile;

public sealed class UpdateArtistProfileCommandValidator : AbstractValidator<UpdateArtistProfileCommand>
{
    public UpdateArtistProfileCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .ArtistName();

        RuleFor(command => command.Bio)
            .ArtistBio()
            .When(command => command.Bio!.IsNotEmpty);
    }
}
