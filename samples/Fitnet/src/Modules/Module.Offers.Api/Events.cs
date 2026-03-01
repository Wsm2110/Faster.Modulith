using Faster.Modulith.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Offers.Api;

/// <summary>
/// Event triggered when a pass has been successfully marked as expired.
/// This event originates from the Passes module and acts as the trigger for offer preparation.
/// </summary>
/// <param name="PassId">The unique identifier of the expired pass.</param>
/// <param name="CustomerId">The associated customer identifier.</param>
public record PassExpiredEvent(Guid PassId, Guid CustomerId) : IEvent;

/// <summary>
/// Event triggered when a new offer has been successfully prepared.
/// </summary>
/// <param name="OfferId">The unique identifier of the created offer.</param>
/// <param name="CustomerId">The associated customer identifier.</param>
/// <param name="FinalPrice">The final calculated price of the offer.</param>
public record OfferPreparedEvent(Guid OfferId, Guid CustomerId, decimal FinalPrice) : IEvent;