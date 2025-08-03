using Chronux.Core.Triggers.Models;

namespace Chronux.Core.Triggers.Contracts;

public interface ITrigger
{
    string Id { get; }
    TriggerType Type { get; }
    TimeZoneInfo? TimeZone { get; }
    DateTimeOffset? GetNextOccurrence(DateTimeOffset after);
}