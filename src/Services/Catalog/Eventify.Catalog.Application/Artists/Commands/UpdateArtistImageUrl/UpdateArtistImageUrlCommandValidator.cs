using FluentValidation;

namespace Eventify.Catalog.Application.Artists.Commands.UpdateArtistImageUrl;

internal sealed class UpdateArtistImageUrlCommandValidator : AbstractValidator<UpdateArtistImageUrlCommand>
{
    public UpdateArtistImageUrlCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.ImageUrl)
            .ArtistImageUrl();
    }
}
