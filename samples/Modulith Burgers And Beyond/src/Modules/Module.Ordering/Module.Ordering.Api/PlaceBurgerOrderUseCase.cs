using Faster.Modulith.Contracts;

namespace Module.Ordering.Api;

public record PlaceBurgerOrderUseCase(string BurgerName, int TableNumber, string SpecialInstructions) : IUseCase<Result<Guid>>;