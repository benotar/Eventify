using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.Application.Artists.Commands.Create;

public sealed record CreateArtistCommand : ICommand<Guid>
{
    public required string Name { get; init; }
    public string? Bio { get; init; }
    public string? ImageUrl { get; init; }
}
