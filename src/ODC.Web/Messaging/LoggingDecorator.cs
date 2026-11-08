using System.Diagnostics;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Messaging;

/// <summary>
/// The outermost step of the pipeline: logs which request ran, how long it took and the error code of a failed result.
/// The request itself is never logged — it may hold passwords, codes or personal data (OWASP ASVS: no sensitive data
/// in logs). Exceptions are not logged here: they reach the exception handler, which logs them once.
/// </summary>
/// <typeparam name="TRequest">The request.</typeparam>
/// <typeparam name="TResponse">The answer.</typeparam>
/// <param name="inner">The next step.</param>
/// <param name="loggerFactory">Creates the <c>ODC.Web.Messaging</c> logger.</param>
internal sealed partial class LoggingDecorator<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> inner, ILoggerFactory loggerFactory)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// The short name of the request type in log messages.
    /// </summary>
    private static readonly string RequestName = RequestNames.Of(typeof(TRequest));

    /// <summary>
    /// The logger shared by all requests.
    /// </summary>
    private readonly ILogger _logger = loggerFactory.CreateLogger("ODC.Web.Messaging");

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var response = await inner.HandleAsync(request, cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        if (response is IOutcome { Error: { } error })
        {
            LogFailed(_logger, RequestName, error.Code, elapsed);
        }
        else
        {
            LogHandled(_logger, RequestName, elapsed);
        }

        return response;
    }

    /// <summary>
    /// Logs a handled request.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="request">The request name.</param>
    /// <param name="elapsed">The duration in milliseconds.</param>
    [LoggerMessage(Level = LogLevel.Debug, Message = "{Request} handled in {Elapsed:0.0} ms")]
    private static partial void LogHandled(ILogger logger, string request, double elapsed);

    /// <summary>
    /// Logs a request that ended with an error result.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="request">The request name.</param>
    /// <param name="code">The error code.</param>
    /// <param name="elapsed">The duration in milliseconds.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "{Request} failed with {Code} in {Elapsed:0.0} ms")]
    private static partial void LogFailed(ILogger logger, string request, string code, double elapsed);
}
