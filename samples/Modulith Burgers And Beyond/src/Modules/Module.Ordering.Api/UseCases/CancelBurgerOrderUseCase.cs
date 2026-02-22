using System;
using Faster.Modulith.Contracts;

namespace Module.Ordering.Api.UseCases;

public record CancelBurgerOrderUseCase(Guid OrderId, string Reason) : IUseCase<Result>;
