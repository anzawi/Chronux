using Chronux.Core.Enqueuing.Models;

namespace Chronux.Core.Storage.Models;

public sealed record DeadLetterItem
{
    public required Guid Id { get; init; }
    public required string JobId { get; init; }
    public required DateTimeOffset FailedAt { get; init; }
    public required object? Input { get; init; }
    public required string? ErrorMessage { get; init; }
    public required Exception? Exception { get; init; }
    
    public EnqueueMetadata? Metadata { get; init; }
    public int RetryAttempt { get; init; }
    public int MaxAttempts { get; init; }
    public string? TriggerId { get; init; }
    public string? InstanceId { get; init; }
    public string[]? Tags { get; init; }
}
