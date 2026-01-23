namespace Faster.Modulith.Behaviors;

public interface IAuditService
{
    Task LogAsync(string operation, string userId, string data, CancellationToken ct);
    Task LogSuccessAsync(string operation, string userId, CancellationToken ct);
    Task LogFailureAsync(string operation, string userId, string error, CancellationToken ct);
}