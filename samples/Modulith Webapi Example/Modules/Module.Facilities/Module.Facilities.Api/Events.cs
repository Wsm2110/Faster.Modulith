using Faster.Modulith.Contracts;

// ARCHITECTURE WARNING:
// You placed this in 'Module.Facilities.Api'.
// typically, the "OnEmployeeHired" event belongs to the PRODUCER (HumanResources), not the Consumer (Facilities).
// Events should be defined in the project that *publishes* them. 
// Facilities should only *reference* Module.HumanResources.Api to use this event.
namespace Module.Facilities.Api;

/// <summary>
/// A public notification (Fact) that an Employee was hired.
/// <para>
/// <b>Why exists?</b> This is the "Contract" for the event. 
/// Any module (Facilities, Payroll, IT) that needs to react to a new hire will listen for this exact message.
/// </para>
/// </summary>
public record OnEmployeeHiredEvent : IEvent
{
    // CRITIQUE: Mutable Property ('set').
    // Events represent history. History cannot change.
    // If a listener receives this event and changes the EmployeeId, it might corrupt the data for other listeners!
    // ALWAYS use '{ get; init; }' or '{ get; }' for Events to make them immutable.
    public Guid EmployeeId { get; set; }

    // BETTER: 'internal set' is safer than 'public set'.
    // It means only the module that created this event (the Producer) can set the name.
    // Listeners (Consumers) can only read it.
    public string FirstName { get; internal set; }
}