using Eventify.SharedKernel.Application.Common;
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
            .EntityName();

        RuleFor(command => command.Bio)
            .ArtistBio()
            .When(command => command.Bio!.IsNotEmpty);
    }
}
