namespace Eventify.SharedKernel.Extensions;

public static class GuidExtensions
{
    extension(Guid guid)
    {
        public bool IsEmpty => guid == Guid.Empty;
    }
}
