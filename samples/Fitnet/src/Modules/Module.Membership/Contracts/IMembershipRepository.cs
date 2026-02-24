using Module.Membership.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Module.Membership.Contracts;

/// <summary>
/// Provides data access operations for managing membership contracts.
/// </summary>
internal interface IMembershipRepository
{
    /// <summary>
    /// Asynchronously saves a new or updated membership contract to the data store.
    /// </summary>
    /// <param name="contract">The membership contract entity to be saved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAsync(Contract contract);

    /// <summary>
    /// Asynchronously retrieves a specific membership contract by its unique identifier.
    /// </summary>
    /// <param name="id">The globally unique identifier (GUID) of the contract.</param>
    /// <returns>
    /// A task representing the asynchronous operation. 
    /// The task result contains the <see cref="Contract"/> if found; otherwise, <c>null</c>.
    /// </returns>
    Task<Contract?> GetByIdAsync(Guid id);
}