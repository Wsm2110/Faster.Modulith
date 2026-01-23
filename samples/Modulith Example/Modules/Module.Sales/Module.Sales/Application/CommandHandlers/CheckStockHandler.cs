using FluentValidation;
using Faster.Modulith.Contracts;

namespace Module.Sales.Application.CommandHandlers
{
    /// <summary>
    /// A specialized worker responsible for verifying inventory levels.
    /// <para>
    /// <b>Why exists?</b> This follows the <i>Single Responsibility Principle</i>. 
    /// The "PlaceOrderHandler" (the orchestrator) shouldn't know how to query the inventory database. 
    /// It delegates that specific question to this handler.
    /// </para>
    /// </summary>
    internal class CheckStockHandler : ICommandHandler<CheckStockCommand, Result<bool>>
    {
        /// <summary>
        /// Executes the stock check.
        /// </summary>
        /// <param name="command">The request containing the Item and Quantity needed.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A Result containing 'true' if stock exists, or 'false' otherwise.</returns>
        public async ValueTask<Result<bool>> Handle(CheckStockCommand command, CancellationToken ct)
        {
            // HOW: In a real application, this would query a database table or call a separate 'Inventory' module.
            // var stockItem = await _repository.GetAsync(command.ProductId);

            // SIMULATION: We assume everything is always in stock for this demo.
            bool inStock = true;

            // WHY Result<bool>?
            // Returning 'bool' is okay, but 'Result<bool>' is better because it allows us to return 
            // a specific error message if something breaks (e.g., "Database Unavailable") 
            // rather than just "false", which might be ambiguous.
            return Result<bool>.Success(inStock);
        }
    }

    /// <summary>
    /// The data packet representing the question: "Do we have this item?"
    /// </summary>
    internal record CheckStockCommand : ICommand<Result<bool>>
    {
        public CheckStockCommand(object productId, object quantity)
        {
            ProductId = productId;
            Quantity = quantity;
        }

        // CRITIQUE: Primitive Obsession / Weak Typing.
        // Using 'object' is dangerous because you could pass a Date into 'Quantity' and the compiler wouldn't complain.
        // Junior Dev Tip: Always use specific types like 'Guid' for IDs and 'int' or 'decimal' for quantities.
        public object ProductId { get; }
        public object Quantity { get; }
    }

    /// <summary>
    /// The Guard Clause.
    /// </summary>
    internal class CheckStockValidator : AbstractValidator<CheckStockCommand>
    {
        public CheckStockValidator()
        {
            // Example: We should verify we aren't asking for negative stock.
            // RuleFor(c => c.Quantity).GreaterThan(0).WithMessage("Must check for at least 1 item.");
        }
    }
}