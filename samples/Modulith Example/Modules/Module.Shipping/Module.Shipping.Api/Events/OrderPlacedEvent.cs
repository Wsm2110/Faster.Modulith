using Faster.Modulith.Contracts; // Access to IEvent
using System;
using System.Collections.Generic;
using System.Text;

// NOTICE: This namespace ends in '.Api.Events'.
// This is the "Public Square". Any message defined here is intended to be shouted 
// to the rest of the system.
namespace Module.Shipping.Api.Events;

/// <summary>
/// A public notification (Fact) that an Order has been successfully placed.
/// <para>
/// <b>Why exists?</b> This implements <i>Event-Driven Architecture</i>. 
/// The "Sales" module does something (places order), but instead of calling "Shipping" directly (coupling), 
/// it just publishes this Event. Shipping (and Inventory, and Billing) can listen if they want.
/// </para>
/// </summary>
/// <remarks>
/// <b>Why Record?</b> Events represent something that <i>already happened</i> in the past. 
/// You cannot change history. Records are immutable by default, making them perfect for this.
/// </remarks>
public record OrderPlacedEvent : IEvent
{
    /// <summary>
    /// Creates a new instance of the event.
    /// </summary>
    /// <param name="orderId">The unique ID of the order.</param>
    /// <param name="customerId">The customer who bought it.</param>
    /// <param name="v">An arbitrary version string or data payload.</param>
    public OrderPlacedEvent(Guid orderId, object customerId, string v)
    {
        // HOW: We assign properties in the constructor to ensure the event 
        // is valid from the moment it is created.
        OrderId1 = orderId;
        CustomerId = customerId;
        V = v;
    }

    // NOTE: In a real scenario, avoid duplicate IDs like OrderId vs OrderId1.
    // 'internal set' means only code inside this specific assembly can change this property,
    // protecting it from external modification.
    public ulong OrderId { get; internal set; }

    /// <summary>
    /// The Global Unique Identifier for the order.
    /// </summary>
    // WHY: Guid is preferred over int/long for distributed systems because 
    // you can generate them anywhere without checking the database for the "next number".
    public Guid OrderId1 { get; }

    /// <summary>
    /// The ID of the customer.
    /// </summary>
    // CRITIQUE: Ideally, use strong types (Guid/string) instead of 'object'. 
    // 'object' forces the listener to guess what the type is (casting), which causes bugs.
    public object CustomerId { get; }

    // CRITIQUE: Naming matters! 'V' doesn't tell the junior dev what this data is.
    // Better names: 'Version', 'VendorCode', 'ValidationToken'.
    public string V { get; }
}