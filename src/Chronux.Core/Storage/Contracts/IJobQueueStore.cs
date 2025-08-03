using Chronux.Core.Storage.Models;

namespace Chronux.Core.Storage.Contracts;

public interface IJobQueueStore
{
    Task EnqueueAsync(JobQueueItem item, CancellationToken ct);
    Task<JobQueueItem?> DequeueAsync(CancellationToken ct);
    Task AcknowledgeAsync(string id, CancellationToken ct);
}