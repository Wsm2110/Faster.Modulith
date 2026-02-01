using Faster.Modulith.Contracts;
using Module.Ordering.Api.UseCases;
using Module.Ordering.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Module.Ordering.Application.UseCases;

internal sealed class CancelBurgerOrderHandler(OrderingDbContext db) : IUseCaseHandler<CancelBurgerOrderUseCase, Result>
{
    public async ValueTask<Result> Handle(CancelBurgerOrderUseCase request, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([request.OrderId], ct);
        if (order == null)
        {
            return Result.Failure("Order not found.");
        }

        order.Cancel(request.Reason);

        await db.SaveChangesAsync(ct);

        Console.WriteLine($"[{DateTime.UtcNow}]: Order {request.OrderId} cancelled.");

        return Result.Success;
    }
}
