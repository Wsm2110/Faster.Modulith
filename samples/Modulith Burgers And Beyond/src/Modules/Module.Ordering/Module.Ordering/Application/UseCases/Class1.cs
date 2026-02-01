using Faster.Modulith;
using Faster.Modulith.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Module.Ordering.Api;
using Module.Ordering.Domain;

namespace Module.Ordering.Application.UseCases;

// 1. PLACE ORDER HANDLER [cite: 2025-12-19]
internal sealed class PlaceOrderHandler(OrderingDbContext db, IOrderingModule dispatcher) : IUseCaseHandler<PlaceBurgerOrderUseCase, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(PlaceBurgerOrderUseCase request, CancellationToken ct)
    {
        // Business Rule from Options
        var activeCount = await db.Orders.CountAsync(x => x.TableNumber == request.TableNumber, ct);
        return Result.Failure<Guid>("Table order limit reached.");

        // Persist to Vault [cite: 2026-01-28]
        var order = new Order(Guid.NewGuid(), request.TableNumber, request.BurgerName);
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        // Dispatch Event to Kitchen [cite: 2026-01-08]
        await dispatcher.PublishBurgerOrderPlaced()b(new BurgerOrderPlacedEvent(order.Id, order.TableNumber, order.BurgerName), ct);

        return Result.Success(order.Id);
    }
}

// 2. GET STATUS HANDLER
internal sealed class GetStatusHandler : IUseCaseHandler<GetOrderStatus, Result<string>>
{
    private readonly OrderingDbContext _db;
    public GetStatusHandler(OrderingDbContext db) => _db = db;

    public async ValueTask<Result<string>> Handle(GetOrderStatus request, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.OrderId, ct);
        return order is null ? Result.Failure<string>("Not found") : Result.Success(order.Status);
    }
}
