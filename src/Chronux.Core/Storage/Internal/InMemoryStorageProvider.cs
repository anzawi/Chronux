using Chronux.Core.Storage.Contracts;
using Chronux.Core.Storage.Models;
using System.Collections.Concurrent;

namespace Chronux.Core.Storage.Internal;

internal sealed class InMemoryStorageProvider : IChronuxStorageProvider
{
    private readonly ConcurrentDictionary<string, TriggerState> _triggers = new();
    private readonly ConcurrentDictionary<string, List<ExecutionLog>> _logs = new();
    private readonly ConcurrentQueue<EnqueuedJob> _queue = new();

    public Task<TriggerState?> GetTriggerStateAsync(string jobId, CancellationToken ct = default)
        => Task.FromResult(_triggers.GetValueOrDefault(jobId));

    public Task SetTriggerStateAsync(string jobId, TriggerState state, CancellationToken ct = default)
    {
        _triggers[jobId] = state;
        return Task.CompletedTask;
    }

    public Task RemoveTriggerStateAsync(string jobId, CancellationToken ct = default)
    {
        _triggers.TryRemove(jobId, out _);
        return Task.CompletedTask;
    }

    public Task AppendExecutionLogAsync(ExecutionLog log, CancellationToken ct = default)
    {
        var list = _logs.GetOrAdd(log.JobId, _ => []);
        lock (list)
        {
            list.Add(log);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ExecutionLog>> QueryExecutionLogsAsync(string jobId, int take = 100,
        CancellationToken ct = default)
    {
        if (_logs.TryGetValue(jobId, out var list))
        {
            lock (list)
            {
                return Task.FromResult<IReadOnlyList<ExecutionLog>>(list
                    .OrderByDescending(x => x.ExecutedAt)
                    .Take(take)
                    .ToList());
            }
        }

        return Task.FromResult<IReadOnlyList<ExecutionLog>>([]);
    }

    public Task EnqueueJobAsync(EnqueuedJob job, CancellationToken ct = default)
    {
        _queue.Enqueue(job);
        return Task.CompletedTask;
    }

    public Task<EnqueuedJob?> DequeueJobAsync(string jobId, CancellationToken ct = default)
    {
        if (_queue.TryDequeue(out var job) && job.JobId == jobId)
            return Task.FromResult<EnqueuedJob?>(job);

        return Task.FromResult<EnqueuedJob?>(null);
    }

    public Task PurgeExecutionLogsOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct)
    {
        foreach (var kvp in _logs)
        {
            var list = kvp.Value;
            lock (list)
            {
                list.RemoveAll(log => log.ExecutedAt < cutoff);
            }
        }

        return Task.CompletedTask;
    }

    public Task PurgeExecutionLogsOverLimitAsync(int maxPerJob, CancellationToken ct)
    {
        foreach (var kvp in _logs)
        {
            var list = kvp.Value;
            lock (list)
            {
                if (list.Count <= maxPerJob) continue;

                var toRemove = list
                    .OrderByDescending(x => x.ExecutedAt)
                    .Skip(maxPerJob)
                    .ToList();

                foreach (var log in toRemove)
                    list.Remove(log);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ExecutionLog>> GetExecutionLogsAsync(string jobId, CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<ExecutionLog>>(_logs.TryGetValue(jobId, out var list)
            ? list.ToList()
            : []);
    }

    public Task<IReadOnlyList<ExecutionLog>> GetAllExecutionLogsAsync(CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<ExecutionLog>>(_logs.Values.SelectMany(x => x).ToList());
    }
}