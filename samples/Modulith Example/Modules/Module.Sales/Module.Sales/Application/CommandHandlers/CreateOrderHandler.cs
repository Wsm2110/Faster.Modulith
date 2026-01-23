using FluentValidation;
using Faster.Modulith.Contracts;

namespace Module.Sales.Application.CommandHandlers
{
    /// <summary>
    /// The "Scribe" responsible for permanently saving the Order to the database.
    /// <para>
    /// <b>Why exists?</b> This is the final step in the chain. After we have checked stock and calculated the price,
    /// we need a dedicated handler to write the result to the database. This separates "Calculation" logic from "Storage" logic.
    /// </para>
    /// </summary>
    internal class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Result>
    {
        /// <summary>
        /// Persists the order data.
        /// </summary>
        public async ValueTask<Result> Handle(CreateOrderCommand command, CancellationToken ct)
        {
            // HOW: In a real app, this is where Entity Framework Core or Dapper code lives.
            // _dbContext.Orders.Add(new Order(command.OrderId, ...));
            // await _dbContext.SaveChangesAsync(ct);

            // SIMULATION: Printing to console to mimic a database insert.
            // BUG WATCH: Notice we access 'command.Amount' here, but the constructor below sets 'FinalPrice'.
            // If 'Amount' is never set, this will print nothing or null!
            Console.WriteLine($"[Sales DB] Saving Order {command.OrderId} for Customer {command.CustomerId} at ${command.Amount}");

            return Result.Success;
        }
    }

    /// <summary>
    /// The data packet containing the final, approved values to be saved.
    /// </summary>
    internal record CreateOrderCommand : ICommand<Result>
    {
        public CreateOrderCommand(object orderId, object finalPrice, object customerId)
        {
            OrderId = orderId;
            FinalPrice = finalPrice;
            CustomerId = customerId;
            // CRITICAL BUG: 'Amount' is never assigned here! 
            // In the Handler above, 'command.Amount' will likely be null/zero. 
            // We should probably assign: Amount = finalPrice;
        }

        // CRITIQUE: Weak Typing.
        // Using 'object' for OrderId and CustomerId removes compile-time safety. 
        // Use 'Guid' for IDs and 'decimal' for money.
        public object OrderId { get; internal set; }
        public object CustomerId { get; internal set; }

        // CRITIQUE: Duplicate Concepts? 
        // We have both 'Amount' and 'FinalPrice'. This usually happens when code evolves and 
        // two developers use different names for the same thing. Pick one!
        public object Amount { get; internal set; }
        public object FinalPrice { get; }
    }

    /// <summary>
    /// The Guard Clause.
    /// </summary>
    internal class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderValidator()
        {
            // Example: RuleFor(c => c.FinalPrice).GreaterThan(0);
        }
    }
}