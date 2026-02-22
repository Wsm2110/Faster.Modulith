using Faster.Modulith.Contracts;
using Faster.Modulith;
using FluentValidation;
using Module.Robotics.Api.UseCases;
using Module.Robotics.Contracts;
using Module.Robotics.Domain;
using Module.Robotics.Infrastructure;

namespace Module.Robotics.Application.UseCases;

/// <summary>
/// Orchestrates the physical delivery lifecycle of a food order.
/// This handler is internal to the Robotics vault to protect hardware logic.
/// </summary>
[Expose("api/v1/robotics/deliverfood")]
internal class DeliverFoodUseCaseHandler(
    RoboticsDbContext db,
    IRoboticsDispatcher dispatcher,
    IRobotHardware robot) : IUseCaseHandler<DeliverFoodUseCase, Result>
{
    public async ValueTask<Result> Handle(DeliverFoodUseCase request, CancellationToken ct)
    {
        // 1. Initialize the Task in the Vault [cite: 2026-01-28]
        // We persist the intent immediately so the task can be recovered if the system restarts.
        var task = new DeliveryTask(request.OrderId, request.TableNumber);
        db.DeliveryTasks.Add(task);
        await db.SaveChangesAsync(ct);

        try
        {
            // 2. Retrieve Food
            // Handshake with the Kitchen module's physical output station.
            Console.WriteLine($"[{DateTime.UtcNow}]: ROBOT - Picking up Order {request.OrderId} from Kitchen counter.");
            await robot.PickupFromCounter(ct);

            // 3. Deliver Food
            // Navigation logic is encapsulated within the IRobotHardware implementation.
            Console.WriteLine($"[{DateTime.UtcNow}]: ROBOT - Navigating to Table {request.TableNumber}.");
            await robot.NavigateToTable(request.TableNumber, ct);

            // Domain state transition: The burger is now officially with the customer.
            task.MarkAsDelivered();

            // 4. Scan for Dirty Dishes (Secondary intent)
            // An opportunistic task to improve restaurant efficiency.
            Console.WriteLine($"[{DateTime.UtcNow}]: ROBOT - Scanning for dirty dishes at Table {request.TableNumber}.");
            bool foundDishes = await robot.ScanForDishes(ct);
            if (foundDishes)
            {
                await robot.PickupDirtyDishes(ct);
            }

            // 5. Return to Kitchen
            // Ensures the robot is back at the charging/pickup station for the next ticket.
            Console.WriteLine($"[{DateTime.UtcNow}]: ROBOT - Returning to base.");
            await robot.ReturnToBase(ct);

            // 6. Finalize State and Signal [cite: 2026-01-08]
            // We save the final telemetry before notifying the rest of the monolith.
            await db.SaveChangesAsync(ct);

            // Signal the Ordering and Kitchen modules that the cycle is finished.
            await dispatcher.PublishDeliveryCycleCompletedAsync(request.OrderId, ct);

            return Result.Success;
        }
        catch (Exception ex)
        {
            // Fail-safe: Ensure the error is recorded in the private Robotics vault.
            task.MarkAsFailed(ex.Message);
            await db.SaveChangesAsync(ct);

            return Result.Failure($"Robot failure: {ex.Message}");
        }
    }
}

/// <summary>
/// Ensures the delivery request contains valid routing data before the robot attempts to move.
/// </summary>
internal class DeliverFoodValidator : AbstractValidator<DeliverFoodUseCase>
{
    public DeliverFoodValidator()
    {
        RuleFor(c => c.OrderId).NotEmpty().WithMessage("Order ID is required for delivery.");
        RuleFor(c => c.TableNumber).GreaterThan(0).WithMessage("Valid table number is required.");
    }
}