using Faster.Modulith.Contracts;
using Module.Ordering.Api.Events;
using Module.Kitchen.Domain;
using Module.Kitchen.Infrastructure;

namespace Module.Kitchen.Application.EventHandlers
{
    /// <summary>
    /// Handles burger order placement events by creating and persisting kitchen tickets.
    /// </summary>
    /// <remarks>This event handler listens for burger order placement events and maps the event data to a
    /// kitchen ticket, which is then saved to the kitchen's database. It also logs the receipt and processing of each
    /// ticket for auditing purposes.</remarks>
    /// <param name="db">The database context used to store kitchen tickets.</param>
    internal class CreateTicketEventHandler(KitchenDbContext db) : IEventHandler<BurgerOrderPlacedEvent> // Note cross module event call
    {
        /// <summary>
        /// Handles a burger order placement event by creating a kitchen ticket and saving it to the database.
        /// </summary>
        /// <remarks>This method logs the receipt of the order and the creation of a kitchen ticket. The
        /// ticket is persisted to the kitchen's database asynchronously.</remarks>
        /// <param name="event">The event containing details of the burger order, including the order identifier, table number, and order
        /// summary.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation of handling the burger order placement event.</returns>
        public async ValueTask Handle(BurgerOrderPlacedEvent @event, CancellationToken ct)
        {
            // 1. Audit Log - Mandatory UTC timestamp
            Console.WriteLine($"[{DateTime.UtcNow}]: KITCHEN ALERT - Received Ticket for Table {@event.TableNumber}");

            // 2. Logic: Map the Event data to the Kitchen's private Domain model
            // Notice we don't use the 'BurgerOrder' from Ordering; we use 'KitchenTicket'
            var ticket = new KitchenTicket(
                @event.OrderId,
                @event.TableNumber,
                @event.Summary
            );

            // 3. Persist to the Kitchen's private Vault [cite: 2026-01-28]
            db.Tickets.Add(ticket);

            await db.SaveChangesAsync(ct);

            Console.WriteLine($"[{DateTime.UtcNow}]: Kitchen Ticket {@event.OrderId} is now on the grill.");
        }
    }
}
