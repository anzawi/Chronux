using System.Threading.Channels;
using Chronux.Core.Execution.Internal.Services;
using Chronux.Core.Runtime.Status.Contracts;
using Chronux.Core.Runtime.Status.Models;
using Chronux.Core.Scheduling.Internal.Contracts;
using Chronux.Core.Storage.Contracts;
using Chronux.Core.Storage.Models;

namespace Chronux.Core.Runtime.Status.Internal.Services;

internal sealed class JobStatusProvider(
    IChronuxStorageProvider storage,
    IDeadLetterStore dlq,
    Channel<EnqueuedJob> queue,
    IJobRegistry registry,
    JobExecutor executor) : IJobStatusProvider
{
    public async Task<JobStatus?> GetStatusAsync(string jobId, CancellationToken ct = default)
    {
        var logs = await storage.GetExecutionLogsAsync(jobId, ct);
        var latest = logs.OrderByDescending(x => x.ExecutedAt).FirstOrDefault();

        var dead = (await dlq.GetAllAsync(ct)).FirstOrDefault(x => x.JobId == jobId);
        var queued = queue.Reader.Count > 0 &&
                     queue.Reader.TryPeek(out var peeked) &&
                     peeked?.JobId == jobId;

        var running = executor.IsRunning(jobId);

        var state = dead is not null ? JobState.DeadLettered
                  : running ? JobState.Running
                  : queued ? JobState.Queued
                  : latest?.Success == true ? JobState.Succeeded
                  : latest is not null ? JobState.Failed
                  : (JobState?)null;

        if (state is null) return null;

        return new JobStatus
        {
            JobId = jobId,
            State = state.Value,
            LastRun = latest?.ExecutedAt,
            LastSuccess = latest?.Success,
            RetryAttempt = latest?.RetryAttempt,
            ErrorMessage = latest?.Exception?.Message,
            Tags = latest?.Tags,

            CorrelationId = latest?.CorrelationId ?? dead?.Metadata?.CorrelationId,
            TriggerSource = latest?.TriggerSource ?? dead?.Metadata?.TriggerSource,
            UserId = latest?.UserId ?? dead?.Metadata?.UserId
        };
    }

    public async Task<IReadOnlyList<JobStatus>> GetAllStatusesAsync(CancellationToken ct = default)
    {
        var jobIds = registry.All.Select(j => j.Id).ToList();
        var result = new List<JobStatus>();

        foreach (var jobId in jobIds)
        {
            var status = await GetStatusAsync(jobId, ct);
            if (status is not null)
                result.Add(status);
        }

        return result;
    }
}
