using System;
using System.Collections.Generic;
using System.Text;
using Faster.Modulith.Contracts;

namespace Module.Ordering.Api.Events
{
    public record TableFinalizedEvent(int TableNumber, decimal Total) : IEvent;
}
