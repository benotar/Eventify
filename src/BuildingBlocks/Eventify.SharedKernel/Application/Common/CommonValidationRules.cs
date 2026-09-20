using Eventify.SharedKernel.Extensions;
using FluentValidation;

namespace Eventify.SharedKernel.Application.Common;

public static class CommonValidationRules
{
    private const string NameRegex = @"^[\p{L}\p{N}\s\-'.]+$";

    public static void EntityName<TEntity>(this IRuleBuilder<TEntity, string> ruleBuilder)
    {
        ruleBuilder
            .NotEmpty()
            .MinimumLength(SharedConstants.MinNameLength)
            .MaximumLength(SharedConstants.MaxNameLength)
            .Matches(NameRegex)
            .Must(name => name.IsNotBlank);
    }
}
