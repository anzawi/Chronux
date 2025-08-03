using Microsoft.Extensions.Logging;

namespace Chronux.Core.Jobs.Models;

public sealed class JobContext<T>
{
    public required T Input { get; init; }
    public required DateTimeOffset ScheduledTime { get; init; }
    public required string JobId { get; init; }
    public required IServiceProvider Services { get; init; }
    public Dictionary<string, object?> Metadata { get; init; } = [];
    public ILogger? Logger { get; init; }
}