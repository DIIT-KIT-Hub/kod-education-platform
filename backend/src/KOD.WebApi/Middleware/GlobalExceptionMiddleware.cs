using System.Net;
using System.Text.Json;

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
        var statusCode = (int)HttpStatusCode.InternalServerError;
        _logger.LogError(exception, "Unhandled exception while processing request.");

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = "An unexpected server error occurred.",
            Detail = exception.StackTrace,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problemDetails, _options);

        return context.Response.WriteAsync(json);
    }

    #endregion
}
