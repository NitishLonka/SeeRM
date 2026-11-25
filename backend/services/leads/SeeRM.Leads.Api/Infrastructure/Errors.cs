using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SeeRM.Leads.Infrastructure;

/// <summary>Thrown by services; mapped to HTTP status codes by <see cref="ApiExceptionHandler"/>.</summary>
public abstract class ApiException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
    public abstract string Title { get; }
}

public sealed class NotFoundException(string resource, long id)
    : ApiException($"{resource} {id} was not found.")
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    public override string Title => "Not Found";
}

public sealed class ConflictException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public override string Title => "Conflict";
}

public sealed class BadRequestException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public override string Title => "Bad Request";
}

/// <summary>A downstream service (e.g. Customer) is unreachable or failing.</summary>
public sealed class ServiceUnavailableException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status503ServiceUnavailable;
    public override string Title => "Service Unavailable";
}

/// <summary>Turns exceptions into RFC 7807 ProblemDetails responses.</summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title, detail) = ex switch
        {
            ApiException api => (api.StatusCode, api.Title, api.Message),
            DbUpdateException { InnerException: PostgresException pg } => pg.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation => (409, "Conflict", "A record with the same unique value already exists."),
                PostgresErrorCodes.CheckViolation => (400, "Bad Request", $"Value violates database rule '{pg.ConstraintName}'."),
                _ => (500, "Server Error", "An unexpected database error occurred."),
            },
            _ => (500, "Server Error", "An unexpected error occurred."),
        };

        if (status >= 500) logger.LogError(ex, "Unhandled exception");

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = ex,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
        });
    }
}
