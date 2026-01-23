using Faster.Modulith;
using Faster.Modulith.Contracts; // Access to standard interfaces like ICommandHandler
using FluentValidation; // Access to the validation framework

// NOTICE: We are in the 'Shipping' namespace. This code is physically isolated from Sales.
namespace Module.Shipping.Application.CommandHandlers;

/// <summary>
/// The specialized "Worker" responsible for communicating with external Shipping Carriers (FedEx/UPS).
/// <para>
/// <b>Why exists?</b> This encapsulates the complexity of 3rd party integrations. 
/// The rest of the system just says "Book Carrier", and this class handles the messy details of HTTP calls, retries, or API keys.
/// </para>
/// </summary>
/// <remarks>
/// <b>Why Internal?</b> This is an implementation detail. Other modules (like Sales) do not need to know 
/// <i>which</i> carrier we use or <i>how</i> we book it. They just need to know it got done.
/// </remarks>
internal class BookCarrierCommandHandler : ICommandHandler<BookCarrierCommand, Result<string>>
{
    /// <summary>
    /// Executes the booking logic.
    /// </summary>
    /// <param name="command">The immutable request data (OrderId, Address).</param>
    /// <param name="ct">Cancellation token to abort the external HTTP call if the user cancels.</param>
    /// <returns>A Result containing the new Tracking Number.</returns>
    public async ValueTask<Result<string>> Handle(BookCarrierCommand command, CancellationToken ct)
    {
        // HOW: In a real app, this would be: 
        // var response = await _httpClient.PostAsync("https://api.fedex.com/ship", payload, ct);

        // SIMULATION: We use Task.Delay to mimic the network latency of a real HTTP call.
        await Task.Delay(100);

        // LOGIC: Generate a fake tracking number based on the Order ID.
        // We use string interpolation ($"...") for clean formatting.
        var trackingNumber = $"TRK-{command.OrderId.ToString().Substring(0, 8).ToUpper()}";

        // SIDE EFFECT: We log to the console to prove something happened. 
        // In production, this would be structured logging (Serilog) or saving to a database.
        Console.WriteLine($"[Shipping] Booked shipment for {command.OrderId} to {command.Address}. Tracking: {trackingNumber}");

        // WHY Result<T>?
        // Instead of throwing an exception if the carrier is down, we could return Result.Failure("Carrier Unavailable").
        // This makes flow control explicit and safer.
        return Result<string>.Success(trackingNumber);
    }
}

/// <summary>
/// The Data Transfer Object (DTO) defining *what* is needed to book a carrier.
/// </summary>
/// <remarks>
/// <b>Why Record?</b> Records are immutable (cannot change). This guarantees that the Address 
/// doesn't get accidentally modified between the time the command was sent and the time it is handled.
/// </remarks>
internal record BookCarrierCommand : ICommand<Result<string>>
{
    // WHY 'object'? (In this demo). In a real app, 'OrderId' would likely be a Guid 
    // and 'Address' would be a strongly-typed 'Address' object or record.
    public object OrderId { get; internal set; }
    public object Address { get; internal set; }
}

/// <summary>
/// The "Quality Control" check.
/// <para>
/// <b>Why separate?</b> We want to fail fast. If the OrderId is missing, there is no point in 
/// starting the Handler or opening an HTTP connection. This keeps the Handler code clean and focused on the "Happy Path".
/// </para>
/// </summary>
internal class BookCarrierValidator : AbstractValidator<BookCarrierCommand>
{
    public BookCarrierValidator()
    {
        // Example Rule:
        // RuleFor(c => c.OrderId).NotNull().WithMessage("Order ID is required to book a shipment.");
    }
}