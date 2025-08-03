namespace Chronux.Core.Storage.Models;

public sealed class ExecutionLog
{
    public required string JobId { get; init; }
    public required DateTimeOffset ExecutedAt { get; init; }
    public required bool Success { get; init; }
    public string? Message { get; init; }
    public Exception? Exception { get; init; }
    public TimeSpan Duration { get; init; }
    public string? TriggerId { get; init; }
    public string? InstanceId { get; init; }

    public string[]? Tags { get; init; }
    public string? CorrelationId { get; init; }
    public string? TriggerSource { get; init; }
    public string? UserId { get; init; }
    public int? RetryAttempt { get; init; }
    public int? RetryCount { get; init; }
    public bool? MaxAttemptsReached { get; init; }
    public TimeSpan? RetryDelay { get; init; }
    public object? Output { get; init; }
}