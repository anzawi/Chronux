using System.Threading.Channels;
using Chronux.Core.Enqueuing.Contracts;
using Chronux.Core.Enqueuing.Models;
using Chronux.Core.Storage.Models;

namespace Chronux.Core.Enqueuing.Internal;


internal sealed class InMemoryJobEnqueuer(Channel<EnqueuedJob> queue) : IJobEnqueuer
{
    public Task EnqueueAsync(string jobId, object? input = null, CancellationToken cancellationToken = default)
    {
        var job = new EnqueuedJob
        {
            JobId = jobId,
            Input = input,
            EnqueuedAt = DateTimeOffset.UtcNow
        };

        return queue.Writer.WriteAsync(job, cancellationToken).AsTask();
    }

    public Task EnqueueAsync(string jobId, object? input = null, EnqueueMetadata? metadata = null, CancellationToken cancellationToken = default)
    {
        var job = new EnqueuedJob
        {
            JobId = jobId,
            Input = input,
            EnqueuedAt = DateTimeOffset.UtcNow,
            RequestId = Guid.NewGuid().ToString(),
            Metadata = metadata
        };

        return queue.Writer.WriteAsync(job, cancellationToken).AsTask();
    }
}