using FluentValidation;
using Faster.Modulith.Contracts;
using Module.Sales.Api; // Access to the public Request object
using Faster.Modulith;
using Module.Sales.Application.CommandHandlers; // Access to internal Commands

namespace Module.Sales.Application.UseCases;

/// <summary>
/// The "Manager" class. It coordinates the entire order placement process.
/// <para>
/// <b>Why exists?</b> This is an <i>Orchestrator</i>. In complex logic, we don't put 500 lines of code 
/// in one method. Instead, we break the work into small steps (Check Stock, Calc Price, Save) 
/// and this class just tells them when to run, like a conductor leading an orchestra.
/// </para>
/// </summary>
/// <remarks>
/// <b>Primary Constructor:</b> We use the modern C# 12 syntax <c>(ISalesDispatcher...)</c> 
/// to inject dependencies directly. It's cleaner than writing a verbose constructor.
/// </remarks>
internal class PlaceOrderHandler(ISalesDispatcher dispatcher, IShippingModule shippingModule)
    : IUseCaseHandler<PlaceOrderUseCase, Result<Guid>>
{
    /// <summary>
    /// Executes the workflow: Stock -> Price -> Save -> Notify.
    /// </summary>
    public async ValueTask<Result<Guid>> Handle(PlaceOrderUseCase useCase, CancellationToken ct)
    {
        // STEP 1: Dispatch Check Stock
        // HOW: We create a specialized internal command (`CheckStockCommand`) and pass it to the dispatcher.
        // WHY: The "PlaceOrderHandler" shouldn't know SQL queries for checking stock. 
        // By delegating this to a specific handler, we respect the "Single Responsibility Principle".
        var stockResult = await dispatcher.CheckStock(new CheckStockCommand(useCase.ProductId, useCase.Quantity), ct);

        // WHY Failure Check?
        // We use the "Fail Fast" approach. If step 1 fails, we stop immediately. 
        // We return a "Result" instead of throwing Exceptions because "Out of Stock" is a 
        // normal business scenario, not a system crash.
        if (stockResult.IsFailure)
        {
            return Result<Guid>.Failure("Out of Stock");
        }

        // STEP 2: Dispatch Pricing
        // HOW: We pipe the output of one step (or input) into the next step.
        // Note: 'dispatcher' here is strictly for *internal* calls within the Sales module.
        var price = await dispatcher.CalculatePrice(new CalculatePriceCommand(useCase.ProductId, useCase.Quantity), ct);

        // STEP 3: Dispatch Save
        // HOW: Now that we have valid stock and a calculated price, we persist the data.
        var saveResult = await dispatcher.SaveOrder(new SaveOrderCommand(useCase.OrderId, price, useCase.CustomerId), ct);
        if (!saveResult.IsSuccess)
        {
            return Result<Guid>.Failure(saveResult.Error);
        }

        // STEP 4: Announce to World (Shipping/Notifications)
        // HOW: We use a specific Interface (IShippingApi) to talk to the Shipping Module.
        // WHY: This is the "Airlock". We don't call 'ShippingHandler' directly. 
        // We go through a strictly defined interface. This ensures that if we move Shipping 
        // to a microservice later, only the implementation of 'IShippingApi' needs to change.
        shippingModule.PublishOrderPlaced(useCase.OrderId, useCase.CustomerId, "FedEx");

        return Result<Guid>.Success(useCase.OrderId);
    }
}

/// <summary>
/// The Guard Clause.
/// <para>
/// <b>Why here?</b> This runs *before* the Handler. It ensures that the Handler never 
/// has to waste time dealing with a null `OrderId` or negative `Quantity`.
/// </para>
/// </summary>
public class PlaceOrderValidator : AbstractValidator<PlaceOrderUseCase>
{
    public PlaceOrderValidator()
    {
        // Example: RuleFor(c => c.Quantity).GreaterThan(0);
    }
}