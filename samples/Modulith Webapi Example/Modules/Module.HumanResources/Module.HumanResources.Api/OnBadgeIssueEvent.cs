using Faster.Modulith.Contracts;

// ARCHITECTURE CHECK:
// You are defining this event in 'Module.HumanResources.Api'.
// Question: Who *issues* the badge? 
// If 'Facilities' prints the badge, then 'Facilities' should own/define this event (e.g., Module.Facilities.Api).
// HR should just be a *Listener*. 
// Defining the event here suggests HR is the one creating badges, which contradicts the logic in previous snippets.
namespace Module.HumanResources.Api;

/// <summary>
/// A public notification (Fact) stating that a physical badge has been created.
/// <para>
/// <b>Why exists?</b> This serves as a message for other parts of the system. 
/// For example, the "Access Control" module might listen to this to activate the door locks 
/// for this specific Badge Code.
/// </para>
/// </summary>
public record OnBadgeIssuedEvent : IEvent
{
    // CRITIQUE: Mutability.
    // Events represent the Past. The Past cannot change.
    // By using 'set', you allow a listener to accidentally modify the ID before the next listener sees it.
    // ALWAYS use '{ get; init; }' for Event properties.
    public Guid EmployeeId { get; set; }

    // CRITIQUE: Primitive Obsession / Naming.
    // 'BadgeCode' is good. 
    // If this was just named 'Code', it would be ambiguous (Zip Code? Pin Code?). 
    // Context-specific naming is crucial in public API contracts.
    public string BadgeCode { get; set; }
}