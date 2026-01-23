using Faster.Modulith.Contracts;
using Faster.Modulith;
using Module.HumanResources.Api; // Access to Public Contracts
using Module.HumanResources.Application.CommandHandlers; // Access to Internal Commands
namespace Module.HumanResources.Application.UseCases;

/// <summary>
/// The "Hiring Manager" class. It coordinates the entire hiring workflow.
/// <para>
/// <b>Why exists?</b> This is an <i>Orchestrator</i>. It does not know how to save to the database 
/// or how to check accounting rules. It just knows the *order* in which these things must happen.
/// </para>
/// </summary>
internal class HireEmployeeHandler : IUseCaseHandler<HireEmployeeUseCase, Result<Guid>>
{
    // The Dispatcher is used to call INTERNAL commands (within HR module).
    private readonly IHumanResourcesDispatcher _dispatcher;

    // The API is used to talk to the OUTSIDE world (or publish public events).
    // CRITIQUE: Variable Naming. '_Api' violates naming conventions (should be '_identityApi' or similar).
    private readonly IIdentityAccessModule _Api;

    // HOW: Constructor Injection.
    // We ask for the tools we need to do the job.
    public HireEmployeeHandler(IHumanResourcesDispatcher dispatcher, IIdentityAccessModule orchestrator)
    {
        _dispatcher = dispatcher;
        _Api = orchestrator;
    }

    /// <summary>
    /// Executes the hiring workflow: Validate Budget -> Save Employee -> Notify System.
    /// </summary>
    public async ValueTask<Result<Guid>> Handle(HireEmployeeUseCase uc, CancellationToken ct)
    {
        // 1. GENERATE ID
        // HOW: We generate the ID here in the Application Layer, not the Database.
        // WHY: This allows us to use the ID in subsequent steps (like events) before the database even sees it.
        var empId = Guid.NewGuid();

        // 2. DISPATCH LOGIC (Step 1: Validation)
        // HOW: We delegate the complex rule "Do we have money?" to a specific handler.
        // WHY: If the logic for calculating budget changes (e.g., Q4 freeze), we update 'ValidateBudgetHandler', not this file.
        var hasBudget = await _dispatcher.ValidateBudget(new ValidateBudgetCommand(uc.DeptId), ct);

        // FAIL FAST:
        // We check the result immediately. If we have no money, we stop. We don't try to create the employee.
        if (!hasBudget)
        {
            return Result<Guid>.Failure("Department is broke");
        }

        // 3. DISPATCH INFRA (Step 2: Persistence)
        // HOW: Now that we know it's safe, we save the data.
        // This likely calls 'CreateEmployeeHandler' -> 'EmployeeRepository' -> 'INSERT INTO...'
        await _dispatcher.CreateEmployee(new CreateEmployeeCommand(empId, uc.Name), ct);

        // 4. PUBLISH EVENT (Step 3: Notification)
        // HOW: We announce to the system "Employee Hired!".
        // WHY: Other modules (like Facilities) are listening. They will now issue badges and assign desks 
        // without this handler needing to know they exist.
        _Api.PublishEmployeeHired(empId, uc.Name);

        // SUCCESS:
        // We return the new ID so the UI can redirect the user to the new employee's profile.
        return Result<Guid>.Success(empId);
    }
}