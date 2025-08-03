using Chronux.Core.Metrics.Contracts;
using Chronux.Core.Metrics.Models;
using Chronux.Core.Storage.Contracts;

namespace Chronux.Core.Metrics.Internal.Services;

internal sealed class ExecutionMetricsProvider(
    IChronuxStorageProvider storage,
    IDeadLetterStore dlq)
    : IExecutionMetricsProvider
{
    public async Task<JobMetrics?> GetMetricsAsync(string jobId, CancellationToken ct = default)
    {
        var logs = await storage.GetExecutionLogsAsync(jobId, ct);
        if (logs.Count == 0)
            return null;

        var deadLetters = await dlq.GetAllAsync(ct);
        var dlCount = deadLetters.Count(x => x.JobId == jobId);

        var total = logs.Count;
        var success = logs.Count(x => x.Success);
        var failure = total - success;
        var totalRetries = logs.Sum(x => x.RetryAttempt ?? 0);
        var avgMs = logs.Count > 0 ? logs.Average(x => x.Duration.TotalMilliseconds) : 0;

        return new JobMetrics
        {
            JobId = jobId,
            TotalExecutions = total,
            SuccessCount = success,
            FailureCount = failure,
            DeadLetterCount = dlCount,
            TotalRetryAttempts = totalRetries,
            AverageDurationMs = avgMs
        };
    }

    public async Task<IReadOnlyList<JobMetrics>> GetAllMetricsAsync(CancellationToken ct = default)
    {
        var logs = await storage.GetAllExecutionLogsAsync(ct);
        var grouped = logs.GroupBy(x => x.JobId).ToList();
        var deadLetters = await dlq.GetAllAsync(ct);

        var result = new List<JobMetrics>();

        foreach (var group in grouped)
        {
            var jobId = group.Key;
            var entries = group.ToList();
            var total = entries.Count;
            var success = entries.Count(x => x.Success);
            var failure = total - success;
            var retry = entries.Sum(x => x.RetryAttempt ?? 0);
            var avg = entries.Average(x => x.Duration.TotalMilliseconds);
            var dead = deadLetters.Count(x => x.JobId == jobId);

            result.Add(new JobMetrics
            {
                JobId = jobId,
                TotalExecutions = total,
                SuccessCount = success,
                FailureCount = failure,
                DeadLetterCount = dead,
                TotalRetryAttempts = retry,
                AverageDurationMs = avg
            });
        }

        return result;
    }
}