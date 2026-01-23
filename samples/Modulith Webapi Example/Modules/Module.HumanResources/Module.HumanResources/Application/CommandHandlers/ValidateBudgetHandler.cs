using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Faster.Modulith.Contracts;

// NOTICE: We are in 'Module.HumanResources'. 
// This logic is isolated to HR. Even if Sales needs to check a budget, 
// they must send a request to HR, they cannot calculate it themselves.
namespace Module.HumanResources.Application.CommandHandlers
{
    /// <summary>
    /// A "Rule Checker" handler. It verifies if a department has enough money.
    /// <para>
    /// <b>Why exists?</b> This encapsulates a specific Business Rule. 
    /// Instead of burying this `if (budget > 0)` check inside a massive "CreateEmployee" method, 
    /// we pull it out into its own class. This makes the rule reusable and testable.
    /// </para>
    /// </summary>
    internal class ValidateBudgetHandler : ICommandHandler<ValidateBudgetCommand, bool>
    {
        /// <summary>
        /// Executes the rule check synchronously.
        /// </summary>
        public ValueTask<bool> Handle(ValidateBudgetCommand cmd, CancellationToken ct)
        {
            // ARCHITECTURE NOTE: Logic in Handler vs. Service?
            // "We don't need a service for this..."
            // Correct! If the logic is self-contained (e.g., comparing numbers), put it right here.
            // You only need a separate "BudgetService" if you are sharing this logic across multiple Handlers.

            // SIMULATION:
            bool hasFunds = true; // e.g. cmd.DeptId != 0;

            // HOW: Returning ValueTask with a result.
            // Since we didn't use 'await' (no DB call), we wrap the result manually.
            // This is extremely efficient (zero memory allocation overhead).
            return new ValueTask<bool>(hasFunds);
        }
    }

    /// <summary>
    /// The input data for the rule check.
    /// </summary>
    internal record ValidateBudgetCommand : ICommand<bool>
    {
        public ValidateBudgetCommand(ulong deptId)
        {
            DeptId = deptId;
        }

        // CRITIQUE: Data Type Choice (ulong).
        // Using 'ulong' (unsigned long integer) implies you are likely using SQL Identity columns (1, 2, 3...).
        // While efficient, it makes it harder to generate IDs in the code (unlike Guid).
        // It also limits you: you can't have a Department ID of -1 for "Internal".
        public ulong DeptId { get; }
    }

    /// <summary>
    /// The Guard Clause.
    /// </summary>
    internal class ValidateBudgetValidator : AbstractValidator<ValidateBudgetCommand>
    {
        public ValidateBudgetValidator()
        {
            // Example: RuleFor(c => c.DeptId).GreaterThan(0);
        }
    }
}