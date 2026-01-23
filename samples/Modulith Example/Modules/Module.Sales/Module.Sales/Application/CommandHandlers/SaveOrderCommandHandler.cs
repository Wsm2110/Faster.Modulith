using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Faster.Modulith.Contracts;

// NOTICE: We are in the 'Sales' Application layer.
// This handler deals with the specific task of persisting (saving) a finalized order.
namespace Module.Sales.Application.CommandHandlers
{
    /// <summary>
    /// The "Archivist" of the Sales module. It takes a complete order and writes it to permanent storage.
    /// <para>
    /// <b>Why exists?</b> After the Orchestrator (PlaceOrderHandler) has coordinated checking stock 
    /// and calculating the price, it needs a final step to commit that transaction to the database.
    /// separating this logic keeps the Orchestrator clean—it doesn't need to know SQL.
    /// </para>
    /// </summary>
    internal class SaveOrderCommandHandler : ICommandHandler<SaveOrderCommand, Result>
    {
        /// <summary>
        /// Writes the order data to the database.
        /// </summary>
        public async ValueTask<Result> Handle(SaveOrderCommand command, CancellationToken ct)
        {
            // Internal Command Handler

            // HOW: In production, this would use Entity Framework:
            // _dbContext.Orders.Add(new OrderEntity(command.OrderId, command.TotalPrice));
            // await _dbContext.SaveChangesAsync(ct);

            // BUG ALERT:
            // The code tries to access `command.TotalPrice`, but looking at the Record below, 
            // `TotalPrice` is an *internal field* that is NEVER assigned in the constructor!
            // It assigns `Price` instead. This line will likely print "$null" or "$0".
            Console.WriteLine($"[Sales DB] Saved Order {command.OrderId} Value: ${command.TotalPrice}");

            return Result.Success;
        }
    }
}

/// <summary>
/// The data packet containing the final details to be saved.
/// </summary>
internal record SaveOrderCommand : ICommand<Result>
{
    // CRITIQUE: Fields vs Properties.
    // These are 'internal fields' (variables), not properties. 
    // This makes them harder to serialize/debug and inconsistent with standard C# practices.
    internal object OrderId;
    internal object TotalPrice; // <-- This field is defined but never set!

    /// <summary>
    /// Initializes the command with data from the Orchestrator.
    /// </summary>
    /// <param name="orderId">The ID generated at the start of the process.</param>
    /// <param name="price">The result object from the Pricing calculation.</param>
    /// <param name="customerId">The buyer.</param>
    public SaveOrderCommand(Guid orderId, Result<decimal> price, object customerId)
    {
        // BUG SOURCE: We are assigning to the 'OrderId' field properly...
        OrderId = orderId;

        // ...but here we assign 'price' to the 'Price' property...
        Price = price;

        // ...leaving the 'TotalPrice' field (used in the Handler) completely empty/null!

        CustomerId = customerId;
    }

    // WHY Result<decimal>? 
    // Ideally, we should unpack the value (decimal) *before* putting it into this command.
    // Passing a 'Result' object into a Command implies we might be passing an Error into the DB layer,
    // which is confusing. Better to pass 'decimal price' directly.
    public Result<decimal> Price { get; }

    public object CustomerId { get; }
}

/// <summary>
/// The Guard Clause.
/// </summary>
internal class SaveOrderValidator : AbstractValidator<SaveOrderCommand>
{
    public SaveOrderValidator()
    {
        // Example: RuleFor(c => c.Price.Value).GreaterThan(0);
    }
}