using Module.Membership.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Membership.Contracts;

internal interface IMembershipRepository
{
    Task SaveAsync(Contract contract);
    Task<Contract?> GetByIdAsync(Guid id);
}