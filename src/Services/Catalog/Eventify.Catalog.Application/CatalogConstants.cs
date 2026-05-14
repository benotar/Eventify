namespace Eventify.Catalog.Application;

public static class CatalogConstants
{
    public const string ArtistNotFound = "Artist not found.";
    public const string ArtistIdIsRequired = "Artist id is required.";
    public const string ArtistsTableName = "artists";

    public const string PageMustBePositive = "Page must be greater than or equal to 1.";
    public const string PageSizeMustBeInRange = "Page size must be between 1 and 100.";

    public const int MinNameLength = 2;
    public const int MaxNameLength = 200;
    public const int MaxBioLength = 2000;
    public const int MaxImageUrlLength = 500;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;
}
