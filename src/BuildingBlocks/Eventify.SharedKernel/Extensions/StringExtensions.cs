namespace Eventify.SharedKernel.Extensions;

public static class StringExtensions
{
    extension(string value)
    {
        public bool IsEmpty => string.IsNullOrEmpty(value);
        
        public bool IsNotEmpty => !string.IsNullOrEmpty(value);
        
        public bool IsBlank => string.IsNullOrWhiteSpace(value);
        
        public bool IsNotBlank => !string.IsNullOrWhiteSpace(value);
    }
}
