using Faster.Modulith.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Offers.Api.UseCases;

/// <summary>
/// Represents the public entry point for marking an existing pass as expired.
/// </summary>
/// <param name="PassId">The unique identifier of the pass to expire.</param>
/// <param name="CustomerId">The identifier of the customer who owns the pass.</param>
/// <param name="Reason">The reason for expiring the pass.</param>
public record MarkPassExpiredUseCase(Guid PassId, Guid CustomerId, string Reason) : IUseCase<Result>;