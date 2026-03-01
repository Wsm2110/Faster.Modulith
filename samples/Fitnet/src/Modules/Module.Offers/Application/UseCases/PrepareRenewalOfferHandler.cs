using Faster.Modulith.Contracts;
using Module.Offers.Api;
using Module.Offers.Api.UseCases;
using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Offers.Application.UseCases;

/// <summary>
/// Executes the offer preparation logic directly within the use case boundary.
/// </summary>
/// <param name="eventBus">The event bus used for publishing domain events.</param>
internal class PrepareOfferUseCaseHandler(IOffersDispatcher eventBus) : IUseCaseHandler<PrepareOfferUseCase, Result>
{
    /// <summary>
    /// Processes the use case request to prepare a tailored offer for the customer.
    /// </summary>
    /// <param name="useCase">The use case parameters.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A value task representing the asynchronous operation, containing the result.</returns>
    public async ValueTask<Result> Handle(PrepareOfferUseCase useCase, CancellationToken ct)
    {
        Console.WriteLine($"[{DateTime.UtcNow:O}] Calculating renewal offer for customer {useCase.CustomerId} based on expired pass {useCase.ExpiredPassId}.");

        decimal basePrice = 49.99m;
        decimal loyaltyDiscount = 15.0m;
        decimal finalPrice = basePrice * (1 - (loyaltyDiscount / 100));

        var offerId = Guid.NewGuid();
        Console.WriteLine($"[{DateTime.UtcNow:O}] Offer {offerId} successfully prepared with a {loyaltyDiscount}% discount. Final price: {finalPrice:C}.");

        var preparedEvent = new OfferPreparedEvent(offerId, useCase.CustomerId, finalPrice);        
        await eventBus.PublishOfferPreparedAsync(preparedEvent, ct);

        Console.WriteLine($"[{DateTime.UtcNow:O}] OfferPreparedEvent published for offer {offerId}.");

        return Result.Success;
    }
}
