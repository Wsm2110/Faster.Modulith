using Faster.Modulith.Contracts;
using FluentValidation;
using Module.HumanResources.Api; // Access to the Event definition
using Module.HumanResources.Infrastructure; // Access to the Database layer

namespace Module.HumanResources.Application.EventHandlers
{
    /// <summary>
    /// A "Listener" class that updates employee records when a badge is created.
    /// <para>
    /// <b>Why exists?</b> This implements the "Leaf Node" pattern. 
    /// The "Facilities" module did the hard work (printing the badge) and shouted "Done!".
    /// This handler hears that shout and simply updates the HR database. It does not trigger any *new* complex workflows.
    /// It is the end of the line (a leaf) for this process.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <b>Primary Constructor:</b> We inject <see cref="EmployeeRepository"/> directly. 
    /// We don't need a Dispatcher here because we aren't calling other logic; we are just saving data.
    /// </remarks>
    internal class OnBadgeIssuedHandler(EmployeeRepository employeeRepository) : IEventHandler<OnBadgeIssuedEvent>
    {
        /// <summary>
        /// Updates the employee's file with the new badge number.
        /// </summary>
        public async ValueTask Handle(OnBadgeIssuedEvent @event, CancellationToken ct)
        {
            // ARCHITECTURE PATTERN: Direct Infrastructure Call (Leaf Node)
            // HOW: We call the repository directly.
            // WHY: Since this handler is the *end* of the workflow, we don't need the complexity of 
            // creating an internal "UpdateBadgeCommand" and a separate "UpdateBadgeHandler".
            // We just do the work. This is perfectly acceptable for simple reactions.
            await employeeRepository.UpdateBadgeAsync(@event.EmployeeId, @event.BadgeCode);
        }
    }

    /// <summary>
    /// The Guard Clause.
    /// </summary>
    internal class OnBadgeIssuedValidator : AbstractValidator<OnBadgeIssuedEvent>
    {
        public OnBadgeIssuedValidator()
        {
            // Example: RuleFor(e => e.BadgeCode).NotEmpty();
        }
    }
}