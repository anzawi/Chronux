using Chronux.Core.Execution.Internal.Models;
using Chronux.Core.Jobs.Models;

namespace Chronux.Core.Execution.Internal.Contracts;

internal interface IJobExecutor
{
    ValueTask<JobResult> ExecuteAsync(InternalJobExecutionContext context);
    bool IsRunning(string jobId);
}