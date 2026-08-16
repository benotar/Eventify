using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.Application.Artists.Commands.Delete;

public sealed record DeleteArtistCommand(Guid Id) : ICommand;
