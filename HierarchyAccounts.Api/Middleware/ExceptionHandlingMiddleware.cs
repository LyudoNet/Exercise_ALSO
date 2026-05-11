namespace HierarchyAccounts.Api.Middleware;

using HierarchyAccounts.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Global middleware that catches domain and application exceptions
/// and maps them to appropriate HTTP status codes with RFC 7807 ProblemDetails responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Map exception types to HTTP status codes
        var (statusCode, title) = exception switch
        {
            KeyNotFoundException      => (StatusCodes.Status404NotFound,           "Resource not found."),
            CycleDetectedException    => (StatusCodes.Status400BadRequest,          "Cycle detected."),
            MaxDepthExceededException => (StatusCodes.Status400BadRequest,          "Maximum depth exceeded."),
            RootAccountException      => (StatusCodes.Status400BadRequest,          "Root account restriction."),
            DomainException           => (StatusCodes.Status400BadRequest,          "Business rule violation."),
            _                         => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message
        };

        return context.Response.WriteAsJsonAsync(problem);
    }
}
