using Eventify.Catalog.Application.Interfaces;
using Eventify.Catalog.Domain.Venues;
using Eventify.Catalog.Domain.Venues.ValueObjects;
using Eventify.SharedKernel;
using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.Application.Venues.Commands.Create;

internal sealed class CreateVenueCommandHandler : ICommandHandler<CreateVenueCommand, Guid>
{
    private readonly IVenueDbContext _dbContext;

    public CreateVenueCommandHandler(IVenueDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<Guid>> HandleAsync(CreateVenueCommand command, CancellationToken cancellationToken)
    {
        var name = VenueName.Create(command.Name);

        var address = Address.Create(command.Country, command.City, command.State, command.Street, command.ZipCode);

        var venue = Venue.Create(name, address);

        _dbContext.Venues.Add(venue);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return venue.Id.Value;
    }
}
