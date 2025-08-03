using Chronux.Core.Execution.Models;
using Chronux.Core.Jobs.Models;
using Chronux.Core.Middleware.Models;

namespace Chronux.Core.Middleware.Contracts;

public interface IJobMiddleware
{
    ValueTask<JobResult> InvokeAsync(JobExecutionContext context, JobExecutionDelegate next);
}