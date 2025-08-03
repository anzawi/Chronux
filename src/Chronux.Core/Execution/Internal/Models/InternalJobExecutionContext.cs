using Chronux.Core.Enqueuing.Models;
using Chronux.Core.Execution.Models;
using Chronux.Core.Jobs.Models;
using Chronux.Core.Scheduling.Models;

namespace Chronux.Core.Execution.Internal.Models;

internal sealed class InternalJobExecutionContext
{
    public required JobDefinition Definition { get; init; }
    public required object? Input { get; init; }
    public required CancellationToken CancellationToken { get; init; }
    public DateTimeOffset ScheduledTime { get; init; } = DateTimeOffset.UtcNow;
    public EnqueueMetadata? Metadata { get; init; }

    public JobExecutionContext ToPublicContext(string[] tags, CancellationToken? cancellationToken = null) => new()
    {
        Job = new JobMetadata
        {
            Id = Definition.Id,
            Description = Definition.Description,
            Metadata = Definition.Metadata
        },
        Input = Input,
        ScheduledTime = ScheduledTime,
        Tags = tags,
        CancellationToken = cancellationToken ?? CancellationToken
    };
}