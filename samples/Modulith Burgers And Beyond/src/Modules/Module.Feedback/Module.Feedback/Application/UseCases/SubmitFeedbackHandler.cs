using Faster.Modulith;
using FluentValidation;
using Faster.Modulith.Contracts;
using Module.Feedback.Api.UseCases;
using Module.Feedback.Application.CommandHandlers;
using Module.Feedback.Infrastructure;

namespace Module.Feedback.Application.UseCases;

/// <summary>
/// The public entry point for customer feedback. 
/// This UseCase orchestrates the saving, signaling, and escalation of feedback.
/// </summary>
internal class SubmitFeedbackHandler(IFeedbackDispatcher dispatcher) : IUseCaseHandler<SubmitFeedbackUseCase, Result>
{
    private readonly SubmitFeedbackValidator _validator = new();

    /// <summary>
    /// Handles the submission of feedback for a specific order, validating the input and persisting the feedback.
    /// Note that this UseCaseHandler, uses a dispatcher to delegate persistence and escalation tasks to the appropriate handlers.
    /// Note different coding styles, use however you like
    /// </summary>
    /// <remarks>If validation fails, the method returns a failure result. The method also triggers an
    /// escalation for negative feedback ratings (2 or below) and publishes a signal for other modules to react to the
    /// feedback submission.</remarks>
    /// <param name="request">The feedback submission request containing the order ID, rating, and comment to be processed.</param>
    /// <param name="ct">A cancellation token to monitor for cancellation requests during the operation.</param>
    /// <returns>A result indicating the success or failure of the feedback submission process.</returns>
    public async ValueTask<Result> Handle(SubmitFeedbackUseCase request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure("Validation failed");
        }

        // 1. Audit Entry [cite: 2026-01-29]
        // Every entry into the vault is logged with a mandatory datetime.
        Console.WriteLine($"[{DateTime.UtcNow}]: FEEDBACK - Entry point reached for Order {request.OrderId}");

        // 2. Persistence Delegation
        // The UseCase doesn't touch the DB directly; it commands the internal vault to save.
        var result = await dispatcher.SaveFeedback(new SaveFeedbackCommand(request.OrderId, request.Rating, request.Comment), ct);

        if (result.IsSuccess)
        {
            // 3. Cross-Vault Signaling [cite: 2026-01-08]
            // We publish a signal so other modules (e.g., Marketing) can react without direct coupling.
            dispatcher.PublishFeedbackSubmittedSignal(request.Comment, request.Rating, ct);
        }

        // 4. The "Else" Logic: Reactive Escalation
        // For ratings <= 2, we trigger the internal escalation specialist handler.
        if (request.Rating <= 2)
        {
            // The UseCase acts as a traffic controller, ensuring negative feedback is never ignored.
            await dispatcher.EscalateNegativeFeedback(new EscalateNegativeFeedbackCommand(
              request.Comment, request.Rating, 1), ct);
        }

        return result;
    }
}

/// <summary>
/// Protects the vault by ensuring only valid data initiates the orchestration cycle.
/// </summary>
internal class SubmitFeedbackValidator : AbstractValidator<SubmitFeedbackUseCase>
{
    public SubmitFeedbackValidator()
    {
        RuleFor(c => c.OrderId).NotEmpty().WithMessage("OrderId must be provided.");
        RuleFor(c => c.Rating).InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
        RuleFor(c => c.Comment).MaximumLength(500).WithMessage("Comment is too long.");
    }
}