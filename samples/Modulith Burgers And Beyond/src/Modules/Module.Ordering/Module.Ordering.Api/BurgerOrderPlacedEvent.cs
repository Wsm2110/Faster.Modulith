using Faster.Modulith.Contracts;

namespace Module.Ordering.Api;

public record BurgerOrderPlacedEvent(Guid OrderId, int TableNumber, string Summary) : IEvent;