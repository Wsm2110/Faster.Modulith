using Faster.Modulith.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Module.IdentityAccess.Api;

public record EmployeeHiredEvent : IEvent
{
    public EmployeeHiredEvent(Guid empId, string name)
    {
        EmployeeId = empId;
        Name = name;
    }

    public Guid EmployeeId { get; }

    public string Name { get; }
}