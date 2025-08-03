using Chronux.Core.Scheduling.Internal.Contracts;
using Microsoft.Extensions.Hosting;

namespace Chronux.Core;

public sealed class ChronuxHostedService(ITriggerScheduler scheduler) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => scheduler.StartAsync(stoppingToken);
}