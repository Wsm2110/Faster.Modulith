namespace BurgersAndBeyond.Shared.Behaviors;

public interface IDbTransaction : System.IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}