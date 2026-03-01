using Faster.Modulith.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Membership.Api.Events;

/// <summary>
/// Event triggered when a membership is successfully signed.
/// This event will trigger pass registration in the Passes module.
/// </summary>
/// <param name="MembershipId">The unique identifier of the signed membership.</param>
/// <param name="CustomerId">The identifier of the customer who signed.</param>
public record MembershipSignedEvent(Guid MembershipId, Guid CustomerId) : IEvent;
