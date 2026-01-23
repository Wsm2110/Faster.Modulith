using FluentValidation; // Used for validating the command before the handler runs.
using Faster.Modulith.Contracts; // Access to ICommandHandler, Result, etc.

namespace Module.Shipping.Application.CommandHandlers;

/// <summary>
/// The "Worker" class that actually performs the logic when someone wants to Place an Order.
/// <para>
/// <b>Why exists?</b> This follows the <i>Command Pattern</i>. Instead of a giant "OrderService" with 50 methods,
/// we have one small class dedicated to doing exactly one thing: Placing an Order.
/// </para>
/// </summary>
/// <remarks>
/// <b>Why Internal?</b> This is the "How". The public API only exposes the <i>Intent</i> (the Command),
/// but the actual execution logic is hidden inside this module.
/// </remarks>
internal class PlaceOrderCommandHandler() : ICommandHandler<PlaceOrderCommand, Result>
{
    /// <summary>
    /// The entry point for the logic.
    /// </summary>
    /// <param name="command">The data payload (Inputs) required to do the work.</param>
    /// <param name="ct">Cancellation Token to stop work if the request is aborted.</param>
    /// <returns>A Result indicating success or failure.</returns>
    public async ValueTask<Result> Handle(PlaceOrderCommand command, CancellationToken ct)
    {
        // 1. Cross-Module Query (The "Orchestration" step)
        // HOW: We don't calculate prices here because that is the responsibility of the 'Pricing' module.
        // WHY: If we calculated price here, we would duplicate logic. By asking the Pricing module via the Orchestrator,
        // we respect the "Single Source of Truth". Even if the Pricing logic changes (e.g., Black Friday sales),
        // this Shipping code remains untouched.
        // var quote = await orchestrator.Execute(new CalculateQuoteQuery(...));

        // 2. Domain Logic ( The "Work" step)
        // HOW: We take the inputs and the external data (quote) to change the state of our system.
        // In a real app, this is where you would save to the database: _repository.Add(newOrder);
        var orderId = command.OrderId;

        // Console.WriteLine($"[Sales] Order {orderId} confirmed...");

        // 3. Publish Fact (The "Notification" step)
        // HOW: We broadcast an event ("OrderPlacedEvent") to the rest of the system.
        // WHY: This decouples "Shipping" from "Sales". The Shipping module (this code) doesn't need to know *who* is listening.
        // It just shouts "Order Placed!" and lets other modules (Inventory, Notifications) react if they care.
        // await orchestrator.PublishAsync(new OrderPlacedEvent(...));

        return Result.Success;
    }
}

/// <summary>
/// A simple data container (DTO) that holds the *Inputs* for the handler.
/// </summary>
/// <remarks>
/// <b>Why a Record?</b> Records are immutable by default. This ensures that once the command is created (e.g., by the API),
/// the data cannot be accidentally changed halfway through the pipeline. It guarantees data integrity.
/// </remarks>
internal record PlaceOrderCommand : ICommand<Result>
{
    public object OrderId { get; internal set; }
}

/// <summary>
/// The "Bouncer" at the door. It checks if the data is valid *before* the Handler even tries to run.
/// <para>
/// <b>Why separate?</b> If we put `if (id == 0) throw...` inside the Handler, the Handler gets cluttered with checks.
/// By separating it, the Handler can assume all data is valid and focus purely on business logic.
/// </para>
/// </summary>
internal class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        // HOW: We define rules using a fluent interface. "Fluent" means it reads like a sentence.
        // "Rule for Id: It must not be equal to 0. If it is, show this message."
        // RuleFor(c => c.OrderId).NotEqual(0).WithMessage("Id cannot be 0");
    }
}