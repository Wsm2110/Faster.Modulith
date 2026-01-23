using Faster.Modulith;
using Faster.Modulith.Contracts;
using FluentValidation;
using Module.Facilities.Api; // Access to Public Contracts
using Module.Facilities.Application.CommandHandlers; // Access to Internal Commands
using Module.HumanResources.Api; // Access to the Event definition (OnEmployeeHiredEvent)

namespace Module.Facilities.Application.EventHandlers
{
    /// <summary>
    /// A "Reactor" class. It wakes up when HR hires someone and ensures Facilities does its part.
    /// <para>
    /// <b>Why exists?</b> This is the glue between modules. HR doesn't know about desks or badges. 
    /// HR just says "Hired!". This handler ensures that *when* someone is hired, 
    /// the necessary physical assets are prepared automatically.
    /// </para>
    /// </summary>
    internal class OnEmployeeHiredHandler(IFacilitiesDispatcher FacilitiesDispatcher, IFacilitiesModule facilitiesApi)
        : IEventHandler<OnEmployeeHiredEvent>
    {
        /// <summary>
        /// Orchestrates the "New Hire" workflow for Facilities.
        /// </summary>
        public async ValueTask Handle(OnEmployeeHiredEvent @event, CancellationToken ct)
        {
            // 1. Assign Desk (Internal Logic)
            // HOW: We don't write the desk assignment logic here. We reuse the existing 'AllocateDesk' command.
            // WHY: This follows the DRY (Don't Repeat Yourself) principle. If the logic for picking a desk changes,
            // we only update 'AllocateDeskHandler', and this event handler gets the update for free.
            await FacilitiesDispatcher.AllocateDesk(new AllocateDeskCommand(@event.EmployeeId), ct);

            // 2. Print Badge (Hardware Interaction)
            // HOW: We trigger another internal command to talk to the hardware.
            // note: We use the data from the event (@event.FirstName) to populate the command.
            var badgeResult = await FacilitiesDispatcher.IssueBadge(new IssueBadgeCommand(@event.EmployeeId, @event.FirstName), ct);

            // FLOW CONTROL: If the printer fails, we stop. We don't want to announce success if we failed.
            if (badgeResult.IsFailure)
            {
                // Junior Dev Note: In a real system, you might want to log this error or 
                // schedule a retry so the new employee isn't left without a badge!
                return;
            }

            // 3. Announce Completion (The "Airlock")
            // HOW: We use the Facilities API to publish a NEW event: "SecurityBadgeIssued".
            // WHY: HR might be listening for this! Once the badge is issued, HR might need to 
            // activate the employee's payroll or building access. 
            // This creates a "Chain of Events": Hired -> [Facilities Work] -> Badge Issued -> [HR Work] -> Access Granted.
            facilitiesApi.PublishSecurityBadgeIssued(@event.EmployeeId, badgeResult.Value);
        }
    }

    /// <summary>
    /// Validates the event data before processing.
    /// </summary>
    internal class OnEmployeeHiredValidator : AbstractValidator<OnEmployeeHiredEvent>
    {
        public OnEmployeeHiredValidator()
        {
            // Example: RuleFor(e => e.EmployeeId).NotEmpty();
        }
    }
}