using Faster.Modulith.Contracts;
using Microsoft.EntityFrameworkCore;
using Module.Ordering.Api.UseCases;
using Module.Ordering.Infrastructure;

namespace Module.Ordering.Application.UseCases;

/// <summary>
/// Handles requests to update the status of an order.
/// </summary>
/// <remarks>If the specified order does not exist, the handler returns a failure result. An
/// InvalidOperationException may be thrown if the status update operation fails.</remarks>
/// <param name="db">The database context used to access and modify order data.</param>
[Expose("api/v1/orders/update")] //Note: Automatically generates a minimalistic Api endpoint
internal sealed class UpdateStatusHandler(OrderingDbContext db) : IUseCaseHandler<UpdateOrderStatusUseCase, Result>
{
    /// <summary>
    /// Handles the update of an order's status based on the provided request.
    /// </summary>
    /// <remarks>If the order is not found, the method returns a failure result. An InvalidOperationException
    /// may be thrown if the status update fails.</remarks>
    /// <param name="request">The request containing the order ID and the new status to be applied to the order.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the asynchronous operation to complete.</param>
    /// <returns>A result indicating the success or failure of the operation, with a message in case of failure.</returns>
    public async ValueTask<Result> Handle(UpdateOrderStatusUseCase request, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == request.OrderId, ct);
        if (order is null) return Result.Failure("Order not found.");

        try
        {
            order.UpdateStatus(request.NewStatus);
            await db.SaveChangesAsync(ct);
            return Result.Success;
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

