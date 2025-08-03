using Chronux.Core.Enqueuing.Models;

namespace Chronux.Core.Enqueuing.Contracts;

public interface IJobEnqueuer
{
    Task EnqueueAsync(string jobId,
        object? input = null,
        CancellationToken cancellationToken = default);

    Task EnqueueAsync(
        string jobId,
        object? input = null,
        EnqueueMetadata? metadata = null,
        CancellationToken cancellationToken = default);
}