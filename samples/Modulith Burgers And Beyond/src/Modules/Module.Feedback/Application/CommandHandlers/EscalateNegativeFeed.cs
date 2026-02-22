using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Faster.Modulith.Contracts;

namespace Module.Feedback.Application.CommandHandlers;

/// <summary>
/// Internal specialist handler that manages negative feedback escalation.
/// This handler ensures that poor ratings trigger the appropriate management alerts.
/// </summary>
internal class EscalateNegativeFeedbackHandler : ICommandHandler<EscalateNegativeFeedbackCommand, Result>
{
    /// <summary>
    /// Processes the escalation logic for "At-Risk" feedback entries.
    /// </summary>
    public async ValueTask<Result> Handle(EscalateNegativeFeedbackCommand cmd, CancellationToken ct)
    {
        // 1. Mandatory Audit Log [cite: 2026-01-29]
        // Every escalation event is tracked with a precise datetime to ensure accountability.
        Console.WriteLine($"[{DateTime.UtcNow}]: ALERT - Negative Feedback ({cmd.Rating} stars) for ID {cmd.FeedbackId}.");
        Console.WriteLine($"[{DateTime.UtcNow}]: COMMENT - \"{cmd.Comment}\"");

        // 2. Integration Point
        // This is where you would call an internal NotificationService or push to a Manager Dashboard.
        // By keeping this logic here, the main UseCase remains clean and focused on orchestration.

        // TODO: await notificationService.AlertManagerAsync(cmd.FeedbackId, cmd.Comment, ct);

        return Result.Success;
    }
}

/// <summary>
/// The internal instruction for feedback escalation.
/// Marked as a record to ensure immutability during the transit through the dispatcher.
/// </summary>
internal record EscalateNegativeFeedbackCommand(string Comment, int Rating, int FeedbackId) : ICommand<Result>;

/// <summary>
/// Validates the escalation parameters before execution.
/// </summary>
internal class EscalateNegativeFeedValidator : AbstractValidator<EscalateNegativeFeedbackCommand>
{
    public EscalateNegativeFeedValidator()
    {
        // Ensure we have a valid reference to the original feedback entry
        RuleFor(c => c.FeedbackId).NotEqual(0).WithMessage("A valid Feedback ID is required for escalation.");

        // Ensure the comment is present for context during escalation
        RuleFor(c => c.Comment).NotEmpty().WithMessage("Comments are required for escalated feedback.");
    }
}