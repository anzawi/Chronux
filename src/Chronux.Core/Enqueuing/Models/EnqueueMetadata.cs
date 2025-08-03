namespace Chronux.Core.Enqueuing.Models;

public sealed class EnqueueMetadata
{
    public string? CorrelationId { get; init; }
    public string? TriggerSource { get; init; }
    public string? UserId { get; init; }
}