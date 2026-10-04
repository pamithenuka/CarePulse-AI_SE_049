using CarePulse.Api.Services.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarePulse.Api.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (Exception exception)
        {
            var (status, message) = exception switch
            {
                ApiProblem problem => (problem.StatusCode, problem.Message),
                DbUpdateConcurrencyException => (409, "This record changed. Refresh and try again."),
                DbUpdateException { InnerException: PostgresException { SqlState: "23505" } } => (409, "This operation conflicts with an existing record. Refresh and try again."),
                DbUpdateException { InnerException: PostgresException { SqlState: "23503" } } => (409, "A related record is missing or still in use."),
                _ => (500, "An unexpected server error occurred. Use the reference ID when reporting it.")
            };
            if (status >= 500) logger.LogError(exception, "Request {TraceId} failed", context.TraceIdentifier);
            if (context.Response.HasStarted) throw;
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new { statusCode = status, message, traceId = context.TraceIdentifier });
        }
    }
}
