namespace Module.Ordering.Domain;

internal class Order
{
    public Guid Id { get; private set; }
    public int TableNumber { get; private set; }
    public string BurgerName { get; private set; }
    public string Status { get; private set; } = "Received";
    public DateTime CreatedAt { get; private set; }

    public Order(Guid id, int tableNumber, string burgerName)
    {
        Id = id;
        TableNumber = tableNumber;
        BurgerName = burgerName;
        CreatedAt = DateTime.UtcNow;
    }
}