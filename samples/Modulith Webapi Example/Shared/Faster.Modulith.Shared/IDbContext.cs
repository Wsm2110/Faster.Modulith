namespace Faster.Modulith.Behaviors;

public interface IDbContext
{
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken ct);
}