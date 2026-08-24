using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.Application.Artists.Commands.UpdateArtistImageUrl;

internal sealed record UpdateArtistImageUrlCommand() : ICommand
{
    public required Guid Id { get; init; }
    public required string ImageUrl { get; init; }
}
