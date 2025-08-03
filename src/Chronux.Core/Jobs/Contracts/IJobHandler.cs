using Chronux.Core.Jobs.Models;

namespace Chronux.Core.Jobs.Contracts;

public interface IJobHandler<TContext>
{
    ValueTask<JobResult> ExecuteAsync(JobContext<TContext> context, CancellationToken cancellationToken);
}