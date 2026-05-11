using Eventify.Catalog.Application.Artists.Responses;
using Eventify.SharedKernel.Application.CQRS;

namespace Eventify.Catalog.Application.Artists.Queries;

public sealed record GetArtistByIdQuery(Guid Id) : IQuery<ArtistResponse>;