using Faster.Modulith.Contracts;

namespace Module.Ordering.Api.Events;

public record BurgerOrderPlacedEvent(Guid OrderId, int TableNumber, string Summary) : IEvent;