using System;
using Faster.Modulith.Contracts;

namespace Module.Membership.Api;

/// <summary>
/// Represents a use case for preparing a membership for a customer with a specified plan type.
/// </summary>
/// <param name="CustomerId">The unique identifier of the customer for whom the membership is being prepared.</param>
/// <param name="PlanType">The type of membership plan to be assigned to the customer. Cannot be null or empty.</param>
public record struct PrepareMembershipUseCase(Guid CustomerId, string PlanType) : IUseCase<Result<Guid>>;
