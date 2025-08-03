using Chronux.Core.Triggers.Contracts;
using Chronux.Core.Triggers.Models;

namespace Chronux.Core.Triggers.Builtin;

public sealed class DelayTrigger(string id, TimeSpan delay, DateTimeOffset? baseTime = null)
    : ITrigger
{
    public string Id { get; } = id;
    public TriggerType Type => TriggerType.Delay;
    public TimeSpan Delay { get; } = delay;
    public TimeZoneInfo? TimeZone => null;

    private readonly DateTimeOffset _anchor = baseTime ?? DateTimeOffset.UtcNow;

    public DateTimeOffset? GetNextOccurrence(DateTimeOffset after)
    {
        var target = _anchor + Delay;
        return after < target ? target : null;
    }
}
