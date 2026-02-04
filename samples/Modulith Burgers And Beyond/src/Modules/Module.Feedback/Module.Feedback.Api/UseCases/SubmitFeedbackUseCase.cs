using System;
using Faster.Modulith.Contracts;

namespace Module.Feedback.Api.UseCases;

public record struct SubmitFeedbackUseCase(Guid OrderId, int Rating, string Comment) : IUseCase<Result>;

