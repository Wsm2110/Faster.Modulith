using System.Diagnostics;
using Faster.Modulith.Contracts;
using Microsoft.Extensions.Logging;

namespace BurgersAndBeyond.Shared.Behaviors;

/// <summary>
/// Logs execution time and request/response details
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Executing {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();

            logger.LogInformation(
                "Completed {RequestName} in {ElapsedMs}ms",
                requestName,
                sw.ElapsedMilliseconds);

            return response;
        }
        catch (System.Exception ex)
        {
            sw.Stop();
            logger.LogError(
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