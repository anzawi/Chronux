using Chronux.Core.Jobs.Models;
using Microsoft.Extensions.Logging;

namespace Chronux.Core.Diagnostics.Services;

public sealed class LoggingDiagnostics(ILoggerFactory loggerFactory, string? category = null)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger(category ?? "Chronux.Diagnostics");

    public void OnJobStart(string jobId, DateTimeOffset time)
        => _logger.LogDebug("Starting job {JobId} at {Time}", jobId, time);

    public void OnJobRetry(string jobId, int attempt, TimeSpan delay)
        => _logger.LogWarning("Retry {Attempt} for job {JobId} after {Delay}", attempt, jobId, delay);

    public void OnJobSuccess(string jobId, JobResult result)
        => _logger.LogInformation("Job {JobId} succeeded");

    public void OnJobFailure(string jobId, Exception ex, int attempt)
        => _logger.LogError(ex, "Job {JobId} failed on attempt {Attempt}", jobId, attempt);

    public void OnJobChained(string from, string to)
        => _logger.LogDebug("Chained job: {From} → {To}", from, to);
}