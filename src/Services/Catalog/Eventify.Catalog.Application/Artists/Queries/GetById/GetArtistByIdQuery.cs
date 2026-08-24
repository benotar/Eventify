using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.Application.Artists.Queries.GetById;

public sealed record GetArtistByIdQuery(Guid Id) : IQuery<ArtistResponse>;
