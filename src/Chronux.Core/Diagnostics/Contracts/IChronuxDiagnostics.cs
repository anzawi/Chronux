using Chronux.Core.Jobs.Models;

namespace Chronux.Core.Diagnostics.Contracts;

public interface IChronuxDiagnostics
{
    void OnJobStart(string jobId, DateTimeOffset scheduledTime);
    void OnJobRetry(string jobId, int attempt, TimeSpan delay);
    void OnJobSuccess(string jobId, JobResult result);
    void OnJobFailure(string jobId, Exception ex, int attempt);
    void OnJobChained(string fromJobId, string toJobId);
}