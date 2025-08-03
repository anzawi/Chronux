using Chronux.Core.Triggers.Contracts;
using Chronux.Core.Triggers.Models;
using NCrontab;
namespace Chronux.Core.Triggers.Builtin;


public sealed class CronTrigger : ITrigger
{
    public string Id { get; }
    public TriggerType Type => TriggerType.Cron;

    public string Expression { get; }
    public TimeZoneInfo TimeZone { get; }

    private readonly CrontabSchedule _schedule;

    public CronTrigger(string expression, TimeZoneInfo? timeZone = null, string? id = null)
    {
        Expression = expression;
        TimeZone = timeZone ?? TimeZoneInfo.Utc;
        Id = id ?? $"cron:{expression}";

        try
        {
            _schedule = CrontabSchedule.Parse(expression, new CrontabSchedule.ParseOptions
            {
                IncludingSeconds = false
            });
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid cron expression '{expression}'", ex);
        }
    }

    public DateTimeOffset? GetNextOccurrence(DateTimeOffset after)
    {
        var localAfter = TimeZoneInfo.ConvertTime(after.UtcDateTime, TimeZone);
        var localNext = _schedule.GetNextOccurrence(localAfter);

        var nextUtc = TimeZoneInfo.ConvertTimeToUtc(localNext, TimeZone);
        return new DateTimeOffset(nextUtc, TimeSpan.Zero);
    }
    
    TimeZoneInfo? ITrigger.TimeZone => TimeZone; 
}