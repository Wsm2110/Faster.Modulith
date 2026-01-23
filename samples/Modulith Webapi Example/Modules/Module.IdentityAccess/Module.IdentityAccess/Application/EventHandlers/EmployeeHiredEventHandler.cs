using Module.IdentityAccess.Infrastructure; // Access to Active Directory (AD)
using Faster.Modulith.Contracts;
using Module.IdentityAccess.Api;
using Faster.Modulith;

// NOTICE: We are in 'Module.IdentityAccess'.
// This module handles Security, Logins, and Passwords.
namespace Module.IdentityAccess.Application.EventHandlers;

/// <summary>
/// A "Reactor" that listens for new hires and creates their computer accounts.
/// <para>
/// <b>Why exists?</b> When HR hires someone, IT needs to set up their account. 
/// Instead of HR calling IT on the phone (or calling the IT code directly), 
/// IT just listens for the "Employee Hired" event. This decouples the two departments.
/// </para>
/// </summary>
internal class EmployeeHiredHandler : IEventHandler<EmployeeHiredEvent>
{
    // INFRASTRUCTURE:
    // This gateway talks to the Microsoft Active Directory server.
    private readonly ActiveDirectoryGateway _ad;

    // API ACCESS:
    // We have a reference to the HR Public API.
    // CRITIQUE: Naming. '_Api' is vague. '_hrApi' would be much clearer.
    private readonly IHumanResourcesModule _Api;

    public EmployeeHiredHandler(ActiveDirectoryGateway ad, IHumanResourcesModule Api)
    {
        _ad = ad;
        _Api = Api;
    }

    /// <summary>
    /// Creates the user account and notifies HR.
    /// </summary>
    public async ValueTask Handle(EmployeeHiredEvent @event, CancellationToken ct)
    {
        // 1. INFRASTRUCTURE WORK (The "Action")
        // HOW: We take the name from the event and create a user in AD.
        // NOTE: In reality, names aren't unique ("John Smith"). 
        // Real code would need logic to handle duplicates (e.g., "john.smith2").
        var email = await _ad.CreateUserAsync(@event.Name);

        // 2. CLOSING THE LOOP (The "Reaction")
        // ARCHITECTURAL SMELL:
        // We are calling '_Api.PublishSystemAccessGranted'.
        // Since '_Api' is the *HR* API, this means Identity is technically telling HR 
        // "Please announce that System Access was granted."
        //
        // IDEALLY: Identity should publish its OWN event (`Module.Identity.Api.UserCreatedEvent`).
        // HR should listen to *that*.
        // The current approach works, but it couples Identity tightly to HR's API definitions.
        _Api.PublishSystemAccessGranted(@event.EmployeeId, email);
    }
}