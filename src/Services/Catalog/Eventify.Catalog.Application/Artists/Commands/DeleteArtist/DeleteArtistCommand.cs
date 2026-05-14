using Eventify.SharedKernel.Application.CQRS;

namespace Eventify.Catalog.Application.Artists.Commands.DeleteArtist;

public sealed record DeleteArtistCommand(Guid Id) : ICommand;
