using Chronux.Core.Enqueuing.Models;
using Chronux.Core.Execution.Contracts;
using Chronux.Core.Execution.Internal.Contracts;
using Chronux.Core.Execution.Internal.Models;
using Chronux.Core.Jobs.Models;
using Chronux.Core.Scheduling.Internal.Contracts;

namespace Chronux.Core.Execution.Internal.Services;

internal sealed class JobDispatcher(
    IJobRegistry registry,
    IJobExecutor executor)
    : IJobDispatcher
{
    public async ValueTask<JobResult> DispatchAsync(string jobId, object? input, CancellationToken cancellationToken)
    {
        var def = registry.Get(jobId)
                  ?? throw new InvalidOperationException($"No job registered with ID: {jobId}");

        var ctx = new InternalJobExecutionContext
        {
            Definition = def,
            Input = input,
            CancellationToken = cancellationToken
        };


        return await executor.ExecuteAsync(ctx);
    }

    public ValueTask<JobResult> DispatchAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return DispatchAsync(jobId, null, cancellationToken);
    }
    
    public async ValueTask<JobResult> DispatchAsync(string jobId, object? input, EnqueueMetadata? metadata, CancellationToken cancellationToken)
    {
        var def = registry.Get(jobId)
                  ?? throw new InvalidOperationException($"No job registered with ID: {jobId}");

        var ctx = new InternalJobExecutionContext
        {
            Definition = def,
            Input = input,
            CancellationToken = cancellationToken,
            Metadata = metadata,
        };

        return await executor.ExecuteAsync(ctx);
    }
}