using Module.Shipping.Api.Events; // Access to the Public Contract (The Event definition)
using Faster.Modulith.Contracts; // Access to the Framework Interfaces (IEventHandler)

namespace Module.Shipping.Application.EventHandlers
{
    /// <summary>
    /// Reacts to the 'OrderPlaced' event by initiating the shipping process.
    /// <para>
    /// <b>Why exists?</b> In a decoupled system, the 'Sales' module shouldn't tell 'Shipping' what to do directly. 
    /// Instead, Sales publishes an <i>Event</i> ("Order Placed"), and this Handler listens for it. 
    /// This keeps modules independent.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <b>Why Internal?</b> This class is <c>internal</c> because it is an implementation detail of the Shipping module. 
    /// No outside code needs to know <i>how</i> shipping handles orders, only that the event occurred.
    /// </remarks>
    internal class OrderPlacedEventHandler : IEventHandler<OrderPlacedEvent>
    {
        /// <summary>
        /// The entry point that triggers whenever the event bus publishes <see cref="OrderPlacedEvent"/>.
        /// </summary>
        /// <param name="event">The immutable data carrier containing details like OrderId.</param>
        /// <param name="ct">
        /// A <see cref="CancellationToken"/> used to stop processing if the application is shutting down 
        /// or the request was cancelled. Always pass this to async DB calls!
        /// </param>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
        public ValueTask Handle(OrderPlacedEvent @event, CancellationToken ct)
        {
            // HOW: In a real app, this would be a database transaction or an external API call.
            // We use 'Console.WriteLine' here to simulate a side-effect (something changing in the world).
            Console.WriteLine($"[Shipping] Generating generic shipping label for Order {@event.OrderId}...");

            // WHY ValueTask?
            // Event Handlers often run "fire and forget" or complete very quickly. 
            // ValueTask is a lighter-weight alternative to Task that reduces memory allocation overhead 
            // when the result is already known or completes synchronously (like this Console.WriteLine).
            return ValueTask.CompletedTask;
        }
    }
}