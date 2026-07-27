using System.Net;
using System.Text.Json;
using FluentValidation;
using Kota.Pokedex.Core.Exceptions;

namespace Kota.Pokedex.Api.Middleware;

public class ExceptionHandlingMiddleware {
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger) {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context) {
        try {
            await _next(context);
        }
        catch (Exception ex) {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception) {
        _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        var (statusCode, message) = exception switch {
            PokeApiException pokeEx => (pokeEx.StatusCode ?? (int)HttpStatusCode.BadGateway, pokeEx.Message),
            ValidationException validationEx => ((int)HttpStatusCode.BadRequest, string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage))),
            UnauthorizedAccessException authEx => ((int)HttpStatusCode.Unauthorized, authEx.Message),
            ArgumentException argEx => ((int)HttpStatusCode.BadRequest, argEx.Message),
            KeyNotFoundException notFoundEx => ((int)HttpStatusCode.NotFound, notFoundEx.Message),
            _ when IsUniqueConstraintViolation(exception) =>
                ((int)HttpStatusCode.Conflict, "Username is already taken."),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }

    private static bool IsUniqueConstraintViolation(Exception exception) {
        for (Exception? ex = exception; ex is not null; ex = ex.InnerException) {
            if (ex.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}

public static class ExceptionHandlingMiddlewareExtensions {
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
