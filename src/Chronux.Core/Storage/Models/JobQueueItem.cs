namespace Chronux.Core.Storage.Models;

public sealed class JobQueueItem
{
    public required string Id { get; init; }
    public required string JobId { get; init; }
    public required object Input { get; init; }
    public required DateTimeOffset EnqueuedAt { get; init; }
}