using Faster.Modulith;
using Faster.Modulith.Contracts;
using Microsoft.EntityFrameworkCore;
using Module.Kitchen.Infrastructure;
using Module.Kitchen.Api.UseCases;

namespace Module.Kitchen.Application.UseCases
{
    /// <summary>
    /// Handles the completion of a cooking process by updating the kitchen ticket status and notifying relevant modules
    /// when an order is ready.
    /// </summary>
    /// <remarks>This handler retrieves the kitchen ticket associated with the specified order, marks it as
    /// ready, persists the change, and signals other systems that the food is ready for delivery. It also logs the
    /// completion for auditing purposes.</remarks>
    /// <param name="db">The database context used to access and update kitchen ticket information.</param>
    /// <param name="dispatcher">The dispatcher responsible for publishing events related to food readiness.</param>
    internal class CompleteCookingHandler(KitchenDbContext db, IKitchenDispatcher dispatcher) : IUseCaseHandler<CompleteCookingUseCase, Result>
    {
        /// <summary>
        /// Handles the completion of a cooking operation by updating the status of the corresponding kitchen ticket and
        /// notifying downstream systems that the food is ready for delivery.
        /// </summary>
        /// <remarks>This method updates the kitchen ticket status to ready, persists the change, and
        /// publishes an event to signal that the food is ready for delivery. It also logs the completion for auditing
        /// purposes.</remarks>
        /// <param name="request">The use case request containing the order identifier used to locate the kitchen ticket to be completed.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A result indicating whether the operation succeeded. Returns a failure result if the kitchen ticket is not
        /// found.</returns>
        public async ValueTask<Result> Handle(CompleteCookingUseCase request, CancellationToken ct)
        {
            // 1. Fetch the ticket from the Kitchen Vault [cite: 2026-01-28]
            var ticket = await db.Tickets.FirstOrDefaultAsync(x => x.OrderId == request.OrderId, ct);
            if (ticket is null)
            {
                return Result.Failure("Kitchen ticket not found.");
            }

            // 2. Domain Logic: Move status to Ready
            ticket.MarkAsReady();

            await db.SaveChangesAsync(ct);

            // 3. THE RAISED EVENT: Signalling the Robotics Module [cite: 2026-01-08]
            // This is the specific line where FoodReady is born.
            dispatcher.PublishFoodReady(ticket.OrderId, ticket.TableNumber, ct);

            // 4. Audit Log [cite: 2026-01-29]
            Console.WriteLine($"[{DateTime.UtcNow}]: KITCHEN - Order {ticket.OrderId} ready for delivery.");

            return Result.Success;
        }
    }
}
