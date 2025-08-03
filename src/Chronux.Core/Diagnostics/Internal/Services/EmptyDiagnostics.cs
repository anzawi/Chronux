using Chronux.Core.Diagnostics.Contracts;
using Chronux.Core.Jobs.Models;

namespace Chronux.Core.Diagnostics.Internal.Services;

internal sealed class EmptyDiagnostics : IChronuxDiagnostics
{
    public void OnJobStart(string jobId, DateTimeOffset time)
    {
    }

    public void OnJobRetry(string jobId, int attempt, TimeSpan delay)
    {
    }

    public void OnJobSuccess(string jobId, JobResult result)
    {
    }

    public void OnJobFailure(string jobId, Exception ex, int attempt)
    {
    }

    public void OnJobChained(string from, string to)
    {
    }
}