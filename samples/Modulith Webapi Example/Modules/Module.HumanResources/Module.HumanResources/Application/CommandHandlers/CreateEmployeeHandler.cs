using Faster.Modulith.Behaviors;
using Faster.Modulith.Contracts;
using FluentValidation;
using Module.Facilities.Application.CommandHandlers;
using Module.HumanResources.Infrastructure; // Access to the database layer

// NOTICE: We are in 'Module.HumanResources'. This is the module responsible for people.
namespace Module.HumanResources.Application.CommandHandlers
{
    /// <summary>
    /// The worker responsible for adding a new employee to the system.
    /// <para>
    /// <b>Why exists?</b> This follows the Command Pattern. Instead of a "God Class" (HRManager) with 100 methods, 
    /// we have one small class that does one thing: Creates Employees. This makes it easy to read and test.
    /// </para>
    /// </summary>
    [EnrichWith(typeof(PerformanceBehavior<,>))]
    internal class CreateEmployeeHandler(EmployeeRepository repo) : ICommandHandler<CreateEmployeeCommand, Result>
    {
        /// <summary>
        /// Executes the creation logic.
        /// </summary>
        public async ValueTask<Result> Handle(CreateEmployeeCommand cmd, CancellationToken ct)
        {
            // Look at the Command class below.
            // The constructor assigns values to properties 'EmpId' and 'Name1'.
            // But here, we are reading 'cmd.Id' and 'cmd.Name'.
            // These properties are never set! The database will receive Empty GUIDs and Null Names.
            await repo.SaveAsync(cmd.Id, cmd.Name);

            return Result.Success;
        }
    }

    /// <summary>
    /// The "Request Form" containing the new employee's details.
    /// </summary>
    internal record CreateEmployeeCommand : ICommand<Result>
    {
        /// <summary>
        /// Initializes the command.
        /// </summary>
        public CreateEmployeeCommand(Guid empId, object name)
        {
            // MAPPING: We put data into specific buckets here...
            EmpId = empId;
            Name1 = name;

            // ...but we forgot to put data into the buckets the Handler uses ('Id' and 'Name')!
            // FIX:
            // Id = empId;
            // Name = name.ToString();
        }

        // CRITIQUE: Duplicate Properties.
        // We have 'Id' vs 'EmpId'. We have 'Name' vs 'Name1'.
        // This is confusing. Pick ONE name for each concept and stick to it.
        public Guid Id { get; internal set; }
        public string Name { get; internal set; }

        public Guid EmpId { get; }

        // CRITIQUE: Weak Typing.
        // Why is Name1 an 'object'? It should be a 'string'.
        // Using object forces us to cast it later and risks runtime errors.
        public object Name1 { get; }
    }

    /// <summary>
    /// The Guard Clause.
    /// </summary>
    internal class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
    {
        public CreateEmployeeValidator()
        {
            // Example Rule:
            // RuleFor(c => c.Name).NotEmpty().WithMessage("Employee must have a name.");
        }
    }
}