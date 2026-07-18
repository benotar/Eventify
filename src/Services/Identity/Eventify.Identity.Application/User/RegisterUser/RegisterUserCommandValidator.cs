using Eventify.Identity.Domain.Enums;
using Eventify.SharedKernel;
using FluentValidation;

namespace Eventify.Identity.Application.User.RegisterUser;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(nameof(CaptionCode.RequiredValidation))
            .EmailAddress().WithMessage(nameof(CaptionCode.EmailAddressValidation))
            .MaximumLength(SharedConstants.MaxEmailLength).WithMessage(nameof(CaptionCode.MaxLengthValidation));

        RuleFor(command => command.FirstName)
            .NotEmpty().WithMessage(nameof(CaptionCode.RequiredValidation))
            .MinimumLength(SharedConstants.MinNameLength).WithMessage(nameof(CaptionCode.MinLengthValidation))
            .MaximumLength(SharedConstants.MaxNameLength).WithMessage(nameof(CaptionCode.MaxLengthValidation));

        RuleFor(command => command.LastName)
            .NotEmpty().WithMessage(nameof(CaptionCode.RequiredValidation))
            .MinimumLength(SharedConstants.MinNameLength).WithMessage(nameof(CaptionCode.MinLengthValidation))
            .MaximumLength(SharedConstants.MaxNameLength).WithMessage(nameof(CaptionCode.MaxLengthValidation));

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage(nameof(CaptionCode.RequiredValidation))
            .MinimumLength(SharedConstants.MinPasswordLength).WithMessage(nameof(CaptionCode.MinLengthValidation))
            .MaximumLength(SharedConstants.MaxPasswordLength).WithMessage(nameof(CaptionCode.MaxLengthValidation))
            .Matches("[A-Z]").WithMessage(nameof(CaptionCode.PasswordUppercase))
            .Matches("[a-z]").WithMessage(nameof(CaptionCode.PasswordLowercase))
            .Matches("[0-9]").WithMessage(nameof(CaptionCode.PasswordDigit))
            .Matches("[^a-zA-Z0-9]").WithMessage(nameof(CaptionCode.PasswordValidation));
    }
}
