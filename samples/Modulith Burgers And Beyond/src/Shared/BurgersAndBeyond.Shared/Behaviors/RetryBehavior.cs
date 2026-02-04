using Faster.Modulith.Contracts;
using Microsoft.Extensions.Logging;

namespace BurgersAndBeyond.Shared.Behaviors;

/// <summary>
/// Retries failed operations with exponential backoff
/// </summary>
public sealed class RetryBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly int _maxAttempts;
    private readonly ILogger<RetryBehavior<TRequest, TResponse>> _logger;

    public RetryBehavior(
        ILogger<RetryBehavior<TRequest, TResponse>> logger,
        int maxAttempts = 3)
    {
        _maxAttempts = maxAttempts;
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        for (int attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                return await next();
            }
            catch (System.Exception ex) when (attempt < _maxAttempts && IsTransient(ex))
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                _logger.LogWarning(
                    ex,
                    "Attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds}s",
                    attempt,
                    _maxAttempts,
                    delay.TotalSeconds);

                await Task.Delay(delay, ct);
            }
        }

        // This shouldn't be reached, but needed for compilation
        return await next();
    }

    private bool IsTransient(System.Exception ex)
    {
        // Add your transient exception detection logic
        return ex is System.Net.Http.HttpRequestException
               || ex is System.TimeoutException;
    }
}