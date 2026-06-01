using Eventify.SharedKernel;
using FluentValidation;

namespace Eventify.Identity.Application.User.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(SharedConstants.MaxEmailLength);

        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MinimumLength(SharedConstants.MinNameLength)
            .MaximumLength(SharedConstants.MaxNameLength);

        RuleFor(command => command.LastName)
            .NotEmpty()
            .MinimumLength(SharedConstants.MinNameLength)
            .MaximumLength(SharedConstants.MaxNameLength);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}
