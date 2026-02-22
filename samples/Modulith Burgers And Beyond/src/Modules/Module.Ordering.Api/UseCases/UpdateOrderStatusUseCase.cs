using System;
using System.Collections.Generic;
using System.Text;
using Faster.Modulith.Contracts;

namespace Module.Ordering.Api.UseCases;

public record struct UpdateOrderStatusUseCase(Guid OrderId, OrderStatus NewStatus) : IUseCase<Result>;
