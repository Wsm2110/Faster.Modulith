using Faster.Modulith.Contracts;

namespace Module.Robotics.Api.UseCases;

public record struct DeliverFoodUseCase(Guid OrderId, int TableNumber) : IUseCase<Result>;
