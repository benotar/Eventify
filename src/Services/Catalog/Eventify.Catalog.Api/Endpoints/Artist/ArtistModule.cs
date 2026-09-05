using Asp.Versioning.Conventions;
using Carter;
using Eventify.Catalog.Application.Artists.Commands.Create;
using Eventify.Catalog.Application.Artists.Commands.Delete;
using Eventify.Catalog.Application.Artists.Commands.UpdateProfile;
using Eventify.Catalog.Application.Artists.Queries;
using Eventify.Catalog.Application.Artists.Queries.Get;
using Eventify.Catalog.Application.Artists.Queries.GetById;
using Eventify.ServiceDefaults;
using Eventify.SharedKernel.Application.Common;
using Eventify.SharedKernel.Application.Messaging;
using Eventify.SharedKernel.Extensions;

namespace Eventify.Catalog.Api.Endpoints.Artist;

internal sealed class ArtistModule : ICarterModule
{
    public sealed record GetArtistsRequest(int Page = 1, int PageSize = 20);

    public sealed record CreateArtistRequest
    {
        public required string Name { get; init; }
        public string? Bio { get; init; }
        public string? ImageUrl { get; init; }
    }

    public sealed record UpdateArtistProfileRequest
    {
        public required string Name { get; init; }
        public required string Bio { get; init; }
    }

    public sealed record UpdateArtistImageUrlRequest
    {
        public required string ImageUrl { get; init; }
    }

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
        group.MapPost("/", async (CreateArtistRequest request,
            ICommandHandler<CreateArtistCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateArtistCommand { Name = request.Name, Bio = request.Bio, ImageUrl = request.ImageUrl };

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(id => Results.Created($"/v1/artists/{id}", id), CustomResults.Problem);
        });

        group.MapPut("/{id:guid}", async (Guid id,
            UpdateArtistProfileRequest profileRequest,
            ICommandHandler<UpdateArtistProfileCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateArtistProfileCommand { Id = id, Name = profileRequest.Name, Bio = profileRequest.Bio, };

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        });

        group.MapDelete("/{id:guid}", async (Guid id,
            ICommandHandler<DeleteArtistCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteArtistCommand { Id = id };

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        });

        // Queries
        group.MapGet("/", async ([AsParameters] GetArtistsRequest request,
            IQueryHandler<GetArtistsQuery, PagedResult<ArtistResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetArtistsQuery(request.Page, request.PageSize);

            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        });

        group.MapGet("/{id:guid}", async (Guid id,
            IQueryHandler<GetArtistByIdQuery, ArtistResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetArtistByIdQuery(id);

            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        });
    }
}
