using Chronux.Core.Enqueuing.Models;
using Chronux.Core.Jobs.Models;

namespace Chronux.Core.Execution.Contracts;


internal interface IJobDispatcher
{
    ValueTask<JobResult> DispatchAsync(string jobId, object? input, CancellationToken cancellationToken = default);
    ValueTask<JobResult> DispatchAsync(string jobId, CancellationToken cancellationToken = default);
    ValueTask<JobResult> DispatchAsync(string jobId, object? input, EnqueueMetadata? metadata, CancellationToken cancellationToken = default);
}