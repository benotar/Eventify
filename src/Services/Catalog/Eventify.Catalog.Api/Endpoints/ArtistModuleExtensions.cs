using Eventify.Catalog.Application.Artists.Commands.CreateArtist;
using Eventify.Catalog.Application.Artists.Commands.UpdateArtist;
using Eventify.Catalog.Application.Artists.Queries.GetArtists;

namespace Eventify.Catalog.Api.Endpoints;

public static class ArtistModuleExtensions
{
    extension(CreateArtistRequest request)
    {
        public CreateArtistCommand ToCommand()
        {
            return new CreateArtistCommand(request.Name, request.Bio, request.ImageUrl);
        }
    }

    extension(UpdateArtistRequest request)
    {
        public UpdateArtistCommand ToCommand(Guid id)
        {
            return new UpdateArtistCommand(id, request.Name, request.Bio, request.ImageUrl);
        }
    }

    extension(GetArtistsRequest request)
    {
        public GetArtistsQuery ToQuery()
        {
            return new GetArtistsQuery(request.Page, request.PageSize);
        }
    }
}
