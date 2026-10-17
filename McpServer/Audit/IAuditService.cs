namespace TradingMcp.Audit;

public interface IAuditService
{
    Task<IReadOnlyList<T>> RunAsync<T>(
        string tool, object args, Func<Task<IReadOnlyList<T>>> action);
}
