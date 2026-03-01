using Faster.Modulith.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Offers.Api.UseCases;

/// <summary>
/// Represents the public entry point for preparing a commercial offer.
/// </summary>
/// <param name="CustomerId">The unique identifier of the customer.</param>
/// <param name="ExpiredPassId">The unique identifier of the pass that has expired.</param>
public record PrepareOfferUseCase(Guid CustomerId, Guid ExpiredPassId) : IUseCase<Result>;