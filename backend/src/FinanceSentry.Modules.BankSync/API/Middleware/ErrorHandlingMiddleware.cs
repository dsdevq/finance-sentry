namespace FinanceSentry.Modules.BankSync.API.Middleware;

using System.Net;
using System.Text.Json;
using FinanceSentry.Modules.BankSync.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Global exception handler — converts domain/infrastructure exceptions to
/// user-friendly HTTP responses. Never exposes stack traces or technical details.
/// </summary>
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger = logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode, userMessage) = exception switch
        {
            BankSyncException bse => (bse.HttpStatusCode, bse.ErrorCode, bse.Message),
            DbUpdateException => (503, "DATABASE_ERROR", "Database unavailable. Try again in 1 minute."),
            OperationCanceledException => (499, "REQUEST_CANCELLED", "Request was cancelled."),
            _ => (500, "INTERNAL_ERROR", "An unexpected error occurred. Please try again.")
        };

        // Log technical details server-side only
        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception [{ErrorCode}]: {Message}", errorCode, exception.Message);
        else
            _logger.LogWarning("Handled exception [{ErrorCode}]: {Message}", errorCode, exception.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new { error = userMessage, errorCode };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }
}
