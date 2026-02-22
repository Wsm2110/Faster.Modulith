using System;
using System.Collections.Generic;
using System.Text;
using Faster.Modulith.Contracts;

namespace Module.Kitchen.Api.Events;

public record struct FoodReadyEvent(Guid OrderId, int TableNumber) : IEvent;

