using System;
using System.Collections.Generic;
using System.Text;
using Faster.Modulith.Contracts;
using Module.Kitchen.Api.Events;

namespace Module.Robotics.Application.EventHandlers;

internal class DeliverFoodEventHandler : IEventHandler<FoodReadyEvent>
{
    public ValueTask Handle(FoodReadyEvent @event, CancellationToken ct)
    {
        //TODO retrieve food,
        //TODO deliver food to table @event.TableNumber
        return default; 
    }
}