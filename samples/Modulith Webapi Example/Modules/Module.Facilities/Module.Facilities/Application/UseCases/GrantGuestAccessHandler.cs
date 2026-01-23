using FluentValidation;
using Faster.Modulith.Contracts; // Access to standard Result<T> types
using Module.Facilities.Api; // Access to the Public Contract (GrantGuestAccessUseCase)
using Module.Facilities.Infrastructure; // Access to the Hardware layer

namespace Module.Facilities.Application.UseCases
{
    /// <summary>
    /// Handles the public request to create a temporary guest pass.
    /// <para>
    /// <b>Why exists?</b> This is a "Simple Use Case". 
    /// Sometimes, a public request corresponds strictly to a single action (Printing a badge).
    /// In these cases, we don't need to over-engineer it by creating an internal Command and a second Handler.
    /// We just do the work right here.
    /// </para>
    /// </summary>
    internal class GrantGuestAccessHandler(SmartCardSystem smartCardSystem) : IUseCaseHandler<GrantGuestAccessUseCase, Result<string>>
    {
        /// <summary>
        /// Executes the logic to print a guest badge.
        /// </summary>
        public async ValueTask<Result<string>> Handle(GrantGuestAccessUseCase useCase, CancellationToken ct)
        {
            // HOW: We call the hardware service directly.
            // WHY: There is no complex business logic (like "Check Stock" or "Calculate Price").
            // We just need to prepend "GUEST-" and print it. 
            // Creating a separate "PrintGuestBadgeCommand" would be unnecessary boilerplate code here.
            var code = await smartCardSystem.PrintBadgeAsync($"GUEST-{useCase.VisitorName}");

            // HOW: Implicit Conversion.
            // The variable 'code' is a 'string', but the method returns 'Result<string>'.
            // The 'Result' class likely has an 'implicit operator' that automatically wraps the string 
            // into a Success result. This makes the code cleaner/shorter.
            return code; // Implicit result cast from string -> Result<string>
        }
    }

    /// <summary>
    /// The Guard Clause.
    /// </summary>
    internal class GrantGuestAccessValidator : AbstractValidator<GrantGuestAccessUseCase>
    {
        public GrantGuestAccessValidator()
        {
            // Example: RuleFor(c => c.VisitorName).NotEmpty().MinimumLength(3);
        }
    }
}