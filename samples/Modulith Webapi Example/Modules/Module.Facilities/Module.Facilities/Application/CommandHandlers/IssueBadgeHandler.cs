using Faster.Modulith.Contracts;
using FluentValidation;
using Module.Facilities.Infrastructure; // Access to the hardware/infrastructure layer
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// NOTICE: We are in 'Module.Facilities'. This module is responsible for physical things like desks and badges.
namespace Module.Facilities.Application.CommandHandlers
{
    /// <summary>
    /// The "Badge Printer Operator". It receives a request and operates the hardware system.
    /// <para>
    /// <b>Why exists?</b> This handler acts as an <i>Adapter</i>. It translates our clean internal command 
    /// ("Issue Badge") into calls that the messy hardware system (`SmartCardSystem`) understands.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <b>Dependency Injection:</b> We inject <see cref="SmartCardSystem"/> directly. 
    /// This service likely contains specific code to talk to a physical card printer or external API.
    /// </remarks>
    internal class IssueBadgeHandler(SmartCardSystem smartCardSystem) : ICommandHandler<IssueBadgeCommand, Result<string>>
    {
        /// <summary>
        /// Triggers the badge printing process.
        /// </summary>
        public async ValueTask<Result<string>> Handle(IssueBadgeCommand command, CancellationToken ct)
        {
            // BUG ALERT: 
            // Look closely at the Command definition below. The constructor sets 'FirstName', 
            // but here we are passing 'command.Name'.
            // 'command.Name' is never assigned! This will likely send "null" to the printer.
            // FIX: Should be 'command.FirstName'.
            var badge = await smartCardSystem.PrintBadgeAsync(command.Name);

            return Result<string>.Success(badge);
        }
    }

    /// <summary>
    /// The data required to print a physical badge.
    /// </summary>
    internal record IssueBadgeCommand : ICommand<Result<string>>
    {
        /// <summary>
        /// Initializes the command.
        /// </summary>
        /// <param name="employeeId">The unique ID of the employee.</param>
        /// <param name="firstName">The name to print on the card.</param>
        public IssueBadgeCommand(Guid employeeId, string firstName)
        {
            EmployeeId = employeeId;

            // WE ASSIGN THIS:
            FirstName = firstName;

            // BUT WE FORGOT TO ASSIGN THIS:
            // Name = firstName; 
        }

        public Guid EmployeeId { get; }

        // This is where the data lives:
        public string FirstName { get; }

        // This is empty/null!
        // Junior Dev Tip: Delete unused properties to avoid this exact confusion.
        public string Name { get; internal set; }
    }

    /// <summary>
    /// The Guard Clause.
    /// </summary>
    internal class IssueBadgeValidator : AbstractValidator<IssueBadgeCommand>
    {
        public IssueBadgeValidator()
        {
            // Example: RuleFor(c => c.FirstName).NotEmpty().MaximumLength(50);
        }
    }
}