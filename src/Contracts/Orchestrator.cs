using Microsoft.Extensions.DependencyInjection;
using Faster.Modulith.Contracts;

namespace Faster.Modulith.Contracts;

public sealed class Orchestrator(IServiceProvider serviceProvider) : IOrchestrator
{
    public ValueTask<TResponse> Dispatch<TUseCase, TResponse>(TUseCase request, CancellationToken ct = default) where TUseCase : IUseCase<TResponse>
    {
        // 1. Look for a UseCase Handler first (Primary preference)
        var useCaseHandler = serviceProvider.GetService<IUseCaseHandler<TUseCase, TResponse>>();
        if (useCaseHandler != null)
        {
            return useCaseHandler.Handle(request, ct);
        }

        throw new InvalidOperationException($"No handler found for request '{typeof(TUseCase).Name}' returning '{typeof(TResponse).Name}'. Check if the Module is registered.");

    }
}
