using Chronux.Core.Execution.Models;
using Chronux.Core.Jobs.Models;

namespace Chronux.Core.Middleware.Models;

public delegate ValueTask<JobResult> JobExecutionDelegate(JobExecutionContext context);
