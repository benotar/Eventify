using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.Application.Artists.Commands.DeleteImageUrl;

internal sealed record DeleteArtistImageUrlCommand : ICommand
{
    public required Guid Id { get; init; }
}
