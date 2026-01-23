using Faster.Modulith.Contracts;
using FluentValidation;
using Module.Facilities.Infrastructure;

// NOTICE: We are in the 'Facilities' Module.
namespace Module.Facilities.Application.CommandHandlers;

/// <summary>
/// The "Facilities Manager" logic. It handles the request to give an employee a desk.
/// <para>
/// <b>Why exists?</b> This handler bridges the gap between the <i>Intent</i> (Allocate Desk) 
/// and the <i>Database</i> (saving the assignment). It contains the business rules for *how* a desk is chosen.
/// </para>
/// </summary>
/// <remarks>
/// <b>Primary Constructor:</b> We inject <see cref="DeskRepository"/> directly in the class declaration 
/// <c>(DeskRepository _repo)</c>. This is the Dependency Injection pattern—we don't create the database connection here; 
/// we just ask for a tool that knows how to use it.
/// </remarks>
internal class AllocateDeskHandler(DeskRepository repo) : ICommandHandler<AllocateDeskCommand, Result<bool>>
{
    /// <summary>
    /// Executes the allocation logic.
    /// </summary>
    public async ValueTask<Result<bool>> Handle(AllocateDeskCommand command, CancellationToken ct)
    {
        // BUSINESS LOGIC:
        // Here is the "Domain Rule": Desks are assigned randomly to floors 1-4.
        // We calculate this *before* calling the database.
        string floor = Random.Shared.Next(1, 5).ToString();

        // INFRASTRUCTURE CALL:
        // HOW: We use the repository to persist the decision. 
        // WHY: The Handler doesn't know *how* to write SQL or talk to the database. 
        // It just tells the Repository "Assign this ID to this Floor". This separates concerns.
        await repo.AssignDeskAsync(command.EmployeeId, $"Floor {floor}");

        return Result<bool>.Success(true);
    }
}

/// <summary>
/// The request data packet.
/// </summary>
internal record AllocateDeskCommand(Guid EmployeeId) : ICommand<Result<bool>>
{
    // GOOD: Using Guid ensures we have a unique, strong type for the Employee ID.
    public Guid EmployeeId { get; internal set; } = EmployeeId;
}

/// <summary>
/// The Guard Clause.
/// </summary>
internal class AllocateDeskValidator : AbstractValidator<AllocateDeskCommand>
{
    public AllocateDeskValidator()
    {
        // Example: RuleFor(c => c.EmployeeId).NotEmpty();
    }
}

