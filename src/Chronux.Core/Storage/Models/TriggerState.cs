namespace Chronux.Core.Storage.Models;

public sealed class TriggerState
{
    public required string JobId { get; init; }
    public DateTimeOffset? LastFiredAt { get; init; }
    public DateTimeOffset? NextDueAt { get; init; }
    public string? TriggerId { get; init; }
}
