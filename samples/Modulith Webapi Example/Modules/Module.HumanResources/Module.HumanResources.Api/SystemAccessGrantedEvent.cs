using Faster.Modulith.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

// NOTICE: This event is defined in 'Module.HumanResources.Api'.
// ARCHITECTURE QUESTION: Who grants system access? 
// Usually, an 'Identity' or 'IT' module grants access and issues emails. 
// If that's the case, this file should arguably live in 'Module.Identity.Api'.
// HR is likely just a *Consumer* of this event (updating the employee file with the new email), 
// not the *Producer* of it.
namespace Module.HumanResources.Api;

/// <summary>
/// A public notification (Fact) that a user has been given login credentials.
/// <para>
/// <b>Why exists?</b> This is the bridge between the "Technical World" (Logins, Emails) 
/// and the "Business World" (Employees). HR needs to know the email address to put on the payroll stub,
/// so it listens for this event.
/// </para>
/// </summary>
public record SystemAccessGrantedEvent : IEvent
{
    // OLD SCHOOL STYLE: Explicit Constructor
    // This is valid C#, but it's verbose. 
    // It forces you to type 'EmployeeId' three times (Parameter, Property, Assignment).
    public SystemAccessGrantedEvent(Guid employeeId, string email)
    {
        EmployeeId = employeeId;
        Email = email;
    }

    /// <summary>
    /// The ID of the employee who gained access.
    /// </summary>
    // GOOD: 'internal set' protects this data from being changed by listeners (Consumers).
    // Only the module that created this object can set the value.
    // MODERN TIP: Use 'public Guid EmployeeId { get; init; }' for cleaner code that achieves the same goal.
    public Guid EmployeeId { get; internal set; }

    /// <summary>
    /// The generated email address (e.g. jane.doe@corp.com).
    /// </summary>
    public string Email { get; internal set; }
}