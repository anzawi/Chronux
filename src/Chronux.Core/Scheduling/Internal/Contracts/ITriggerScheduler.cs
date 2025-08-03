using Chronux.Core.Scheduling.Models;

namespace Chronux.Core.Scheduling.Internal.Contracts;
//Future versions can support StopAsync() or Pause()

public interface ITriggerScheduler
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task PauseAsync();
    Task ResumeAsync();
    TriggerSchedulerStatus GetStatus();
}