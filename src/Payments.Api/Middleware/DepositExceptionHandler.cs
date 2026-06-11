using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Payments.Api.Providers;
using Payments.Api.Services;

namespace Payments.Api.Middleware;

/// <summary>
/// Maps deposit-domain exceptions to HTTP responses per the NFR-7 error
/// classification (validation, idempotency conflict, provider business failure vs.
/// transient failure, internal error).
/// </summary>
public class DepositExceptionHandler(ILogger<DepositExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            // Thrown by minimal API request body binding when the JSON payload doesn't
            // match the target type (e.g. a decimal where `amount` expects a long, NFR-9).
            BadHttpRequestException ex => new ProblemDetails
            {
                Status = ex.StatusCode,
                Title = "Malformed request",
                Detail = "The request body could not be parsed.",
            },
            DepositValidationException ex => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = string.Join(" ", ex.Errors),
                Extensions = { ["errors"] = ex.Errors },
            },
            IdempotencyConflictException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Idempotency conflict",
                Detail = ex.Message,
            },
            DepositNotFoundException ex => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Deposit not found",
                Detail = ex.Message,
            },
            TerminalDepositException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Deposit is in a terminal status",
                Detail = ex.Message,
            },
            DepositNotReadyException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Deposit is not ready",
                Detail = ex.Message,
            },
            ProviderTransientException => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Payment provider unavailable",
                Detail = "The payment provider request failed transiently. Retry, or reconcile once the outcome is known.",
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal error",
                Detail = "An unexpected error occurred.",
            },
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
