using FluentValidation;
using Faster.Modulith.Contracts;

// NOTICE: This resides in 'CommandHandlers'. 
// It is a specific instruction to "Do" something (Calculate), 
// distinguishing it from a "Query" (just fetching data).
namespace Module.Sales.Application.CommandHandlers;

/// <summary>
/// A specialized worker that handles the business rules for pricing.
/// <para>
/// <b>Why exists?</b> By isolating pricing logic here, we ensure that if pricing rules change 
/// (e.g., bulk discounts), we only touch this file. We don't risk breaking the "Save Order" logic.
/// </para>
/// </summary>
internal class CalculatePriceHandler : ICommandHandler<CalculatePriceCommand, Result<decimal>>
{
    /// <summary>
    /// performs the calculation.
    /// </summary>
    public async ValueTask<Result<decimal>> Handle(CalculatePriceCommand command, CancellationToken ct)
    {
        // BUG ALERT: The command.Quantity property is never set in the constructor below (it sets Quantity1).
        // A junior dev needs to watch out for property name mismatches!
        // Assuming we meant to use the actual input:
        // decimal qty = Convert.ToDecimal(command.Quantity1); 

        // DOMAIN LOGIC: 
        // This is "Pure Logic". It doesn't need a database. It takes input -> processes -> returns output.
        // Pure logic is the easiest code to unit test because you don't need to mock a database.
        decimal total = 10.00m * command.Quantity;

        // WHY Decimal?
        // Always use 'decimal' for money. 'double' or 'float' have floating-point errors 
        // that can lose pennies (0.1 + 0.2 != 0.3 in floating point math).
        return Result<decimal>.Success(total);
    }
}

/// <summary>
/// The input data required to perform the calculation.
/// </summary>
internal record CalculatePriceCommand : ICommand<Result<decimal>>
{
    public CalculatePriceCommand(object productId, object quantity)
    {
        ProductId = productId;
        // CRITIQUE: Confusing Naming. We are setting 'Quantity1' but the Handler might expect 'Quantity'.
        // This creates bugs. Properties should match the intent clearly.
        Quantity1 = quantity;
    }

    // CRITIQUE: This property is never assigned by the constructor! It will be 0.
    public decimal Quantity { get; internal set; }

    // CRITIQUE: Avoid 'object'. Use 'Guid' for IDs.
    public object ProductId { get; }

    // CRITIQUE: Avoid suffixing with numbers (Quantity1). It usually implies a merge conflict or copy-paste error.
    public object Quantity1 { get; }
}

/// <summary>
/// The Guard Clause.
/// </summary>
internal class CalculatePriceValidator : AbstractValidator<CalculatePriceCommand>
{
    public CalculatePriceValidator()
    {
        // Example: RuleFor(c => c.Quantity).GreaterThan(0);
    }
}