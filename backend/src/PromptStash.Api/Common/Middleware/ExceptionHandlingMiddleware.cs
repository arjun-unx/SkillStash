using System.Net;
using System.Text.Json;
using FluentValidation;
using PromptStash.Api.Common.Exceptions;

namespace PromptStash.Api.Common.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteResponseAsync(context, ex);
        }
    }

    private async Task WriteResponseAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail, errors) = MapException(exception, env.IsDevelopment());
        if (status >= 500)
            logger.LogError(exception, "Unhandled exception while processing request");
        else
            logger.LogWarning(exception, "Request failed: {Title}", title);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;

        var payload = new
        {
            type = $"https://httpstatuses.io/{status}",
            title,
            status,
            detail,
            traceId = context.TraceIdentifier,
            errors
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static (int Status, string Title, string Detail, object? Errors) MapException(Exception ex, bool isDev)
    {
        return ex switch
        {
            ValidationException ve => (
                (int)HttpStatusCode.BadRequest, "Validation failed", "One or more validation errors occurred.",
                ve.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            NotFoundException => ((int)HttpStatusCode.NotFound, "Not Found", ex.Message, null),
            ConflictException => ((int)HttpStatusCode.Conflict, "Conflict", ex.Message, null),
            ForbiddenAccessException => ((int)HttpStatusCode.Forbidden, "Forbidden", ex.Message, null),
            TrendingSyncException => ((int)HttpStatusCode.ServiceUnavailable, "Trending sync unavailable", ex.Message, null),
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "Unauthorized", ex.Message, null),
            _ => ((int)HttpStatusCode.InternalServerError, "Internal Server Error",
                isDev ? ex.ToString() : "An unexpected error occurred.", null)
        };
    }
}
