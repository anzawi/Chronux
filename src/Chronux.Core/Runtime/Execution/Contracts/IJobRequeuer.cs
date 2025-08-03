using Chronux.Core.Enqueuing.Models;

namespace Chronux.Core.Runtime.Execution.Contracts;

public interface IJobRequeuer
{
    Task RequeueAsync(
        string jobId,
        object? input,
        EnqueueMetadata? metadata = null,
        CancellationToken ct = default);

    Task RequeueFromDeadLetterAsync(
        Guid deadLetterId,
        CancellationToken ct = default);
}