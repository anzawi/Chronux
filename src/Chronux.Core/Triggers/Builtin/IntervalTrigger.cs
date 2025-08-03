using Chronux.Core.Triggers.Contracts;
using Chronux.Core.Triggers.Models;

namespace Chronux.Core.Triggers.Builtin;

public sealed class IntervalTrigger(string id, TimeSpan interval)
    : ITrigger
{
    public string Id { get; } = id;
    public TriggerType Type => TriggerType.Interval;
    public TimeZoneInfo? TimeZone => null;
    public TimeSpan Interval { get; } = interval;

    public DateTimeOffset? GetNextOccurrence(DateTimeOffset after)
    {
        // Round forward to next occurrence
        var ticks = (after.Ticks / Interval.Ticks + 1) * Interval.Ticks;
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
