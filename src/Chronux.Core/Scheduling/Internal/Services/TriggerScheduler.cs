using Chronux.Core.Execution.Contracts;
using Chronux.Core.Scheduling.Internal.Contracts;
using Chronux.Core.Scheduling.Models;
using Chronux.Core.Storage.Contracts;
using Chronux.Core.Storage.Models;
using Microsoft.Extensions.Logging;

namespace Chronux.Core.Scheduling.Internal.Services;

internal sealed class TriggerScheduler(
    IJobRegistry registry,
    IJobDispatcher dispatcher,
    IChronuxStorageProvider storage,
    ILogger<TriggerScheduler> logger,
    TimeProvider timeProvider,
    TimeSpan pollInterval,
    TimeZoneInfo defaultTimeZone) : ITriggerScheduler
{
    private volatile TriggerSchedulerStatus _status = TriggerSchedulerStatus.NotStarted;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Trigger scheduler started");
        _status = TriggerSchedulerStatus.Running;

        while (!cancellationToken.IsCancellationRequested  && _status != TriggerSchedulerStatus.Stopped)
        {
            if (_status == TriggerSchedulerStatus.Paused)
            {
                await Task.Delay(pollInterval, cancellationToken);
                continue;
            }
            
            try
            {
                var jobs = registry.All;
                var now = timeProvider.GetUtcNow();

                foreach (var job in jobs)
                {
                    if (job.Trigger is null)
                        continue;

                    var storedState = await storage.GetTriggerStateAsync(job.Id, cancellationToken);
                    var triggerZone = job.Trigger.TimeZone ?? defaultTimeZone;
                    var localNow = TimeZoneInfo.ConvertTime(now, triggerZone);
                    var next = job.Trigger.GetNextOccurrence(localNow.UtcDateTime);
                    if (next is null)
                        continue;

                    var allowMisfire = job.EnableMisfireHandling ?? false;
                    var lastFired = storedState?.LastFiredAt;

                    if (next <= now)
                    {
                        var isMisfire = lastFired is not null && next < now;
                        if (isMisfire && !allowMisfire)
                        {
                            logger.LogWarning("Skipped misfired job '{JobId}' scheduled at {Next}", job.Id, next);
                            continue;
                        }

                        logger.LogInformation("Triggering job '{JobId}' at {Time}", job.Id, now);

                        var input = job.Input ?? Activator.CreateInstance(job.ContextType)
                            ?? throw new InvalidOperationException(
                                $"No input provided for job '{job.Id}', and context type has no parameterless constructor.");

                        await dispatcher.DispatchAsync(job.Id, input, cancellationToken);

                        var nextAfter = job.Trigger.GetNextOccurrence(now.UtcDateTime);

                        await storage.SetTriggerStateAsync(job.Id, new TriggerState
                        {
                            JobId = job.Id,
                            LastFiredAt = now,
                            NextDueAt = nextAfter,
                            TriggerId = job.Trigger.Id
                        }, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in trigger scheduler");
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        logger.LogInformation("Trigger scheduler stopped");
    }

    public Task PauseAsync()
    {
        _status = TriggerSchedulerStatus.Paused;
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        if (_status == TriggerSchedulerStatus.Paused)
            _status = TriggerSchedulerStatus.Running;

        return Task.CompletedTask;
    }

    public TriggerSchedulerStatus GetStatus() => _status;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _status = TriggerSchedulerStatus.Stopped;
        return Task.CompletedTask;
    }
}