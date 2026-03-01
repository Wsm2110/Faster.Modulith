using System;
using Faster.Modulith.Contracts;

namespace Module.Membership.Api;

/// <summary>
/// Represents the public entry point for finalizing and signing an existing membership.
/// </summary>
/// <param name="MembershipId">The unique identifier of the membership.</param>
/// <param name="SignatureReference">The digital signature reference.</param>
public record SignMembershipUseCase(Guid MembershipId, string SignatureReference) : IUseCase<Result>;

/// <summary>
/// Represents the public entry point for preparing a formal membership document.
/// </summary>
/// <param name="CustomerId">The target customer ID.</param>
/// <param name="Terms">The specific terms of the membership.</param>
/// <param name="Tier">The membership tier level.</param>
public record PrepareMembershipUseCase(Guid CustomerId, string Terms, string Tier) : IUseCase<Result>;
