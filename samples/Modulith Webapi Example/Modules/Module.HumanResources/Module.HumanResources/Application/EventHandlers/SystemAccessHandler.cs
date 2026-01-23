using Module.HumanResources.Infrastructure; // Access to the database
using Faster.Modulith.Contracts; // Access to IEventHandler
using Module.HumanResources.Api; // Access to the Event definition (likely from Identity/IT module)

namespace Module.HumanResources.Application.EventHandlers
{
    /// <summary>
    /// A "Sync Agent" that keeps HR records up to date.
    /// <para>
    /// <b>Why exists?</b> In this system, the "IT/Identity" module is responsible for creating emails (e.g., john.doe@company.com).
    /// HR needs to know this email, but HR doesn't generate it.
    /// So, HR listens for the 'SystemAccessGranted' event and updates its own database.
    /// This is called <i>Eventual Consistency</i>.
    /// </para>
    /// </summary>
    internal class SystemAccessHandler : IEventHandler<SystemAccessGrantedEvent>
    {
        // INFRASTRUCTURE:
        // We hold a reference to the repository to perform database updates.
        private readonly EmployeeRepository _employeeRepository;

        // HOW: Explicit Constructor Injection.
        // In previous examples, we used "Primary Constructors" (public class X(Repo r)).
        // This is the "Classic" C# syntax. It does the exact same thing: 
        // asking the Dependency Injection container to provide the repository.
        public SystemAccessHandler(EmployeeRepository employeeRepository) => _employeeRepository = employeeRepository;

        /// <summary>
        /// Updates the employee record with their new corporate email.
        /// </summary>
        public async ValueTask Handle(SystemAccessGrantedEvent @event, CancellationToken ct)
        {
            // ARCHITECTURE PATTERN: Leaf Node / Direct Call
            // HOW: We take the data from the event (@event.Email) and push it directly to our database.
            // WHY: There is no business logic here (we aren't deciding *if* they get an email, that already happened).
            // We are just recording the fact. Therefore, we don't need a middleman Command/Handler.
            await _employeeRepository.UpdateEmailAsync(@event.EmployeeId, @event.Email);
        }
    }
}