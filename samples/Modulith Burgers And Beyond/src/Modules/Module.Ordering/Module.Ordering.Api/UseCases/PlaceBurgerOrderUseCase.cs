using Faster.Modulith.Contracts;

namespace Module.Ordering.Api.UseCases;

public record struct PlaceBurgerOrderUseCase(string BurgerName, int TableNumber, string SpecialInstructions) : IUseCase<Result<Guid>>;