using Eventify.SharedKernel.Domain.Exceptions;
using Eventify.SharedKernel.Extensions;

namespace Eventify.SharedKernel.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }

    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException("Money amount cannot be negative.");
        }

        if (currency.IsBlank || currency.Length != 3)
        {
            throw new DomainException("Currency must be a 3-letter ISO 4217 code.");
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
}
