using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Common.Exceptions;

namespace Ticketing.Api.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,Exception exception,CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            httpContext.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation error",
                Detail = validationException.Message,
                Instance = httpContext.Request.Path
            };

            await httpContext.Response.WriteAsJsonAsync(
                problemDetails,
                cancellationToken);

            return true;
        }

        _logger.LogError(
            exception,
            "An unhandled exception occurred while processing the request.");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var internalServerError = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "An unexpected error occurred while processing the request.",
            Instance = httpContext.Request.Path
        };

        await httpContext.Response.WriteAsJsonAsync(
            internalServerError,
            cancellationToken);

        return true;
    }
}