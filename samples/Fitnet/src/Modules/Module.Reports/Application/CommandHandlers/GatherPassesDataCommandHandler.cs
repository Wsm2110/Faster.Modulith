using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Faster.Modulith.Contracts;

namespace Module.Reports.Application.CommandHandlers;

internal class GatherPassesDataCommandHandler : ICommandHandler<GatherPassesDataCommand, Result>
{
    /// <summary>
    /// Processes the command to gather pass data.
    /// </summary>
    /// <param name="command">The data gathering parameters.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A value task representing the asynchronous operation, containing the result.</returns>
    public ValueTask<Result> Handle(GatherPassesDataCommand command, CancellationToken ct)
    {
        Console.WriteLine($"[{DateTime.UtcNow:O}] Gathering raw pass data for {command.TargetMonth}/{command.TargetYear}.");

        // Domain logic to query the data store would execute here.
        Console.WriteLine($"[{DateTime.UtcNow:O}] Successfully retrieved pass data records.");

        return ValueTask.FromResult(Result.Success);
    }
}

/// <summary>
/// Internal command to gather the raw pass generation data from the database.
/// </summary>
/// <param name="TargetMonth">The target month.</param>
/// <param name="TargetYear">The target year.</param>
internal record GatherPassesDataCommand(int TargetMonth, int TargetYear) : ICommand<Result>;

/// <summary>
/// Provides validation logic for the GatherPassesDataCommand using predefined validation rules.
/// </summary>
/// <remarks>This validator is intended to be used with the FluentValidation framework to ensure that instances of
/// GatherPassesDataCommand meet required criteria before processing. Validation rules should be defined within the
/// constructor.</remarks>
internal class GatherPassesDataValidator : AbstractValidator<GatherPassesDataCommand>
{
    public GatherPassesDataValidator()
    {
        // RuleFor(c => c.Id).NotEqual(0).WithMessage("Id cannot be 0");
    }
}