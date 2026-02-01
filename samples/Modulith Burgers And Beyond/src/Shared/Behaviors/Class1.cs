namespace BurgersAndBeyond.Shared.Behaviors;

internal sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async ValueTask<TResponse> Handle(TRequest req, CancellationToken ct, RequestHandlerDelegate<TResponse> next)
    {
        Console.WriteLine($"[{DateTime.UtcNow}]: START {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"[{DateTime.UtcNow}]: END {typeof(TRequest).Name}");
        return response;
    }
}