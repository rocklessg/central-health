using System.Net;
using System.Text.Json;
using CentralHealth.Application.Common;
using FluentValidation;

namespace CentralHealth.Api.Middleware;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);

        var statusCode = HttpStatusCode.InternalServerError;
        var response = ApiResponse.FailureResponse("An unexpected error occurred");

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = HttpStatusCode.BadRequest;
                var errors = validationException.Errors.Select(e => e.ErrorMessage).ToList();
                response = ApiResponse.FailureResponse("Validation failed", errors);
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                response = ApiResponse.FailureResponse("Unauthorized access");
                break;

            case ArgumentException argumentException:
                statusCode = HttpStatusCode.BadRequest;
                response = ApiResponse.FailureResponse(argumentException.Message);
                break;

            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                response = ApiResponse.FailureResponse("Resource not found");
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, jsonOptions);

        await context.Response.WriteAsync(json);
    }
}
