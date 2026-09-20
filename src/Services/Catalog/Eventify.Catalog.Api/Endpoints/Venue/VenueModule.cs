using Carter;
using Eventify.Catalog.Application.Venues.Commands.Create;
using Eventify.ServiceDefaults;
using Eventify.SharedKernel.Application.Messaging;
using Eventify.SharedKernel.Extensions;

namespace Eventify.Catalog.Api.Endpoints.Venue;

public sealed class VenueModule : ICarterModule
{
    public sealed class CreateVenueRequest
    {
        public required string Name { get; init; }
        public required string Country { get; init; }
        public required string City { get; init; }
        public string? State { get; init; }
        public required string Street { get; init; }
        public required string ZipCode { get; init; }
    }

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/venues", async (CreateVenueRequest request,
            ICommandHandler<CreateVenueCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateVenueCommand
            {
                Name = request.Name,
                Country = request.Country,
                City = request.City,
                State = request.State,
                Street = request.Street,
                ZipCode = request.ZipCode
            };

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(id => Results.CreatedAtRoute("GetVenueById", new { id }, id), CustomResults.Problem);
        });
    }
}
