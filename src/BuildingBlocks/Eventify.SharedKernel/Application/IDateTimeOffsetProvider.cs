namespace Eventify.SharedKernel.Application;

public interface IDateTimeOffsetProvider
{
    DateTimeOffset UtcNow { get; }
}
