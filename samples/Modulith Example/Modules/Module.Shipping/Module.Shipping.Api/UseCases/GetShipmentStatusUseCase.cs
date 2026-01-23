using System;
using Faster.Modulith.Contracts; // Access to IUseCase<T>

// NOTICE: This namespace ends in '.Api'. 
// This indicates this file lives in a separate project (e.g., Module.Shipping.Api) 
// that contains ONLY public contracts (interfaces, DTOs, Enums).
namespace Module.Shipping.Api;

/// <summary>
/// A public request object (Contract) asking the Shipping module for the status of a specific order.
/// <para>
/// <b>Why exists?</b> This is the "Menu Item". Other modules (like Sales or CustomerService) 
/// cannot see the database or the internal logic of Shipping. They can only see this object.
/// By creating this, we define exactly what inputs we accept from the outside world.
/// </para>
/// </summary>
/// <remarks>
/// <b>Why Public?</b> Unlike the Handlers (which are internal "kitchen staff"), this class must be <c>public</c> 
/// so that other modules can reference it and send it via the Orchestrator.
/// </remarks>
public record GetShipmentStatusUseCase : IUseCase<Result<string>>
{
    // HOW: We use a Primary Constructor (or standard constructor) to enforce required data.
    // You cannot create this request without providing an OrderId.
    public GetShipmentStatusUseCase(Guid orderId)
    {
        OrderId = orderId;
    }

    /// <summary>
    /// The unique identifier of the order to look up.
    /// </summary>
    // WHY: Immutable properties (get-only) ensure that the request doesn't change 
    // while it travels from the caller to the handler.
    public Guid OrderId { get; }
}