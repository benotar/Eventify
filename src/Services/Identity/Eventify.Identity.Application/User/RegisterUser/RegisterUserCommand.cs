using Eventify.SharedKernel.Application.CQRS;

namespace Eventify.Identity.Application.User.RegisterUser;

public sealed record RegisterUserCommand(string Email, string FirstName, string LastName, string Password) : ICommand<Guid>;