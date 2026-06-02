namespace Eventify.SharedKernel;

public static class SharedConstants
{
    // Roles
    public const string Admin = "admin";
    public const string Customer = "customer";

    // Configuration
    public const string Options = "Options";

    // Validation messages
    public const string PageMustBePositive = "Page must be greater than or equal to 1.";

    public const string PageSizeMustBeInRange = "Page size must be between 1 and 100.";

    // Length constraints
    public const int MinNameLength = 2;
    public const int MinPasswordLength = 8;


    public const int MaxNameLength = 200;
    public const int MaxBioLength = 2000;
    public const int MaxImageUrlLength = 500;
    public const int MaxEmailLength = 256;
    public const int MaxPasswordLength = 100;

    // Pagination
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;
}
