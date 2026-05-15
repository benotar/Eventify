using Eventify.SharedKernel.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Eventify.Catalog.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (title, status) = exception switch
        {
            DomainException => ("A domain invariant was violated.", StatusCodes.Status500InternalServerError),
            _ => ("An unexpected error occurred.", StatusCodes.Status500InternalServerError)
        };

        var problemDetails = new ProblemDetails { Title = title, Status = status, Type = $"https://httpstatuses.com/{status}", };

        httpContext.Response.StatusCode = status;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }
}
