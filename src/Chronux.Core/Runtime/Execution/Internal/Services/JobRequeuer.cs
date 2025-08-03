using Chronux.Core.Enqueuing.Contracts;
using Chronux.Core.Enqueuing.Models;
using Chronux.Core.Runtime.Execution.Contracts;
using Chronux.Core.Storage.Contracts;

namespace Chronux.Core.Runtime.Execution.Internal.Services;

internal sealed class JobRequeuer(
    IDeadLetterStore dlq,
    IJobEnqueuer enqueuer,
    IChronuxStorageProvider storage)
    : IJobRequeuer
{
    public Task RequeueAsync(
        string jobId,
        object? input,
        EnqueueMetadata? metadata = null,
        CancellationToken ct = default)
    {
        return enqueuer.EnqueueAsync(jobId, input, metadata, ct);
    }

    public async Task RequeueFromDeadLetterAsync(
        Guid deadLetterId,
        CancellationToken ct = default)
    {
        var item = await dlq.GetByIdAsync(deadLetterId, ct);
        if (item is null)
            throw new InvalidOperationException($"No dead letter item found for ID '{deadLetterId}'");

        var metadata = item.Metadata;

        await enqueuer.EnqueueAsync(item.JobId, item.Input, metadata, ct);

        // Optional: log or remove after requeue
    }
}
