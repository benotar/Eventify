using Eventify.SharedKernel.Application.Common;
using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.Application.Artists.Queries.Get;

public sealed record GetArtistsQuery(int Page, int PageSize) : IQuery<PagedResult<ArtistResponse>>;
