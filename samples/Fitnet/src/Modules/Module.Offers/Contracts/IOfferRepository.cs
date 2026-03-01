/// <summary>
/// Defines repository operations for commercial offers.
/// </summary>
public interface IOfferRepository
{
    /// <summary>
    /// Saves a new offer record asynchronously.
    /// </summary>
    /// <param name="offerId">The unique identifier for the offer.</param>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="originalPrice">The baseline price before discounts.</param>
    /// <param name="finalPrice">The final price to be offered.</param>
    /// <param name="ct">The cancellation token.</param>
    ValueTask SaveAsync(Guid offerId, Guid customerId, decimal originalPrice, decimal finalPrice, CancellationToken ct);

    /// <summary>
    /// Checks if an offer exists and is currently valid asynchronously.
    /// </summary>
    /// <param name="offerId">The unique identifier for the offer.</param>
    /// <param name="ct">The cancellation token.</param>
    ValueTask<bool> IsOfferValidAsync(Guid offerId, CancellationToken ct);

    /// <summary>
    /// Marks a specific offer as accepted asynchronously.
    /// </summary>
    /// <param name="offerId">The unique identifier for the offer.</param>
    /// <param name="ct">The cancellation token.</param>
    ValueTask MarkAsAcceptedAsync(Guid offerId, CancellationToken ct);
}