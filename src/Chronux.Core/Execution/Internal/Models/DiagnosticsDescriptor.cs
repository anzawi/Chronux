using Chronux.Core.Jobs.Models;

namespace Chronux.Core.Execution.Internal.Models;

internal class DiagnosticsDescriptor
{
    public string JobId { get; set; } = null!;
    public DiagnosticsOrder FireOn { get; set; }
    public InternalJobExecutionContext? Context { get; set; }
    public string? Next { get; set; }
    public JobResult? JobResult { get; set; }
    public Exception? Exception { get; set; }
    public int Attempt { get; set; }
    public TimeSpan? Delay { get; set; }
}