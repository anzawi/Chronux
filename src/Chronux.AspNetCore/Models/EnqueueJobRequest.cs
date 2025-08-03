using Chronux.Core.Enqueuing.Models;

namespace Chronux.AspNetCore.Models;

public sealed class EnqueueJobRequest
{
    public required string JobId { get; init; }
    public object? Input { get; init; }
    public EnqueueMetadata? Metadata { get; init; }
}