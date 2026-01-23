using Faster.Modulith.Contracts;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Faster.Modulith.Behaviors;

/// <summary>
/// Logs execution time and request/response details
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Executing {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();

            _logger.LogInformation(
                "Completed {RequestName} in {ElapsedMs}ms",
                requestName,
                sw.ElapsedMilliseconds);

            return response;
        }
        catch (System.Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "Failed {RequestName} after {ElapsedMs}ms",
                requestName,
                sw.ElapsedMilliseconds);
            throw;
        }
    }
}

// ==========================================
// VALIDATION BEHAVIOR
// ==========================================