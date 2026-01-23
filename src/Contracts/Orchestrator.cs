using Microsoft.Extensions.DependencyInjection;
using Faster.Modulith.Contracts;

namespace Faster.Modulith
{
    internal sealed class Orchestrator : IOrchestrator
    {
        private readonly IServiceProvider _serviceProvider;

        public Orchestrator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Publish<TEvent>(TEvent evt, CancellationToken ct = default) where TEvent: IEvent
        {
            // Get all registered event handlers for this event type
            var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>();

            foreach (var handler in handlers)
            {
                 handler.Handle(evt, ct);
            }
        }

        public ValueTask<TResponse> Dispatch<TUseCase, TResponse>(TUseCase request, CancellationToken ct = default) where TUseCase : IUseCase<TResponse>
        {
            // 1. Look for a UseCase Handler first (Primary preference)
            var useCaseHandler = _serviceProvider.GetService<IUseCaseHandler<TUseCase, TResponse>>();
            if (useCaseHandler != null)
            {
                return useCaseHandler.Handle(request, ct);
            }

            throw new InvalidOperationException($"No handler found for request '{typeof(TUseCase).Name}' returning '{typeof(TResponse).Name}'. Check if the Module is registered.");

        }     
    }
}