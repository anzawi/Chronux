using Chronux.Core.Configuration.Models;
using Chronux.Core.Storage.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chronux.Core.Workers.Internal;

internal sealed class RetentionWorker(
    IChronuxStorageProvider storage,
    ChronuxOptions options,
    ILogger<RetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromMinutes(5); // configurable later
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = options.TimeProvider.GetUtcNow();
                if (options.Retention.MaxAge is { } age)
                {
                    var cutoff = now - age;
                    await storage.PurgeExecutionLogsOlderThanAsync(cutoff, stoppingToken);
                }

                if (options.Retention.MaxCountPerJob is { } maxCount)
                {
                    await storage.PurgeExecutionLogsOverLimitAsync(maxCount, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Retention cleanup failed");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }
}
