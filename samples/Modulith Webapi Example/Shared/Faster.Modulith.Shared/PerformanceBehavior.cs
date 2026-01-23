using System.Diagnostics;
using Faster.Modulith.Contracts;
using Microsoft.Extensions.Logging;

namespace Faster.Modulith.Behaviors;

/// <summary>
/// Tracks performance metrics for requests
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IMetricsCollector _metrics;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly long _warningThresholdMs;

    public PerformanceBehavior(
        IMetricsCollector metrics,
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
        long warningThresholdMs = 3000)
    {
        _metrics = metrics;
        _logger = logger;
        _warningThresholdMs = warningThresholdMs;
    }

    public async ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            _metrics.RecordSuccess(requestName, sw.ElapsedMilliseconds);

            if (sw.ElapsedMilliseconds > _warningThresholdMs)
            {
                _logger.LogWarning(
                    "Long running request {RequestName} took {ElapsedMs}ms",
                    requestName,
                    sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (System.Exception ex)
        {
            sw.Stop();
            _metrics.RecordFailure(requestName, sw.ElapsedMilliseconds, ex);
            throw;
        }
    }
}