using Chronux.Core.Storage.Contracts;
using Chronux.Core.Storage.Models;

namespace Chronux.Core.Storage.providers;

public sealed class InMemoryJobQueueStore : IJobQueueStore
{
    private readonly Queue<JobQueueItem> _queue = new();
    private readonly Lock _lock = new();

    public Task EnqueueAsync(JobQueueItem item, CancellationToken ct)
    {
        lock (_lock)
        {
            _queue.Enqueue(item);
        }
        return Task.CompletedTask;
    }

    public Task<JobQueueItem?> DequeueAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            return Task.FromResult(_queue.Count > 0 ? _queue.Dequeue() : null);
        }
    }

    public Task AcknowledgeAsync(string id, CancellationToken ct)
    {
        // No-op in memory; real stores may use this to remove / mark complete
        return Task.CompletedTask;
    }
}
