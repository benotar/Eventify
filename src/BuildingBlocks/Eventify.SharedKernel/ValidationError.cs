namespace Eventify.SharedKernel;

public sealed record ValidationError : Error
{
    public ValidationError(Error[] error)
        : base("Validation.General",
            "One or more validation errors occurred",
            ErrorType.Validation)
    {
        Errors = error;
    }

    public Error[] Errors { get; }

    public static ValidationError FromResults(IEnumerable<Result> results)
    {
        return new ValidationError([.. results.Where(r => r.IsFailure).Select(r => r.Error)]);
    }
}
