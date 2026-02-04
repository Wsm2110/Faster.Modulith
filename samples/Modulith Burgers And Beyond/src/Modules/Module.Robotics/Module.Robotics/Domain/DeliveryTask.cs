using System;

namespace Module.Robotics.Domain;

/// <summary>
/// Internal state machine for tracking physical robot delivery tasks.
/// </summary>
internal enum DeliveryStatus
{
    Created,
    PickingUp,
    Navigating,
    ScanningDishes,
    Returning,
    Completed,
    Failed
}

internal class DeliveryTask
{
    public Guid OrderId { get; private set; }
    public int TableNumber { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public bool DishesCollected { get; private set; }

    // Telemetry and Auditing [cite: 2026-01-29]
    public DateTime CreatedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    internal DeliveryTask(Guid orderId, int tableNumber)
    {
        OrderId = orderId;
        TableNumber = tableNumber;
        Status = DeliveryStatus.Created;
        CreatedAt = DateTime.UtcNow;

        Console.WriteLine($"[{CreatedAt}]: TASK {OrderId} - Created for Table {TableNumber}");
    }

    /// <summary>
    /// Moves the robot through its physical lifecycle steps.
    /// </summary>
    public void TransitionTo(DeliveryStatus newStatus)
    {
        // Log transition for auditing [cite: 2026-01-29]
        Console.WriteLine($"[{DateTime.UtcNow}]: TASK {OrderId} - Transitioning {Status} -> {newStatus}");
        Status = newStatus;
    }

    /// <summary>
    /// Finalizes the handoff to the customer.
    /// </summary>
    public void MarkAsDelivered()
    {
        if (Status == DeliveryStatus.Completed) return;
        if (Status == DeliveryStatus.Failed)
            throw new InvalidOperationException("Cannot mark a failed task as delivered.");

        Status = DeliveryStatus.Completed;
        DeliveredAt = DateTime.UtcNow;
        FinishedAt = DeliveredAt;

        Console.WriteLine($"[{DeliveredAt}]: ROBOTICS - Order {OrderId} successfully placed on table {TableNumber}.");
    }

    public void MarkDishesCollected()
    {
        DishesCollected = true;
        Console.WriteLine($"[{DateTime.UtcNow}]: TASK {OrderId} - Dirty dishes collected.");
    }

    /// <summary>
    /// Standard completion for non-delivery specific tasks (like returning to base).
    /// </summary>
    public void Complete()
    {
        if (Status == DeliveryStatus.Completed) return;

        Status = DeliveryStatus.Completed;
        FinishedAt = DateTime.UtcNow;

        Console.WriteLine($"[{FinishedAt}]: TASK {OrderId} - Cycle finished.");
    }

    /// <summary>
    /// Records mechanical or logical failures during the cycle.
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        Status = DeliveryStatus.Failed;
        ErrorMessage = reason;
        FinishedAt = DateTime.UtcNow;

        Console.WriteLine($"[{FinishedAt}]: TASK {OrderId} - FAILED: {reason}");
    }
}