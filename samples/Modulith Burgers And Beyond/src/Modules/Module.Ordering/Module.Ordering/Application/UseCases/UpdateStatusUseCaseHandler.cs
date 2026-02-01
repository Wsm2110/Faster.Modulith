using System;
using System.Collections.Generic;
using System.Text;
using Faster.Modulith.Contracts;
using Microsoft.EntityFrameworkCore;
using Module.Ordering.Api.UseCases;
using Module.Ordering.Domain;

namespace Module.Ordering.Application.UseCases;

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

