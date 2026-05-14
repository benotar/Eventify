using Eventify.SharedKernel.Application.CQRS;

namespace Eventify.Catalog.Application.Artists.Commands.UpdateArtist;

public sealed record UpdateArtistCommand(Guid Id, string Name, string? Bio, string? ImageUrl) : ICommand;
