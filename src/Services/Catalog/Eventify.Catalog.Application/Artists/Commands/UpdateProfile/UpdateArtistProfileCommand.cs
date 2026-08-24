using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.Application.Artists.Commands.UpdateProfile;

public sealed record UpdateArtistProfileCommand : ICommand
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Bio { get; init; }
}
