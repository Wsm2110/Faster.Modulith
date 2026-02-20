using Faster.Modulith.Contracts;
using Module.Ordering.Api.UseCases;
using Module.Ordering.Infrastructure;

namespace Module.Ordering.Application.UseCases;

/// <summary>
/// Handles the cancellation of a burger order within the ordering module.
/// </summary>
[Expose("api/v1/orders/cancel")]
internal sealed class CancelBurgerOrderHandler(OrderingDbContext db) : IUseCaseHandler<CancelBurgerOrderUseCase, Result>
{
    public async ValueTask<Result> Handle(CancelBurgerOrderUseCase request, CancellationToken ct)
    {
        // Retrieval of the aggregate root using the provided OrderId
        var order = await db.Orders.FindAsync([request.OrderId], ct);

        if (order == null)
        {
            return Result.Failure("Order not found.");
        }

        // Domain logic is encapsulated within the Order entity to maintain invariants
        order.Cancel(request.Reason);

        await db.SaveChangesAsync(ct);

        // Standardized logging with UTC datetime inclusion
        Console.WriteLine($"[{DateTime.UtcNow}]: Order {request.OrderId} cancelled.");

        return Result.Success;
    }
}