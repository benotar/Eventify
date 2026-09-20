using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.Application.Venues.Commands.Create;

public sealed record CreateVenueCommand : ICommand<Guid>
{
    public required string Name { get; init; }
    public required string Country { get; init; }
    public required string City { get; init; }
    public string? State { get; init; }
    public required string Street { get; init; }
    public required string ZipCode { get; init; }
}
