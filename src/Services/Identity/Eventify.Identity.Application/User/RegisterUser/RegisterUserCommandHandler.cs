using Eventify.Identity.Domain.Entities;
using Eventify.Localization;
using Eventify.SharedKernel;
using Eventify.SharedKernel.Application.Messaging;
using Microsoft.AspNetCore.Identity;

namespace Eventify.Identity.Application.User.RegisterUser;

public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterUserCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        if (await _userManager.FindByEmailAsync(command.Email) is not null)
        {
            return Result.Failure<Guid>(Error.Conflict(nameof(command.Email),
                Captions.AlreadyExistsValidation)); // Move to UserErrors
        }

        var user = new ApplicationUser(command.Email, command.FirstName, command.LastName);

        var createUserResult = await _userManager.CreateAsync(user, command.Password);

        if (!createUserResult.Succeeded)
        {
            return Result.Failure<Guid>(Error.Failure(string.Empty,
                createUserResult.Errors.First().Description)); // Move to UserErrors
        }

        return user.Id;
    }
}
