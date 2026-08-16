using FluentValidation;

namespace Eventify.Catalog.Application.Artists.Commands.Delete;

public sealed class DeleteArtistCommandValidator : AbstractValidator<DeleteArtistCommand>
{
    public DeleteArtistCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty();
    }
}
