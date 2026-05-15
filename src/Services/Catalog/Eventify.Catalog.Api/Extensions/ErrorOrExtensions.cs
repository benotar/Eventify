using ErrorOr;
using Eventify.SharedKernel.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Eventify.Catalog.Api.Extensions;

public static class ErrorOrExtensions
{
    extension(IEnumerable<Error> errors)
    {
        public IResult ToProblemDetails()
        {
            var errorList = errors.ToList();

            if (errorList.IsEmpty)
            {
                return Results.Problem();
            }

            return errorList.All(error => error.Type == ErrorType.Validation)
                ? CreateValidationProblem(errorList)
                : CreateProblem(errorList.First());
        }
    }

    private static ProblemHttpResult CreateProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.Unauthorized => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        return TypedResults.Problem(statusCode: statusCode, title: error.Code, detail: error.Description);
    }

    private static ValidationProblem CreateValidationProblem(IEnumerable<Error> errors)
    {
        var validationErrors = errors
            .GroupBy(error => error.Code)
            .ToDictionary(group => group.Key,
                group => group.Select(error => error.Description).ToArray());

        return TypedResults.ValidationProblem(validationErrors, title: "Validation Error");
    }
}
