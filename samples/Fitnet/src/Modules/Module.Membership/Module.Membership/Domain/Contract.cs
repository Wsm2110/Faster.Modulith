using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Membership.Domain;

/// <summary>
/// Represents a membership contract for a customer.
/// </summary>
internal class Contract
{
    /// <summary>
    /// Gets the unique identifier for the contract.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the customer associated with this contract.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Gets the type of membership plan selected by the customer.
    /// </summary>
    public string PlanType { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the contract has been signed.
    /// </summary>
    public bool IsSigned { get; private set; }

    /// <summary>
    /// Gets the date and time in UTC when the contract was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets or sets the credit card number associated with the contract for billing purposes.
    /// </summary>
    public int CreditCardNumber { get; internal set; }

    /// <summary>
    /// Gets or sets the expiration date of the contract's validity.
    /// </summary>
    public DateTime ValidUntil { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Contract"/> class.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer.</param>
    /// <param name="planType">The specified membership plan type.</param>
    public Contract(Guid customerId, string planType)
    {
        // Assign a new unique identifier for the contract instance
        Id = Guid.NewGuid();
        CustomerId = customerId;
        PlanType = planType;

        // Contracts default to an unsigned state upon initialization
        IsSigned = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the contract as signed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when attempting to sign an already signed contract.</exception>
    public void Sign()
    {
        // Guard clause to prevent duplicate signing operations
        if (IsSigned)
        {
            throw new InvalidOperationException("Contract is already signed.");
        }

        IsSigned = true;
    }
}