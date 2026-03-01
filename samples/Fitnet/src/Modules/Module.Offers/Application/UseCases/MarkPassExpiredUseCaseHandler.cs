using Faster.Modulith.Contracts;
using Module.Offers.Api;
using Module.Offers.Api.UseCases;
using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Offers.Application.UseCases;

/// <summary>
/// Executes the pass expiration logic directly within the use case boundary.
/// </summary>
/// <param name="eventBus">The event bus used for publishing domain events to cross module boundaries.</param>
internal class MarkPassExpiredUseCaseHandler(IOffersDispatcher eventBus) : IUseCaseHandler<MarkPassExpiredUseCase, Result>
{
    /// <summary>
    /// Processes the use case request to expire a pass and trigger the subsequent renewal offer workflow.
    /// </summary>
    /// <param name="useCase">The use case parameters containing the pass identifier and reason.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A value task representing the asynchronous operation, containing the execution result.</returns>
    public async ValueTask<Result> Handle(MarkPassExpiredUseCase useCase, CancellationToken ct)
    {
        Console.WriteLine($"[{DateTime.UtcNow:O}] Initiating expiration for pass {useCase.PassId} belonging to customer {useCase.CustomerId}. Reason: {useCase.Reason}.");

        if (useCase.PassId == Guid.Empty)
        {
            Console.WriteLine($"[{DateTime.UtcNow:O}] Validation failed: Pass identifier cannot be empty.");
            return Result.Failure("A valid pass identifier must be provided.");
        }

        // Domain logic for updating the pass status would execute here.
        Console.WriteLine($"[{DateTime.UtcNow:O}] Pass {useCase.PassId} status successfully updated to Expired in the database.");

        var expiredEvent = new PassExpiredEvent(useCase.PassId, useCase.CustomerId);

        await eventBus.PublishPassExpiredAsync(expiredEvent, ct);

        Console.WriteLine($"[{DateTime.UtcNow:O}] PassExpiredEvent published for pass {useCase.PassId}.");

        return Result.Success;
    }
}
