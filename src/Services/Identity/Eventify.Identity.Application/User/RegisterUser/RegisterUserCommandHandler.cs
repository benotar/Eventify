using ErrorOr;
using Eventify.Identity.Domain.Entities;
using Eventify.SharedKernel.Application.CQRS;
using Microsoft.AspNetCore.Identity;

namespace Eventify.Identity.Application.User.RegisterUser;

public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterUserCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ErrorOr<Guid>> Handle(RegisterUserCommand command, CancellationToken ct)
    {
        if (await _userManager.FindByEmailAsync(command.Email) is not null)
        {
            return Error.Conflict(description: string.Format(IdentityConstants.UserAlreadyExist, command.Email));
        }

        var user = new ApplicationUser(command.Email, command.FirstName, command.LastName);

        var createUserResult = await _userManager.CreateAsync(user, command.Password);

        if (!createUserResult.Succeeded)
        {
            return Error.Failure(description: createUserResult.Errors.First().Description);
        }

        return user.Id;
    }
}