using Eventify.SharedKernel.Application.CQRS;

namespace Eventify.Catalog.Application.Artists.Commands.CreateArtist;

public sealed record CreateArtistCommand(string Name, string? Bio, string? ImageUrl) : ICommand<Guid>;
