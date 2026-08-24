using FluentValidation;

namespace Eventify.Catalog.Application.Artists.Commands.DeleteImageUrl;

internal sealed class DeleteArtistImageUrlCommandValidator : AbstractValidator<DeleteArtistImageUrlCommand>
{
    public DeleteArtistImageUrlCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}