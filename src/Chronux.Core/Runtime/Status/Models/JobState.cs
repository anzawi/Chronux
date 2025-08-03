namespace Chronux.Core.Runtime.Status.Models;

public enum JobState
{
    Queued,
    Running,
    Succeeded,
    Failed,
    DeadLettered
}