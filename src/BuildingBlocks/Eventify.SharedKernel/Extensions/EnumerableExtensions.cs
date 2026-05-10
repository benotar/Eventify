namespace Eventify.SharedKernel.Extensions;

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T>? enumerable)
    {
        public bool IsEmpty => enumerable is null || !enumerable.Any();
        public bool IsNotEmpty => enumerable is not null && enumerable.Any();
    }
}
