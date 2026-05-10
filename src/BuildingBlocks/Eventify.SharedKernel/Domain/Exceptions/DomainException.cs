using Eventify.SharedKernel.Extensions;

namespace Eventify.SharedKernel.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static void ThrowIfNullOrWhiteSpace(string? value, string message)
    {
        if (value!.IsBlank)
        {
            throw new DomainException(message);
        }
    }

    public static void ThrowIfNullOrEmpty(string? value, string message)
    {
        if (value!.IsEmpty)
        {
            throw new DomainException(message);
        }
    }
}
