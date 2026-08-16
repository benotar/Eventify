using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.Application.Artists.Commands.Update;

public sealed record UpdateArtistCommand(Guid Id, string Name, string? Bio, string? ImageUrl) : ICommand;
