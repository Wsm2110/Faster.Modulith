using System;
using System.Threading;
using System.Threading.Tasks;

namespace Faster.Modulith.Contracts;

// --- MARKER INTERFACES ---
public interface IUseCase<TResponse> { }
public interface IUseCase { }
public interface ICommand<TResponse> { }
public interface ICommand { }
public interface IEvent { }

// --- HANDLERS ---
public interface IUseCaseHandler<in TUseCase, TResponse> where TUseCase : IUseCase<TResponse>
{
    ValueTask<TResponse> Handle(TUseCase useCase, CancellationToken ct);
}

public interface IUseCaseHandler<in TUseCase> where TUseCase : IUseCase
{
    ValueTask Handle(TUseCase useCase, CancellationToken ct);
}

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    ValueTask<TResponse> Handle(TCommand command, CancellationToken ct);
}

public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    ValueTask Handle(TEvent @event, CancellationToken ct);
}

// --- ORCHESTRATOR ---
public interface IOrchestrator
{
    // 1. DISPATCH (Fast, Direct)
    ValueTask<TResponse> Dispatch<TUseCase, TResponse>(TUseCase request, CancellationToken ct = default) where TUseCase : IUseCase<TResponse>;

}
