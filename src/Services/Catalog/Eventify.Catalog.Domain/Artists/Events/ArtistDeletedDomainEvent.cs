using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Domain;

namespace Eventify.Catalog.Domain.Artists.Events;

public sealed record ArtistDeletedDomainEvent(ArtistId ArtistId) : DomainEvent;
