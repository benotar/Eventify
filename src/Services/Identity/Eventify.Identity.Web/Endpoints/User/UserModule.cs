using Asp.Versioning.Conventions;
using Carter;
using Eventify.Identity.Application.User.RegisterUser;
using Eventify.ServiceDefaults;
using Eventify.SharedKernel;
using Eventify.SharedKernel.Application.Messaging;
using Eventify.SharedKernel.Extensions;

namespace Eventify.Identity.Web.Endpoints.User;

public sealed class UserModule : ICarterModule
{
    private record RegisterUserRequest(string Email, string FirstName, string LastName, string Password);

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
        group.MapPost("/", async (RegisterUserRequest request,
                ICommandHandler<RegisterUserCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RegisterUserCommand(request.Email, request.FirstName, request.LastName, request.Password);

                var result = await handler.HandleAsync(command, cancellationToken);

                return result.Match(id => Results.Created($"/v1/users/{id}", id), CustomResults.Problem);

                // return result.Match(id => Results.Created($"/v1/users/{id}", id),
                //     errors => errors.ToProblemDetails());
            })
            .RequireAuthorization(policyBuilder => policyBuilder.RequireRole(SharedConstants.Admin));
    }
}
