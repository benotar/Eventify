using Eventify.Catalog.Application.Artists.Responses;
using Eventify.SharedKernel.Application.Common;
using Eventify.SharedKernel.Application.CQRS;

namespace Eventify.Catalog.Application.Artists.Queries.GetArtists;

public sealed record GetArtistsQuery(int Page, int PageSize) : IQuery<PagedResult<ArtistResponse>>;
