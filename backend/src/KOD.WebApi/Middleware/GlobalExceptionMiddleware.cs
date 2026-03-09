using System.Net;
using System.Text.Json;

using KOD.Application.Exceptions.Statuses;
using KOD.Domain.Exceptions.Auth;
using KOD.Domain.Exceptions.Users;

using Microsoft.AspNetCore.Mvc;

namespace KOD.WebApi.Middleware;

/// <summary>
/// Middleware responsible for handling unhandled exceptions globally and converting them into standardized HTTP responses.
/// </summary>
internal sealed class GlobalExceptionMiddleware
{
    #region Private fields

    /// <summary>
    /// The next middleware in the HTTP request pipeline.
    /// </summary>
    private readonly RequestDelegate _next;

    /// <summary>
    /// Logger used to record exception details and other middleware events.
    /// </summary>
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// The options used for JSON serialization when writing responses.
    /// </summary>
    private readonly JsonSerializerOptions _options;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the HTTP request pipeline.</param>
    /// <param name="logger">The logger used to record exception details.</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Invokes the middleware to process the HTTP request and handle any thrown exceptions.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

    #endregion

    #region Private methods

    /// <summary>
    /// Handles an exception by mapping it to an appropriate HTTP status code
    /// and writing a standardized JSON error response.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode statusCode;
        string message;

        switch (exception)
        {
            case NotFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = exception.Message;
                break;
            case CredentialsException:
                statusCode = HttpStatusCode.Unauthorized;
                message = exception.Message;
                break;
            case UserVerifiedException:
                statusCode = HttpStatusCode.Conflict;
                message = exception.Message;
                break;
            case UserNotVerifiedException:
                statusCode = HttpStatusCode.Unauthorized;
                message = exception.Message;
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                message = "An unexpected server error occurred.";
                break;
        }

        _logger.LogError(exception, "Unhandled exception while processing request.");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = message,
            Detail = exception.StackTrace,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problemDetails, _options);

        return context.Response.WriteAsync(json);
    }

    #endregion
}
