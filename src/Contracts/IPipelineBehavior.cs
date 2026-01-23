using System.Threading;
using System.Threading.Tasks;

namespace Faster.Modulith.Contracts;

// 1. The Delegate: Represents the "next" step in the pipeline
public delegate ValueTask<TResult> RequestHandlerDelegate<TResult>();

// 2. The Interface: Middleware must implement this
public interface IPipelineBehavior<in TRequest, TResult> where TRequest : notnull
{
    /// <summary>
    /// Handles the request and decides whether to call the next step.
    /// </summary>
    /// <param name="request">The command/query object.</param>
    /// <param name="next">The delegate to call the next behavior or the final handler.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result.</returns>
    ValueTask<TResult> Handle(
        TRequest request,
        RequestHandlerDelegate<TResult> next,
        CancellationToken ct);
}
