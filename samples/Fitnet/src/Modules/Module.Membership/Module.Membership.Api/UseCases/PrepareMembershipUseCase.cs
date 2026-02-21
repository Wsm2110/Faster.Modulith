using System;
using Faster.Modulith.Contracts;

namespace Module.Membership.Api;

public record PrepareMembershipUseCase : IUseCase<Result<Guid>>
{
    public Guid CustomerId { get; set; }
    public string PlanType { get; set; }
}