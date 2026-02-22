using System;
using Faster.Modulith.Contracts;

namespace Module.Kitchen.Api.UseCases;

public record CompleteCookingUseCase(Guid OrderId) : IUseCase<Result>;
