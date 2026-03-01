using Faster.Modulith.Contracts;
using FluentValidation;
using Module.Reports.Api;
using Module.Reports.Application.CommandHandlers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Module.Reports.Application.UseCases;

internal class GenerateReportUseCaseHandler(IReportsDispatcher dispatcher) : IUseCaseHandler<GenerateReportUseCase, Result>
{

    /// <summary>
    /// Coordinates the internal workflow to generate the report and trigger the subsequent event.
    /// </summary>
    /// <param name="useCase">The use case parameters.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A value task representing the asynchronous operation, containing the result.</returns>
    public async ValueTask<Result> Handle(GenerateReportUseCase useCase, CancellationToken ct)
    {
        Console.WriteLine($"[{DateTime.UtcNow:O}] Use Case Entry Point hit: GeneratePassesReportUseCase. Orchestrating internal commands.");

        if (useCase.Year > DateTime.UtcNow.Year || (useCase.Year == DateTime.UtcNow.Year && useCase.Month > DateTime.UtcNow.Month))
        {
            Console.WriteLine($"[{DateTime.UtcNow:O}] Validation failed: Cannot generate reports for future dates.");
            return Result.Failure("Target date cannot be in the future.");
        }

        // Step 1: Dispatch internal data gathering command
        var gatherCommand = new GatherPassesDataCommand(useCase.Month, useCase.Year);
        var gatherResult = await dispatcher.GatherPassesData(gatherCommand);

        if (!gatherResult.IsSuccess)
        {
            return gatherResult;
        }

        // Step 2: Dispatch internal formatting command
        var formatCommand = new FormatPassesReportCommand(useCase.Month, useCase.Year, "ExtractedDataPayload");
        var formatResult = await dispatcher.FormatPassesReport(formatCommand, ct);

        if (!formatResult.IsSuccess)
        {
            return formatResult;
        }

        // Step 3: Publish completion event
        var reportGeneratedEvent = new PassesReportGeneratedEvent(useCase.Month, useCase.Year);
        await dispatcher.PublishPassesReportGeneratedAsync(reportGeneratedEvent, ct);

        Console.WriteLine($"[{DateTime.UtcNow:O}] PassesReportGeneratedEvent published to the event bus.");

        return Result.Success;
    }
}

internal class GeneratePassesReportValidator : AbstractValidator<GenerateReportUseCase>
{
    public GeneratePassesReportValidator()
    {
        // RuleFor(c => c.Id).NotEqual(0).WithMessage("Id cannot be 0");
    }
}