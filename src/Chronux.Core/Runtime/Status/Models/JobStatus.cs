namespace Chronux.Core.Runtime.Status.Models;

public sealed class JobStatus
{
    public required string JobId { get; init; }
    public JobState State { get; init; }

    public DateTimeOffset? LastRun { get; init; }
    public bool? LastSuccess { get; init; }
    public int? RetryAttempt { get; init; }

    public string? ErrorMessage { get; init; }
    public string[]? Tags { get; init; }
    public string? CorrelationId { get; init; }
    public string? TriggerSource { get; init; }
    public string? UserId { get; init; }
}
