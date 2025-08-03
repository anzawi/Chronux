using System.Threading.Channels;
using Chronux.Core.Execution.Contracts;
using Chronux.Core.Storage.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chronux.Core.Enqueuing.Internal;

internal sealed class JobQueueWorker(
    Channel<EnqueuedJob> queue,
    IJobDispatcher dispatcher,
    ILogger<JobQueueWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Job queue worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await queue.Reader.ReadAsync(stoppingToken);
                logger.LogInformation("Dequeued job '{JobId}'", job.JobId);

                await dispatcher.DispatchAsync(
                    job.JobId,
                    job.Input,
                    job.Metadata,
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while dispatching enqueued job");
            }
        }

        logger.LogInformation("Job queue worker stopped");
    }
}