using Chronux.Core.Jobs.Models;

namespace Chronux.Core.Execution.Models;

public sealed class JobExecutionContext
{
    public required JobMetadata Job { get; init; }
    public required object? Input { get; init; }
    public DateTimeOffset ScheduledTime { get; init; }
    public CancellationToken CancellationToken { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = [];
}