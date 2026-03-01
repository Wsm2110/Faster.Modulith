using Faster.Modulith.Contracts;
using Module.Membership.Api;
using Module.Membership.Api.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Membership.Application.UseCases;

/// <summary>
/// Executes the membership signing logic directly within the use case boundary.
/// </summary>
/// <param name="eventBus">The event bus used for publishing domain events.</param>
internal class SignMembershipUseCaseHandler(IMembershipDispatcher dispatcher) : IUseCaseHandler<SignMembershipUseCase, Result>
{
    /// <summary>
    /// Processes the use case request to sign the membership and trigger subsequent events.
    /// </summary>
    /// <param name="useCase">The use case parameters.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A value task representing the asynchronous operation, containing the result.</returns>
    public async ValueTask<Result> Handle(SignMembershipUseCase useCase, CancellationToken ct)
    {
        Console.WriteLine($"[{DateTime.UtcNow:O}] Verifying signature reference {useCase.SignatureReference} for membership {useCase.MembershipId}.");

        if (string.IsNullOrWhiteSpace(useCase.SignatureReference))
        {
            Console.WriteLine($"[{DateTime.UtcNow:O}] Validation failed: Signature reference is missing.");
            return Result.Failure("Signature reference must be provided.");
        }

        // Simulating aggregate retrieval
        var customerId = Guid.NewGuid();
        Console.WriteLine($"[{DateTime.UtcNow:O}] Membership {useCase.MembershipId} successfully signed by customer {customerId}.");

        var signedEvent = new MembershipSignedEvent(useCase.MembershipId, customerId);
        await dispatcher.PublishMembershipSignedAsync(signedEvent);

        Console.WriteLine($"[{DateTime.UtcNow:O}] MembershipSignedEvent published for membership {useCase.MembershipId}.");

        return Result.Success;
    }
}