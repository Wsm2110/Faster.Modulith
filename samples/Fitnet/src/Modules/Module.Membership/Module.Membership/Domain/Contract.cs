using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Membership.Domain;

internal class Contract
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string PlanType { get; private set; }
    public bool IsSigned { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Contract(Guid customerId, string planType)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        PlanType = planType;
        IsSigned = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void Sign()
    {
        if (IsSigned) throw new InvalidOperationException("Contract is already signed.");
        IsSigned = true;
    }
}