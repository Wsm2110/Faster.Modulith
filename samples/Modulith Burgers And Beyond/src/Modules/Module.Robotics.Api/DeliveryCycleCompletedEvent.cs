using System;
using System.Collections.Generic;
using System.Text;
using Faster.Modulith.Contracts;

namespace Module.Robotics.Api
{
    public record DeliveryCycleCompletedEvent(Guid OrderId) : IEvent;

}
