using Asp.Versioning.Conventions;
using Carter;
using Eventify.Catalog.Api.Extensions;
using Eventify.Catalog.Application.Artists.Commands.DeleteArtist;
using Eventify.Catalog.Application.Artists.Queries.GetArtistById;
using MediatR;

namespace Eventify.Catalog.Api.Endpoints;

public sealed class ArtistModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(1, 0)
            .Build();

        var group = app.MapGroup("/v1/artists")
            .WithTags("Artists")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(1, 0);

        // Commands
        group.MapPost("/", async (CreateArtistRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(request.ToCommand(), ct);

            return result.Match(id => Results.Created($"/v1/artists/{id}", id),
                errors => errors.ToProblemDetails());
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateArtistRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(request.ToCommand(id), ct);

            return result.Match(_ => Results.NoContent(),
                errors => errors.ToProblemDetails());
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteArtistCommand(id), ct);

            return result.Match(_ => Results.NoContent(),
                errors => errors.ToProblemDetails());
        });

        // Queries
        group.MapGet("/", async ([AsParameters] GetArtistsRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(request.ToQuery(), ct);

            return result.Match(Results.Ok, errors => errors.ToProblemDetails());
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetArtistByIdQuery(id), ct);

            return result.Match(Results.Ok, errors => errors.ToProblemDetails());
        });
    }
}
