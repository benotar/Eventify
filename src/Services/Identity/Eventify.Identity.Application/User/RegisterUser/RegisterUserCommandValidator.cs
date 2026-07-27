using Eventify.Localization;
using Eventify.SharedKernel;
using FluentValidation;

namespace Eventify.Identity.Application.User.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(Captions.RequiredValidation)
            .EmailAddress().WithMessage(Captions.EmailAddressValidation)
            .MaximumLength(SharedConstants.MaxEmailLength).WithMessage(Captions.MaxLengthValidation);

        RuleFor(command => command.FirstName)
            .NotEmpty().WithMessage(Captions.RequiredValidation)
            .MinimumLength(SharedConstants.MinNameLength).WithMessage(Captions.MinLengthValidation)
            .MaximumLength(SharedConstants.MaxNameLength).WithMessage(Captions.MaxLengthValidation);

        RuleFor(command => command.LastName)
            .NotEmpty().WithMessage(Captions.RequiredValidation)
            .MinimumLength(SharedConstants.MinNameLength).WithMessage(Captions.MinLengthValidation)
            .MaximumLength(SharedConstants.MaxNameLength).WithMessage(Captions.MaxLengthValidation);

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage(Captions.RequiredValidation)
            .MinimumLength(SharedConstants.MinPasswordLength).WithMessage(Captions.MinLengthValidation)
            .MaximumLength(SharedConstants.MaxPasswordLength).WithMessage(Captions.MaxLengthValidation)
            .Matches("[A-Z]").WithMessage(Captions.PasswordUppercase)
            .Matches("[a-z]").WithMessage(Captions.PasswordLowercase)
            .Matches("[0-9]").WithMessage(Captions.PasswordDigit)
            .Matches("[^a-zA-Z0-9]").WithMessage(Captions.PasswordValidation);
    }
}
