namespace Faster.Modulith.Behaviors;

public interface IMetricsCollector
{
    void RecordSuccess(string operationName, long durationMs);
    void RecordFailure(string operationName, long durationMs, System.Exception ex);
}