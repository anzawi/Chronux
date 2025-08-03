using Chronux.Core.Enqueuing.Models;

namespace Chronux.Core.Storage.Models;

public sealed class EnqueuedJob
{
    public required string JobId { get; init; }
    public object? Input { get; init; }
    public DateTimeOffset EnqueuedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? RequestId { get; init; }
    
    public EnqueueMetadata? Metadata { get; init; }
}