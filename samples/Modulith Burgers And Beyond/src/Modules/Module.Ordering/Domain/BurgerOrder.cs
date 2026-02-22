using Module.Ordering.Api.UseCases;

namespace Module.Ordering.Domain;

internal class BurgerOrder
{
    public Guid Id { get; private set; }
    public int TableNumber { get; private set; }
    public string BurgerName { get; private set; }
    public string SpecialInstructions { get; private set; }
    public decimal TotalPrice { get; private set; }
    public OrderStatus Status { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public string? CancellationReason { get; private set; }


    internal BurgerOrder(Guid id, int tableNumber, string burgerName, string specialInstructions)
    {
        if (tableNumber <= 0) throw new ArgumentException("Invalid table number.");
        if (string.IsNullOrWhiteSpace(burgerName)) throw new ArgumentException("Burger name is required.");

        Id = id;
        TableNumber = tableNumber;
        BurgerName = burgerName;
        SpecialInstructions = specialInstructions ?? string.Empty;
        Status = OrderStatus.Pending;

        // Auto-add the primary item to trigger price calculation behavior
        AddPremiumBurger(burgerName, 15.50m);
    }

    public void AddPremiumBurger(string name, decimal price)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot modify an order already sent to the kitchen.");

        _items.Add(new OrderItem(name, price));
        TotalPrice = _items.Sum(x => x.Price);
    }
    
    public void UpdateStatus(OrderStatus newStatus)
    {
        // Audit log for state transition [cite: 2026-01-29]
        Console.WriteLine($"[{DateTime.UtcNow}]: Order {Id} transitioning {Status} -> {newStatus}");

        // Rule: Cannot move backwards (e.g., from Delivered back to Preparing)
        if (newStatus < Status)
            throw new InvalidOperationException("Orders cannot move to a previous status.");

        Status = newStatus;
    }

    public void UpdateInstructions(string newInstructions)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("The kitchen has already started; instructions are locked.");

        SpecialInstructions = newInstructions;
    }

    public void MarkAsPaid()
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot pay for a cancelled order.");

        Status = OrderStatus.Paid;
    }

    internal void UpdateStatus(object newStatus)
    {
        throw new NotImplementedException();
    }

    internal void Cancel(string reason)
    {
        throw new NotImplementedException();
    }
}


internal record OrderItem(string Name, decimal Price);