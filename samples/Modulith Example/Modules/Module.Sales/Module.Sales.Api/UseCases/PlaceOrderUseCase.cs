using System;
using Faster.Modulith.Contracts; // Access to IUseCase<T>

// NOTICE: This resides in '.Api'. 
// This file is the "Public Menu" for the Sales module. 
// Any other module (like a Web API or Shipping) that wants to place an order must use this class.
namespace Module.Sales.Api;

/// <summary>
/// Represents a request to create a new Order in the system.
/// <para>
/// <b>Why exists?</b> This serves as a <i>Data Transfer Object (DTO)</i>. 
/// It acts as a rigid envelope to carry data across module boundaries. 
/// By defining this contract, we ensure that anyone calling 'Sales' provides exactly the data we need.
/// </para>
/// </summary>
/// <remarks>
/// <b>Why IUseCase?</b> Implementing <see cref="IUseCase{TResult}"/> tells the system:
/// "This is a request (Input) that expects a <see cref="Result{Guid}"/> (Output) in return."
/// </remarks>
public record PlaceOrderUseCase : IUseCase<Result<Guid>>
{
    // CRITIQUE: Avoid public fields (variables). Use Properties ({ get; }) instead.
    // Fields break encapsulation and serialization often behaves differently with them.
    public object CustomerId;

    /// <summary>
    /// Initializes the request with required data.
    /// </summary>
    /// <param name="orderId">The ID for the new order.</param>
    /// <param name="productId">The item being bought.</param>
    /// <param name="v">The version or variant.</param>
    /// <param name="customerId">The buyer.</param>
    public PlaceOrderUseCase(Guid orderId, Guid productId, int v, Guid customerId)
    {
        OrderId = orderId;
        ProductId = productId;
        V = v;
        CustomerId = customerId;
    }

    // CRITIQUE: Using 'object' destroys type safety. 
    // If you pass a "Banana" string into ProductId, the compiler won't stop you, but the app will crash at runtime.
    // ALWAYS use strong types: 'public Guid ProductId { get; }'
    public object ProductId { get; set; }

    // CRITIQUE: Mutable Properties ({ get; set; }).
    // A request shouldn't change once it's sent. Ideally, use '{ get; init; }' or just '{ get; }'
    // to make this immutable (Read-Only).
    public object Quantity { get; set; }

    // GOOD: Using a strong type (Guid) ensures we are dealing with a valid ID.
    public Guid OrderId { get; set; }

    // CRITIQUE: Naming. 'V' is a "Mystery Meat" name. 
    // A junior dev reading this won't know if V stands for Version, Velocity, or Volume.
    // Better: 'public int SchemaVersion { get; }'
    public int V { get; }
}