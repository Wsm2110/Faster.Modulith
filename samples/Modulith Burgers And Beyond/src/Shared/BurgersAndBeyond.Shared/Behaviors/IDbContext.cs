namespace BurgersAndBeyond.Shared.Behaviors;

public interface IDbContext
{
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken ct);
}