using Faster.Modulith.Contracts;
using Module.Ordering.Api.UseCases;
using Module.Ordering.Domain;
using Module.Ordering.Infrastructure;

namespace Module.Ordering.Application.UseCases;

/// <summary>
/// Handles the placement of a burger order by coordinating domain logic, persisting the order, and dispatching related
/// events within the ordering module.
/// </summary>
/// <remarks>This handler is responsible for orchestrating the end-to-end process of placing a burger order,
/// including auditing actions, saving the order, and notifying other modules such as the kitchen. All actions are
/// logged with UTC timestamps for traceability.</remarks>
/// <param name="db">The database context used to persist burger orders to the internal ordering database. Cannot be null.</param>
/// <param name="dispatcher">The dispatcher responsible for publishing events related to burger order placement. Cannot be null.</param>
[Expose("api/v1/orders/place")]
internal sealed class PlaceOrderHandler(OrderingDbContext db, IOrderingDispatcher dispatcher) : IUseCaseHandler<PlaceBurgerOrderUseCase, Result<Guid>>
{
    /// <summary>
    /// Handles the placement of a burger order by coordinating domain logic, persisting the order, and notifying
    /// relevant modules.
    /// </summary>
    /// <remarks>This method logs the order placement process, saves the order to the database, and dispatches
    /// an event to notify other modules of the order placement.</remarks>
    /// <param name="request">The details of the burger order to be placed, including the table number, burger name, and any special
    /// instructions.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A result containing the unique identifier of the placed order, represented as a GUID.</returns>
    public async ValueTask<Result<Guid>> Handle(PlaceBurgerOrderUseCase request, CancellationToken ct)
    {
        // 2. Domain Logic: Create the rich entity
        // Realistically, the handler orchestrates the domain's rules
        var orderId = Guid.NewGuid();
        var order = new BurgerOrder(
            orderId,
            request.TableNumber,
            request.BurgerName,
            request.SpecialInstructions
        );

        // 3. Persistence: Save to the module-specific, internal database [cite: 2026-01-28]
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        // 4. Orchestration: Dispatch the 'Aftermath' event [cite: 2026-01-08]
        // This is where the UseCase signals the Kitchen and Robotics modules.
        await dispatcher.PublishBurgerOrderPlacedAsync(
            orderId,
            request.TableNumber,
            $"{request.BurgerName} (Instructions: {request.SpecialInstructions})", ct);

        return Result<Guid>.Success(orderId);
    }
}