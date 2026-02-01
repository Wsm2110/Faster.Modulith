using Faster.Modulith.Contracts;
using Module.Ordering.Domain;
using Faster.Modulith;
using Microsoft.EntityFrameworkCore;
using Module.Ordering.Api.UseCases;

namespace Module.Ordering.Application.UseCases;

/// <summary>
/// Handles the finalization of all active orders for a specified table, marking them as paid and calculating the total
/// amount due.
/// </summary>
/// <remarks>This handler retrieves all active (non-delivered) orders for the given table, marks them as paid, and
/// returns the total amount due. If no active orders are found, the operation fails. After finalization, it dispatches
/// notifications to coordinate further actions in the restaurant workflow.</remarks>
/// <param name="db">The database context used to access and update order information.</param>
/// <param name="dispatcher">The dispatcher responsible for orchestrating subsequent actions after a table's orders are finalized, such as
/// notifying other systems or services.</param>
internal sealed class FinalizeTableHandler(OrderingDbContext db, IOrderingDispatcher dispatcher) : IUseCaseHandler<FinalizeTableOrderUseCase, Result<decimal>>
{
    public async ValueTask<Result<decimal>> Handle(FinalizeTableOrderUseCase request, CancellationToken ct)
    {
        // 1. Audit Entry [cite: 2026-01-29]
        Console.WriteLine($"[{DateTime.UtcNow}]: Orchestrating finalization for Table {request.TableNumber}");

        // 2. Logic: Fetch all active orders for the table
        var orders = await db.Orders
            .Where(x => x.TableNumber == request.TableNumber && x.Status != OrderStatus.Delivered)
            .ToListAsync(ct);

        if (!orders.Any())
        {
            return Result<decimal>.Failure("No active orders found.");
        }

        // 3. Command: Update all items to 'Finalized'
        decimal total = 0;
        foreach (var order in orders)
        {
            total += order.TotalPrice;
            order.MarkAsPaid();
        }

        await db.SaveChangesAsync(ct);

        // 4. Dispatch: Orchestrate the next steps in the restaurant [cite: 2026-01-08]
        // This notifies Kitchen to clear the screen and Robotics to clean the table
        dispatcher.PublishTableFinalized(request.TableNumber, total, ct);

        return Result<decimal>.Success(total);
    }
}

