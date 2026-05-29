using System.Net;
using System.Text.Json;
using FamilyFirst.Application.Common.Exceptions;
using FamilyFirst.Application.Common.Models;
using AppValidationException = FamilyFirst.Application.Common.Exceptions.ValidationException;

namespace FamilyFirst.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            AppValidationException validationException => (
                (HttpStatusCode)validationException.StatusCode,
                ApiResponse<object>.Failure(
                    FlattenValidationErrors(validationException),
                    validationException.Message)),
            NotFoundException notFoundException => (
                HttpStatusCode.NotFound,
                ApiResponse<object>.Failure("NotFound", notFoundException.Message)),
            ForbiddenAccessException forbiddenAccessException => (
                HttpStatusCode.Forbidden,
                ApiResponse<object>.Failure("Forbidden", forbiddenAccessException.Message)),
            ConflictException conflictException => (
                HttpStatusCode.Conflict,
                ApiResponse<object>.Failure("Conflict", conflictException.Message)),
            UnauthorizedAccessException unauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                ApiResponse<object>.Failure("Unauthorized", unauthorizedAccessException.Message)),
            _ => (
                HttpStatusCode.InternalServerError,
                ApiResponse<object>.Failure("InternalServerError", "An unexpected error occurred."))
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing request {Method} {Path}.", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with status {StatusCode} for {Method} {Path}.", (int)statusCode, context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static IEnumerable<ErrorDto> FlattenValidationErrors(AppValidationException exception)
    {
        return exception.Errors.SelectMany(error =>
            error.Value.Select(message => new ErrorDto(error.Key, message)));
    }
}
