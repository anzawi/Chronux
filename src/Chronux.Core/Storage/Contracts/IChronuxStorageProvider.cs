using Chronux.Core.Storage.Models;

namespace Chronux.Core.Storage.Contracts;

public interface IChronuxStorageProvider
{
    Task<TriggerState?> GetTriggerStateAsync(string jobId, CancellationToken ct = default);
    Task SetTriggerStateAsync(string jobId, TriggerState state, CancellationToken ct = default);
    Task RemoveTriggerStateAsync(string jobId, CancellationToken ct = default);

    Task AppendExecutionLogAsync(ExecutionLog log, CancellationToken ct = default);

    Task<IReadOnlyList<ExecutionLog>> QueryExecutionLogsAsync(string jobId, int take = 100,
        CancellationToken ct = default);

    Task EnqueueJobAsync(EnqueuedJob job, CancellationToken ct = default);
    Task<EnqueuedJob?> DequeueJobAsync(string jobId, CancellationToken ct = default);
    Task PurgeExecutionLogsOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct);
    Task PurgeExecutionLogsOverLimitAsync(int maxPerJob, CancellationToken ct);
    
    Task<IReadOnlyList<ExecutionLog>> GetExecutionLogsAsync(string jobId, CancellationToken ct);
    Task<IReadOnlyList<ExecutionLog>> GetAllExecutionLogsAsync(CancellationToken ct);

}