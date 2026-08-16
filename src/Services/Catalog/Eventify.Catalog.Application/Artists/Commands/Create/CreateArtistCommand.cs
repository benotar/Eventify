using Eventify.SharedKernel.Application.Messaging;

namespace Eventify.Catalog.Application.Artists.Commands.Create;

public sealed record CreateArtistCommand(string Name, string? Bio, string? ImageUrl) : ICommand<Guid>;
