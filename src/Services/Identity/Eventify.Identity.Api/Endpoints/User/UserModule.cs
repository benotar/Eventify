using Asp.Versioning.Conventions;
using Carter;
using Eventify.ServiceDefaults;
using MediatR;

namespace Eventify.Identity.Api.Endpoints.User;

public sealed class UserModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(1, 0)
            .Build();

        var group = app.MapGroup("/v1/users")
            .WithTags("Users")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(1, 0);

        // Commands
        group.MapPost("/", async (RegisterUserRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(request.ToCommand(), ct);

            return result.Match(id => Results.Created($"/v1/users/{id}", id),
                errors => errors.ToProblemDetails());
        });
    }
}
