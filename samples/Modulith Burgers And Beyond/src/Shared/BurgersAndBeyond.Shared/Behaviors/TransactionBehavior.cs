using Faster.Modulith.Contracts;

namespace BurgersAndBeyond.Shared.Behaviors;

/// <summary>
/// Wraps execution in a database transaction
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IDbContext _dbContext;

    public TransactionBehavior(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        await using var transaction = await _dbContext.BeginTransactionAsync(ct);

        try
        {
            var response = await next();
            await transaction.CommitAsync(ct);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}