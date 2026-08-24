using Eventify.Catalog.Domain.Artists.ValueObjects;
using Eventify.SharedKernel.Domain;

namespace Eventify.Catalog.Domain.Artists.DomainEvents;

public record ArtistDeletedDomainEvent(ArtistId ArtistId) : IDomainEvent;
