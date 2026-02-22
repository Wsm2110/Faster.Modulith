using System;
using Faster.Modulith.Contracts;

namespace Module.Ordering.Api.UseCases;

public record struct PayOrderUseCase(Guid OrderId) : IUseCase<Result>;
