using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BurgersAndBeyond.Shared.Behaviors;
using FluentValidation;
using Faster.Modulith.Contracts;

namespace Module.Feedback.Application.CommandHandlers;

/// <summary>
/// Internal specialist handler responsible for the persistence of customer feedback.
/// Enriched with LoggingBehavior to ensure a consistent audit trail.
/// </summary>
[EnrichWith(typeof(LoggingBehavior<SaveFeedbackCommand, Result>))]
internal class SaveFeedbackHandler : ICommandHandler<SaveFeedbackCommand, Result>
{
    /// <summary>
    /// Executes the primary intent of saving feedback into the Feedback Vault.
    /// </summary>
    public async ValueTask<Result> Handle(SaveFeedbackCommand command, CancellationToken ct)
    {
        // 1. Audit Log (Managed by LoggingBehavior via [EnrichWith])
        // The cross-cutting concern ensures a datetime-stamped log exists [cite: 2026-01-29].

        // 2. Logic Placeholder
        // This is where you will interact with the internal FeedbackDbContext.
        // var feedback = new FeedbackEntity(command.OrderId, command.Rating, command.Comment);
        // db.Feedback.Add(feedback);
        // await db.SaveChangesAsync(ct);

        return Result.Success;
    }
}

/// <summary>
/// The internal instruction for saving feedback. 
/// Using a record ensures immutability within the vault.
/// </summary>
internal record SaveFeedbackCommand(Guid OrderId, int Rating, string Comment) : ICommand<Result>;

/// <summary>
/// Validates the command structure before it reaches the handler.
/// </summary>
internal class SaveFeedbackValidator : AbstractValidator<SaveFeedbackCommand>
{
    public SaveFeedbackValidator()
    {
        RuleFor(c => c.OrderId).NotEmpty().WithMessage("OrderId is required for persistence.");
        RuleFor(c => c.Rating).InclusiveBetween(1, 5).WithMessage("Rating must be within the valid range (1-5).");
    }
}