namespace Module.Kitchen.Domain;

internal class KitchenTicket
{
    public Guid OrderId { get; private set; } // The link back to Ordering
    public int TableNumber { get; private set; }
    public string Description { get; private set; }
    public TicketStatus Status { get; private set; }

    internal KitchenTicket(Guid orderId, int tableNumber, string description)
    {
        OrderId = orderId;
        TableNumber = tableNumber;
        Description = description;
        Status = TicketStatus.Queued;
    }

    public void MarkAsReady()
    {
        if (Status == TicketStatus.Ready)
            return; // Idempotent

        if (Status == TicketStatus.Cancelled)
            throw new InvalidOperationException("Cannot ready a cancelled order.");

        Status = TicketStatus.Ready;
        
        Console.WriteLine($"[{DateTime.UtcNow}]: Kitchen Ticket {OrderId} marked as READY.");
    }

    public void StartCooking() => Status = TicketStatus.Cooking;
}

internal enum TicketStatus {Cancelled, Queued, Cooking, Ready }
