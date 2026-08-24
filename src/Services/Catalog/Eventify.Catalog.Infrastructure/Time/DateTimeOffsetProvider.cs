using Eventify.SharedKernel.Application;

namespace Eventify.Catalog.Infrastructure.Time;

internal sealed class DateTimeOffsetProvider : IDateTimeOffsetProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
